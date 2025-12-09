using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Resolves generate command inputs and builds the log path based on sources precedence.
/// </summary>
internal static class GenerateInputResolver
{
  private static readonly char[] FolderSeparators = [',', ';'];

  /// <summary>
  /// Resolves inputs for the generate command and determines the log path location.
  /// </summary>
  /// <param name="settings">Generate CLI settings.</param>
  /// <param name="configuration">Previously resolved configuration.</param>
  /// <returns>Input resolution result including the log path.</returns>
  public static GenerateInputResolutionResult Resolve(GenerateSettings settings, ConfigurationLoadResult configuration)
  {
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(configuration);

    var resolvedInputs = ResolveGenerateInputs(
      settings,
      configuration.EnvironmentConfiguration.Paths,
      configuration.FileConfiguration.Paths,
      configuration.GeneralOptions.WorkingDirectory);

    var logPath = BuildLogPath(resolvedInputs, configuration.GeneralOptions.WorkingDirectory);
    return GenerateInputResolutionResult.Success(resolvedInputs, logPath);
  }

  /// <summary>
  /// Validates the resolved inputs to ensure the command has required arguments.
  /// </summary>
  /// <param name="inputs">Resolved generate inputs.</param>
  /// <returns>Validation outcome.</returns>
  public static ValidationOutcome Validate(ResolvedGenerateInputs inputs)
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
    var excludeMethods = settings.ExcludeMethods
      || envPaths.ExcludeMethods == true
      || filePaths.ExcludeMethods == true;
    var excludeProperties = settings.ExcludeProperties
      || envPaths.ExcludeProperties == true
      || filePaths.ExcludeProperties == true;
    var excludeFields = settings.ExcludeFields
      || envPaths.ExcludeFields == true
      || filePaths.ExcludeFields == true;
    var excludeEvents = settings.ExcludeEvents
      || envPaths.ExcludeEvents == true
      || filePaths.ExcludeEvents == true;
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
      ExcludeMethods: excludeMethods,
      ExcludeProperties: excludeProperties,
      ExcludeFields: excludeFields,
      ExcludeEvents: excludeEvents,
      ReplaceBaseline: replaceBaseline,
      BaselineStoragePath: CommandPathResolver.MakeAbsolute(baselineStoragePath, workingDirectory),
      CoverageHtmlDir: CommandPathResolver.MakeAbsolute(coverageHtmlDir, workingDirectory),
      AnalyzeSuppressedSymbols: analyzeSuppressed,
      SuppressedSymbols: CommandPathResolver.MakeAbsolute(suppressedSymbols, workingDirectory),
      SolutionDirectory: CommandPathResolver.MakeAbsolute(solutionDirectory, workingDirectory),
      SourceCodeFolders: ResolveFolders(settings.SourceCodeFolders, envPaths.SourceCodeFolders, filePaths.SourceCodeFolders),
      MetricsDirProvided: !string.IsNullOrWhiteSpace(metricsDir));
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
/// Outcome of resolving generate inputs together with the log path.
/// </summary>
/// <param name="Succeeded">Indicates resolution success.</param>
/// <param name="ExitCode">Exit code to use on failure.</param>
/// <param name="Inputs">Resolved inputs when successful.</param>
/// <param name="LogPath">Resolved log path when successful.</param>
internal sealed record GenerateInputResolutionResult(bool Succeeded, int? ExitCode, ResolvedGenerateInputs? Inputs, string? LogPath)
{
  public static GenerateInputResolutionResult Success(ResolvedGenerateInputs inputs, string logPath) =>
    new(true, null, inputs, logPath);

  public static GenerateInputResolutionResult Failure(int exitCode) =>
    new(false, exitCode, null, null);
}

/// <summary>
/// Resolved inputs for the generate command.
/// </summary>
internal sealed record ResolvedGenerateInputs(
  string SolutionName,
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
  bool ExcludeMethods,
  bool ExcludeProperties,
  bool ExcludeFields,
  bool ExcludeEvents,
  bool ReplaceBaseline,
  string? BaselineStoragePath,
  string? CoverageHtmlDir,
  bool AnalyzeSuppressedSymbols,
  string? SuppressedSymbols,
  string? SolutionDirectory,
  string[] SourceCodeFolders,
  bool MetricsDirProvided);

