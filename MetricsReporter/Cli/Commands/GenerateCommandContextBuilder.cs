using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.Services;
using MetricsReporter.Services.Scripts;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds the execution context for the generate command, combining configuration, inputs, and validation.
/// </summary>
internal sealed class GenerateCommandContextBuilder
{
  private static readonly char[] FolderSeparators = [',', ';'];
  private readonly MetricsReporterConfigLoader _configLoader;

  public GenerateCommandContextBuilder(MetricsReporterConfigLoader configLoader)
  {
    _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
  }

  /// <summary>
  /// Creates a fully validated generate command context or returns an error outcome.
  /// </summary>
  /// <param name="settings">Generate CLI settings.</param>
  /// <returns>Outcome containing context or exit code.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Build orchestrates configuration, path resolution, script resolution, and options assembly in a single entry point; further splitting into smaller methods would scatter validation flow and reduce readability for the command pipeline.")]
  public BuildGenerateContextResult Build(GenerateSettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);

    var configuration = LoadConfiguration(settings);
    if (!configuration.Succeeded)
    {
      return BuildGenerateContextResult.CreateFailure(configuration.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var inputs = ResolveInputs(settings, configuration);
    if (!inputs.Succeeded)
    {
      return BuildGenerateContextResult.CreateFailure(inputs.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var validation = Validate(inputs.Inputs!);
    if (!validation.Succeeded)
    {
      AnsiConsole.MarkupLine($"[red]{validation.Error}[/]");
      return BuildGenerateContextResult.CreateFailure((int)MetricsReporterExitCode.ValidationError);
    }

    var logPath = BuildLogPath(inputs.Inputs!, configuration.GeneralOptions.WorkingDirectory);
    var scripts = ResolveScripts(settings, configuration);
    if (!scripts.Succeeded)
    {
      return BuildGenerateContextResult.CreateFailure(scripts.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var options = BuildOptions(inputs.Inputs!, logPath);
    return BuildGenerateContextResult.CreateSuccess(
      new GenerateCommandContext(
        configuration.GeneralOptions,
        scripts.Scripts!,
        inputs.Inputs!,
        options,
        logPath));
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Configuration load step must evaluate CLI, environment, and file configuration sources together; the coupling reflects required dependencies for resolution.")]
  private ConfigurationLoadResult LoadConfiguration(GenerateSettings settings)
  {
    var envConfig = EnvironmentConfigurationProvider.Read();
    var workingDirectoryHint = settings.WorkingDirectory
      ?? envConfig.General.WorkingDirectory
      ?? Environment.CurrentDirectory;

    var configResult = _configLoader.Load(settings.ConfigPath, workingDirectoryHint);
    if (!configResult.IsSuccess)
    {
      foreach (var error in configResult.Errors)
      {
        AnsiConsole.MarkupLine($"[red]{error}[/]");
      }

      return ConfigurationLoadResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    var general = ConfigurationResolver.ResolveGeneral(
      settings.Verbosity,
      settings.TimeoutSeconds,
      settings.WorkingDirectory,
      settings.LogTruncationLimit,
      settings.RunScripts,
      settings.AggregateAfterScripts,
      envConfig,
      configResult.Configuration);

    return ConfigurationLoadResult.Success(general, envConfig, configResult.Configuration);
  }

  private static GenerateInputResolutionResult ResolveInputs(GenerateSettings settings, ConfigurationLoadResult configuration)
  {
    var resolvedInputs = ResolveGenerateInputs(settings, configuration.EnvironmentConfiguration.Paths, configuration.FileConfiguration.Paths, configuration.GeneralOptions.WorkingDirectory);
    return GenerateInputResolutionResult.Success(resolvedInputs);
  }

  private static ScriptResolutionResult ResolveScripts(GenerateSettings settings, ConfigurationLoadResult configuration)
  {
    var scripts = ConfigurationResolver.ResolveScripts(
      settings.Scripts,
      Array.Empty<string>(),
      Array.Empty<(string Metric, string Path)>(),
      Array.Empty<string>(),
      Array.Empty<(string Metric, string Path)>(),
      configuration.EnvironmentConfiguration.Scripts,
      configuration.FileConfiguration.Scripts);

    return ScriptResolutionResult.Success(scripts);
  }

  private static ResolvedGenerateInputs ResolveGenerateInputs(
    GenerateSettings settings,
    PathsConfiguration envPaths,
    PathsConfiguration filePaths,
    string workingDirectory)
  {
    var metricsDir = CommandPathResolver.FirstNonEmpty(settings.MetricsDir, envPaths.MetricsDir, filePaths.MetricsDir);
    var solutionName = CommandPathResolver.FirstNonEmpty(settings.SolutionName, envPaths.SolutionName, filePaths.SolutionName) ?? "Solution";
    var outputJson = CommandPathResolver.FirstNonEmpty(settings.OutputJson, envPaths.Report, filePaths.Report);
    var outputHtml = CommandPathResolver.FirstNonEmpty(settings.OutputHtml, envPaths.OutputHtml, filePaths.OutputHtml);
    var inputJson = CommandPathResolver.FirstNonEmpty(settings.InputJson, envPaths.InputJson, filePaths.InputJson);
    var baseline = CommandPathResolver.FirstNonEmpty(settings.Baseline, envPaths.Baseline, filePaths.Baseline);
    var baselineReference = CommandPathResolver.FirstNonEmpty(settings.BaselineReference, envPaths.BaselineReference, filePaths.BaselineReference);
    var thresholdsFile = CommandPathResolver.FirstNonEmpty(settings.ThresholdsFile, envPaths.Thresholds, filePaths.Thresholds);
    var thresholdsInline = CommandPathResolver.FirstNonEmpty(settings.Thresholds, envPaths.ThresholdsInline, filePaths.ThresholdsInline);
    var excludedMembers = CommandPathResolver.FirstNonEmpty(settings.ExcludedMembers, envPaths.ExcludedMembers, filePaths.ExcludedMembers);
    var excludedAssemblies = CommandPathResolver.FirstNonEmpty(settings.ExcludedAssemblies, envPaths.ExcludedAssemblies, filePaths.ExcludedAssemblies);
    var excludedTypes = CommandPathResolver.FirstNonEmpty(settings.ExcludedTypes, envPaths.ExcludedTypes, filePaths.ExcludedTypes);
    var baselineStoragePath = CommandPathResolver.FirstNonEmpty(settings.BaselineStoragePath, envPaths.BaselineStoragePath, filePaths.BaselineStoragePath);
    var coverageHtmlDir = CommandPathResolver.FirstNonEmpty(settings.CoverageHtmlDir, envPaths.CoverageHtmlDir, filePaths.CoverageHtmlDir);
    var suppressedSymbols = CommandPathResolver.FirstNonEmpty(settings.SuppressedSymbols, envPaths.SuppressedSymbols, filePaths.SuppressedSymbols);
    var solutionDirectory = CommandPathResolver.FirstNonEmpty(settings.SolutionDirectory, envPaths.SolutionDirectory, filePaths.SolutionDirectory);

    var replaceBaseline = settings.ReplaceBaseline
      || envPaths.ReplaceBaseline == true
      || filePaths.ReplaceBaseline == true;

    var analyzeSuppressed = settings.AnalyzeSuppressedSymbols
      || envPaths.AnalyzeSuppressedSymbols == true
      || filePaths.AnalyzeSuppressedSymbols == true;

    return new ResolvedGenerateInputs(
      SolutionName: solutionName,
      MetricsDir: CommandPathResolver.MakeAbsolute(metricsDir, workingDirectory),
      AltCover: ResolveList(settings.AltCover, envPaths.AltCover, filePaths.AltCover, workingDirectory),
      Roslyn: ResolveList(settings.Roslyn, envPaths.Roslyn, filePaths.Roslyn, workingDirectory),
      Sarif: ResolveList(settings.Sarif, envPaths.Sarif, filePaths.Sarif, workingDirectory),
      Baseline: CommandPathResolver.MakeAbsolute(baseline, workingDirectory),
      BaselineReference: baselineReference,
      OutputJson: CommandPathResolver.MakeAbsolute(outputJson, workingDirectory),
      OutputHtml: CommandPathResolver.MakeAbsolute(outputHtml, workingDirectory),
      ThresholdsFile: CommandPathResolver.MakeAbsolute(thresholdsFile, workingDirectory),
      ThresholdsInline: thresholdsInline,
      InputJson: CommandPathResolver.MakeAbsolute(inputJson, workingDirectory),
      ExcludedMembers: excludedMembers,
      ExcludedAssemblies: excludedAssemblies,
      ExcludedTypes: excludedTypes,
      ReplaceBaseline: replaceBaseline,
      BaselineStoragePath: CommandPathResolver.MakeAbsolute(baselineStoragePath, workingDirectory),
      CoverageHtmlDir: CommandPathResolver.MakeAbsolute(coverageHtmlDir, workingDirectory),
      AnalyzeSuppressedSymbols: analyzeSuppressed,
      SuppressedSymbols: CommandPathResolver.MakeAbsolute(suppressedSymbols, workingDirectory),
      SolutionDirectory: CommandPathResolver.MakeAbsolute(solutionDirectory, workingDirectory),
      SourceCodeFolders: ResolveFolders(settings.SourceCodeFolders, envPaths.SourceCodeFolders, filePaths.SourceCodeFolders),
      MetricsDirProvided: !string.IsNullOrWhiteSpace(metricsDir));
  }

  private static ValidationOutcome Validate(ResolvedGenerateInputs inputs)
  {
    if (!string.IsNullOrWhiteSpace(inputs.InputJson))
    {
      if (string.IsNullOrWhiteSpace(inputs.OutputHtml))
      {
        return ValidationOutcome.Fail("--output-html is required when using --input-json.");
      }

      return ValidationOutcome.Success();
    }

    if (!inputs.MetricsDirProvided)
    {
      return ValidationOutcome.Fail("--metrics-dir is required when generating from metrics inputs.");
    }

    if (string.IsNullOrWhiteSpace(inputs.OutputJson))
    {
      return ValidationOutcome.Fail("--output-json is required when generating from metrics inputs.");
    }

    return ValidationOutcome.Success();
  }

  private static string BuildLogPath(ResolvedGenerateInputs inputs, string workingDirectory)
  {
    if (!string.IsNullOrWhiteSpace(inputs.Baseline))
    {
      var baselineDir = Path.GetDirectoryName(inputs.Baseline);
      if (!string.IsNullOrWhiteSpace(baselineDir))
      {
        Directory.CreateDirectory(baselineDir);
        return Path.Combine(baselineDir, "MetricsReporter.log");
      }
    }

    if (!string.IsNullOrWhiteSpace(inputs.MetricsDir))
    {
      Directory.CreateDirectory(inputs.MetricsDir);
      return Path.Combine(inputs.MetricsDir, "MetricsReporter.log");
    }

    if (!string.IsNullOrWhiteSpace(inputs.OutputJson))
    {
      var directory = Path.GetDirectoryName(inputs.OutputJson);
      if (!string.IsNullOrWhiteSpace(directory))
      {
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "MetricsReporter.log");
      }
    }

    return Path.Combine(workingDirectory, "MetricsReporter.log");
  }

  private static MetricsReporterOptions BuildOptions(ResolvedGenerateInputs inputs, string logPath)
  {
    return new MetricsReporterOptions
    {
      SolutionName = inputs.SolutionName ?? "Solution",
      MetricsDirectory = inputs.MetricsDir ?? string.Empty,
      AltCoverPaths = inputs.AltCover,
      RoslynPaths = inputs.Roslyn,
      SarifPaths = inputs.Sarif,
      BaselinePath = inputs.Baseline,
      BaselineReference = inputs.BaselineReference,
      ThresholdsJson = inputs.ThresholdsInline,
      ThresholdsPath = inputs.ThresholdsFile,
      InputJsonPath = inputs.InputJson,
      OutputJsonPath = inputs.OutputJson ?? string.Empty,
      OutputHtmlPath = inputs.OutputHtml ?? string.Empty,
      LogFilePath = logPath,
      ExcludedMemberNamesPatterns = inputs.ExcludedMembers,
      ExcludedAssemblyNames = inputs.ExcludedAssemblies,
      ExcludedTypeNamePatterns = inputs.ExcludedTypes,
      ReplaceMetricsBaseline = inputs.ReplaceBaseline,
      MetricsReportStoragePath = inputs.BaselineStoragePath,
      CoverageHtmlDir = inputs.CoverageHtmlDir,
      AnalyzeSuppressedSymbols = inputs.AnalyzeSuppressedSymbols,
      SuppressedSymbolsPath = inputs.SuppressedSymbols,
      SolutionDirectory = inputs.SolutionDirectory,
      SourceCodeFolders = inputs.SourceCodeFolders
    };
  }

  private static string[] ResolveList(
    List<string> cli,
    IReadOnlyList<string>? env,
    IReadOnlyList<string>? file,
    string workingDirectory)
  {
    if (cli.Count > 0)
    {
      return cli
        .Select(path => CommandPathResolver.MakeAbsolute(path, workingDirectory))
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Select(path => path!)
        .ToArray();
    }

    if (env is { Count: > 0 })
    {
      return env
        .Select(path => CommandPathResolver.MakeAbsolute(path, workingDirectory))
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Select(path => path!)
        .ToArray();
    }

    if (file is { Count: > 0 })
    {
      return file
        .Select(path => CommandPathResolver.MakeAbsolute(path, workingDirectory))
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Select(path => path!)
        .ToArray();
    }

    return Array.Empty<string>();
  }

  private static string[] ResolveFolders(
    string? cliFolders,
    IReadOnlyList<string>? envFolders,
    IReadOnlyList<string>? fileFolders)
  {
    if (!string.IsNullOrWhiteSpace(cliFolders))
    {
      return SplitFolders(cliFolders);
    }

    if (envFolders is { Count: > 0 })
    {
      return envFolders as string[] ?? envFolders.ToArray();
    }

    if (fileFolders is { Count: > 0 })
    {
      return fileFolders as string[] ?? fileFolders.ToArray();
    }

    return Array.Empty<string>();
  }

  private static string[] SplitFolders(string value)
  {
    return value
      .Split(FolderSeparators, StringSplitOptions.RemoveEmptyEntries)
      .Select(folder => folder.Trim())
      .Where(folder => folder.Length > 0)
      .ToArray();
  }
}

/// <summary>
/// Represents the fully resolved inputs for executing the generate command.
/// </summary>
/// <param name="GeneralOptions">Resolved general options shared across commands.</param>
/// <param name="Scripts">Scripts to run for the generate command.</param>
/// <param name="Inputs">Resolved generate inputs including sources and outputs.</param>
/// <param name="Options">Options passed to the MetricsReporter application.</param>
/// <param name="LogPath">Log path chosen for generation.</param>
internal sealed record GenerateCommandContext(
  ResolvedGeneralOptions GeneralOptions,
  ResolvedScripts Scripts,
  ResolvedGenerateInputs Inputs,
  MetricsReporterOptions Options,
  string LogPath);

/// <summary>
/// Outcome of building the generate command context.
/// </summary>
/// <param name="Succeeded">Indicates whether context creation succeeded.</param>
/// <param name="ExitCode">Exit code to return when creation fails.</param>
/// <param name="Context">Resolved context when successful.</param>
internal sealed record BuildGenerateContextResult(bool Succeeded, int? ExitCode, GenerateCommandContext? Context)
{
  public static BuildGenerateContextResult CreateSuccess(GenerateCommandContext context) =>
    new(true, null, context);

  public static BuildGenerateContextResult CreateFailure(int exitCode) =>
    new(false, exitCode, null);
}

/// <summary>
/// Outcome of resolving generate inputs from CLI and configuration.
/// </summary>
/// <param name="Succeeded">Indicates resolution success.</param>
/// <param name="ExitCode">Exit code to use on failure.</param>
/// <param name="Inputs">Resolved inputs when successful.</param>
internal sealed record GenerateInputResolutionResult(bool Succeeded, int? ExitCode, ResolvedGenerateInputs? Inputs)
{
  public static GenerateInputResolutionResult Success(ResolvedGenerateInputs inputs) =>
    new(true, null, inputs);

  public static GenerateInputResolutionResult Failure(int exitCode) =>
    new(false, exitCode, null);
}

