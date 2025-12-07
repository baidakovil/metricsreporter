using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.Logging;
using MetricsReporter.Services;
using MetricsReporter.Services.Scripts;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Generates metrics reports (JSON/HTML) from AltCover, Roslyn, and SARIF inputs.
/// </summary>
internal sealed class GenerateCommand : AsyncCommand<GenerateSettings>
{
  private readonly MetricsReporterConfigLoader _configLoader;
  private readonly ScriptExecutionService _scriptExecutor;
  private static readonly char[] FolderSeparators = [',', ';'];

  /// <summary>
  /// Initializes a new instance of the <see cref="GenerateCommand"/> class.
  /// </summary>
  /// <param name="configLoader">Configuration loader.</param>
  /// <param name="scriptExecutor">Script executor.</param>
  public GenerateCommand(MetricsReporterConfigLoader configLoader, ScriptExecutionService scriptExecutor)
  {
    _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
    _scriptExecutor = scriptExecutor ?? throw new ArgumentNullException(nameof(scriptExecutor));
  }

  /// <inheritdoc />
  public override async Task<int> ExecuteAsync(CommandContext context, GenerateSettings settings)
  {
    _ = context;
    var cancellationToken = CancellationToken.None;
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

      return (int)MetricsReporterExitCode.ValidationError;
    }

    var general = ConfigurationResolver.ResolveGeneral(
      settings.Verbosity,
      settings.TimeoutSeconds,
      settings.WorkingDirectory,
      settings.LogTruncationLimit,
      envConfig,
      configResult.Configuration);

    var resolved = ResolveGenerateInputs(
      settings,
      envConfig.Paths,
      configResult.Configuration.Paths,
      general.WorkingDirectory);

    var validation = Validate(resolved);
    if (!validation.Succeeded)
    {
      AnsiConsole.MarkupLine($"[red]{validation.Error}[/]");
      return (int)MetricsReporterExitCode.ValidationError;
    }

    var logPath = BuildLogPath(resolved, general.WorkingDirectory);
    var scripts = ConfigurationResolver.ResolveScripts(
      settings.Scripts,
      Array.Empty<string>(),
      Array.Empty<(string Metric, string Path)>(),
      envConfig.Scripts,
      configResult.Configuration.Scripts);

    using (var fileLogger = new FileLogger(logPath))
    {
      var logger = new VerbosityAwareLogger(fileLogger, general.Verbosity);
      var scriptResult = await _scriptExecutor.RunAsync(
        scripts.Generate,
        new ScriptExecutionContext(general.WorkingDirectory, general.Timeout, general.LogTruncationLimit, logger),
        cancellationToken).ConfigureAwait(false);

      if (!scriptResult.IsSuccess)
      {
        return (int)scriptResult.ExitCode;
      }
    }

    var options = BuildOptions(resolved, logPath);
    var application = new MetricsReporterApplication();
    var exitCode = await application.RunAsync(options, cancellationToken).ConfigureAwait(false);
    return (int)exitCode;
  }

  private static ResolvedGenerateInputs ResolveGenerateInputs(
    GenerateSettings settings,
    PathsConfiguration envPaths,
    PathsConfiguration filePaths,
    string workingDirectory)
  {
    var metricsDir = FirstNonEmpty(settings.MetricsDir, envPaths.MetricsDir, filePaths.MetricsDir);
    var solutionName = FirstNonEmpty(settings.SolutionName, envPaths.SolutionName, filePaths.SolutionName) ?? "Solution";
    var outputJson = FirstNonEmpty(settings.OutputJson, envPaths.Report, filePaths.Report);
    var outputHtml = FirstNonEmpty(settings.OutputHtml, envPaths.OutputHtml, filePaths.OutputHtml);
    var inputJson = FirstNonEmpty(settings.InputJson, envPaths.InputJson, filePaths.InputJson);
    var baseline = FirstNonEmpty(settings.Baseline, envPaths.Baseline, filePaths.Baseline);
    var baselineReference = FirstNonEmpty(settings.BaselineReference, envPaths.BaselineReference, filePaths.BaselineReference);
    var thresholdsFile = FirstNonEmpty(settings.ThresholdsFile, envPaths.Thresholds, filePaths.Thresholds);
    var thresholdsInline = FirstNonEmpty(settings.Thresholds, envPaths.ThresholdsInline, filePaths.ThresholdsInline);
    var excludedMembers = FirstNonEmpty(settings.ExcludedMembers, envPaths.ExcludedMembers, filePaths.ExcludedMembers);
    var excludedAssemblies = FirstNonEmpty(settings.ExcludedAssemblies, envPaths.ExcludedAssemblies, filePaths.ExcludedAssemblies);
    var excludedTypes = FirstNonEmpty(settings.ExcludedTypes, envPaths.ExcludedTypes, filePaths.ExcludedTypes);
    var baselineStoragePath = FirstNonEmpty(settings.BaselineStoragePath, envPaths.BaselineStoragePath, filePaths.BaselineStoragePath);
    var coverageHtmlDir = FirstNonEmpty(settings.CoverageHtmlDir, envPaths.CoverageHtmlDir, filePaths.CoverageHtmlDir);
    var suppressedSymbols = FirstNonEmpty(settings.SuppressedSymbols, envPaths.SuppressedSymbols, filePaths.SuppressedSymbols);
    var solutionDirectory = FirstNonEmpty(settings.SolutionDirectory, envPaths.SolutionDirectory, filePaths.SolutionDirectory);

    var replaceBaseline = settings.ReplaceBaseline
      || envPaths.ReplaceBaseline == true
      || filePaths.ReplaceBaseline == true;

    var analyzeSuppressed = settings.AnalyzeSuppressedSymbols
      || envPaths.AnalyzeSuppressedSymbols == true
      || filePaths.AnalyzeSuppressedSymbols == true;

    return new ResolvedGenerateInputs(
      SolutionName: solutionName,
      MetricsDir: MakeAbsolute(metricsDir, workingDirectory),
      AltCover: ResolveList(settings.AltCover, envPaths.AltCover, filePaths.AltCover, workingDirectory),
      Roslyn: ResolveList(settings.Roslyn, envPaths.Roslyn, filePaths.Roslyn, workingDirectory),
      Sarif: ResolveList(settings.Sarif, envPaths.Sarif, filePaths.Sarif, workingDirectory),
      Baseline: MakeAbsolute(baseline, workingDirectory),
      BaselineReference: baselineReference,
      OutputJson: MakeAbsolute(outputJson, workingDirectory),
      OutputHtml: MakeAbsolute(outputHtml, workingDirectory),
      ThresholdsFile: MakeAbsolute(thresholdsFile, workingDirectory),
      ThresholdsInline: thresholdsInline,
      InputJson: MakeAbsolute(inputJson, workingDirectory),
      ExcludedMembers: excludedMembers,
      ExcludedAssemblies: excludedAssemblies,
      ExcludedTypes: excludedTypes,
      ReplaceBaseline: replaceBaseline,
      BaselineStoragePath: MakeAbsolute(baselineStoragePath, workingDirectory),
      CoverageHtmlDir: MakeAbsolute(coverageHtmlDir, workingDirectory),
      AnalyzeSuppressedSymbols: analyzeSuppressed,
      SuppressedSymbols: MakeAbsolute(suppressedSymbols, workingDirectory),
      SolutionDirectory: MakeAbsolute(solutionDirectory, workingDirectory),
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
    // Prefer a temp/log location alongside the baseline when available.
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
        .Select(path => MakeAbsolute(path, workingDirectory))
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Select(path => path!)
        .ToArray();
    }

    if (env is { Count: > 0 })
    {
      return env
        .Select(path => MakeAbsolute(path, workingDirectory))
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Select(path => path!)
        .ToArray();
    }

    if (file is { Count: > 0 })
    {
      return file
        .Select(path => MakeAbsolute(path, workingDirectory))
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

  private static string? MakeAbsolute(string? path, string workingDirectory)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      return null;
    }

    return Path.IsPathRooted(path)
      ? Path.GetFullPath(path)
      : Path.GetFullPath(Path.Combine(workingDirectory, path));
  }

  private static string? FirstNonEmpty(params string?[] values)
  {
    foreach (var value in values)
    {
      if (!string.IsNullOrWhiteSpace(value))
      {
        return value;
      }
    }

    return null;
  }
}

internal sealed record ResolvedGenerateInputs(
  string? SolutionName,
  string? MetricsDir,
  string[] AltCover,
  string[] Roslyn,
  string[] Sarif,
  string? Baseline,
  string? BaselineReference,
  string? OutputJson,
  string? OutputHtml,
  string? ThresholdsFile,
  string? ThresholdsInline,
  string? InputJson,
  string? ExcludedMembers,
  string? ExcludedAssemblies,
  string? ExcludedTypes,
  bool ReplaceBaseline,
  string? BaselineStoragePath,
  string? CoverageHtmlDir,
  bool AnalyzeSuppressedSymbols,
  string? SuppressedSymbols,
  string? SolutionDirectory,
  string[] SourceCodeFolders,
  bool MetricsDirProvided);

internal sealed record ValidationOutcome(bool Succeeded, string? Error)
{
  public static ValidationOutcome Success() => new(true, null);

  public static ValidationOutcome Fail(string message) => new(false, message);
}

