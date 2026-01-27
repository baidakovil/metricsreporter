namespace MetricsReporter.Cli.Infrastructure;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetricsReporter.Configuration;
using MetricsReporter.Services;

/// <summary>
/// Resolves aggregation inputs for commands that need to run the metrics pipeline after scripts.
/// </summary>
internal static class AggregationOptionsResolver
{
  /// <summary>
  /// Resolves aggregation inputs using precedence: env > config, with optional report override.
  /// </summary>
  public static ResolvedAggregationInputs Resolve(
    PathsConfiguration envPaths,
    PathsConfiguration filePaths,
    string workingDirectory,
    string? reportOverride)
  {
    ArgumentNullException.ThrowIfNull(envPaths);
    ArgumentNullException.ThrowIfNull(filePaths);
    ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

    var metricsDir = FirstNonEmpty(envPaths.MetricsDir, filePaths.MetricsDir);
    var outputJson = FirstNonEmpty(reportOverride, envPaths.ReadReport, filePaths.ReadReport, envPaths.Report, filePaths.Report);

    return new ResolvedAggregationInputs(
      SolutionName: FirstNonEmpty(envPaths.SolutionName, filePaths.SolutionName) ?? "Solution",
      MetricsDir: MakeAbsolute(metricsDir, workingDirectory),
      OpenCover: ResolveList(envPaths.OpenCover, filePaths.OpenCover, workingDirectory),
      Roslyn: ResolveList(envPaths.Roslyn, filePaths.Roslyn, workingDirectory),
      Sarif: ResolveList(envPaths.Sarif, filePaths.Sarif, workingDirectory),
      Baseline: MakeAbsolute(FirstNonEmpty(envPaths.Baseline, filePaths.Baseline), workingDirectory),
      BaselineReference: FirstNonEmpty(envPaths.BaselineReference, filePaths.BaselineReference),
      OutputJson: MakeAbsolute(outputJson, workingDirectory),
      OutputHtml: MakeAbsolute(FirstNonEmpty(envPaths.OutputHtml, filePaths.OutputHtml), workingDirectory),
      ThresholdsFile: MakeAbsolute(FirstNonEmpty(envPaths.Thresholds, filePaths.Thresholds), workingDirectory),
      ThresholdsInline: FirstNonEmpty(envPaths.ThresholdsInline, filePaths.ThresholdsInline),
      InputJson: MakeAbsolute(FirstNonEmpty(envPaths.InputJson, filePaths.InputJson), workingDirectory),
      ExcludedMembers: FirstNonEmpty(envPaths.ExcludedMembers, filePaths.ExcludedMembers),
      ExcludedAssemblies: FirstNonEmpty(envPaths.ExcludedAssemblies, filePaths.ExcludedAssemblies),
      ExcludedTypes: FirstNonEmpty(envPaths.ExcludedTypes, filePaths.ExcludedTypes),
      ExcludeMethods: envPaths.ExcludeMethods == true || filePaths.ExcludeMethods == true,
      ExcludeProperties: envPaths.ExcludeProperties == true || filePaths.ExcludeProperties == true,
      ExcludeFields: envPaths.ExcludeFields == true || filePaths.ExcludeFields == true,
      ExcludeEvents: envPaths.ExcludeEvents == true || filePaths.ExcludeEvents == true,
      ReplaceBaseline: envPaths.ReplaceBaseline == true || filePaths.ReplaceBaseline == true,
      BaselineStoragePath: MakeAbsolute(FirstNonEmpty(envPaths.BaselineStoragePath, filePaths.BaselineStoragePath), workingDirectory),
      CoverageHtmlDir: MakeAbsolute(FirstNonEmpty(envPaths.CoverageHtmlDir, filePaths.CoverageHtmlDir), workingDirectory),
      AnalyzeSuppressedSymbols: envPaths.AnalyzeSuppressedSymbols == true || filePaths.AnalyzeSuppressedSymbols == true,
      SuppressedSymbols: MakeAbsolute(FirstNonEmpty(envPaths.SuppressedSymbols, filePaths.SuppressedSymbols), workingDirectory),
      SolutionDirectory: MakeAbsolute(FirstNonEmpty(envPaths.SolutionDirectory, filePaths.SolutionDirectory), workingDirectory),
      SourceCodeFolders: ResolveFolders(envPaths.SourceCodeFolders, filePaths.SourceCodeFolders),
      MetricsDirProvided: !string.IsNullOrWhiteSpace(metricsDir));
  }

  /// <summary>
  /// Validates aggregation inputs for pipeline execution.
  /// </summary>
  public static ValidationOutcome Validate(ResolvedAggregationInputs inputs)
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
      return ValidationOutcome.Fail("--metrics-dir is required when aggregating from metrics inputs.");
    }

    if (string.IsNullOrWhiteSpace(inputs.OutputJson))
    {
      return ValidationOutcome.Fail("--report/--output-json is required when aggregating from metrics inputs.");
    }

    return ValidationOutcome.Success();
  }

  /// <summary>
  /// Builds a log path close to metrics artifacts.
  /// </summary>
  public static string BuildLogPath(ResolvedAggregationInputs inputs, string workingDirectory)
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

    Directory.CreateDirectory(workingDirectory);
    return Path.Combine(workingDirectory, "MetricsReporter.log");
  }

  /// <summary>
  /// Maps resolved inputs to pipeline options.
  /// </summary>
  public static MetricsReporterOptions BuildOptions(ResolvedAggregationInputs inputs, string logPath, string verbosity)
  {
    return new MetricsReporterOptions
    {
      SolutionName = inputs.SolutionName ?? "Solution",
      MetricsDirectory = inputs.MetricsDir ?? string.Empty,
      OpenCoverPaths = inputs.OpenCover,
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
      Verbosity = verbosity,
      ExcludedMemberNamesPatterns = inputs.ExcludedMembers,
      ExcludedAssemblyNames = inputs.ExcludedAssemblies,
      ExcludedTypeNamePatterns = inputs.ExcludedTypes,
      ExcludeMethods = inputs.ExcludeMethods,
      ExcludeProperties = inputs.ExcludeProperties,
      ExcludeFields = inputs.ExcludeFields,
      ExcludeEvents = inputs.ExcludeEvents,
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
    IReadOnlyList<string>? env,
    IReadOnlyList<string>? file,
    string workingDirectory)
  {
    IReadOnlyList<string>? selected = env ?? file;
    if (selected is null || selected.Count == 0)
    {
      return Array.Empty<string>();
    }

    return selected
      .Where(item => !string.IsNullOrWhiteSpace(item))
      .Select(item => MakeAbsolute(item, workingDirectory)!)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();
  }

  private static string[] ResolveFolders(
    IReadOnlyList<string>? env,
    IReadOnlyList<string>? file)
  {
    var selected = env ?? file;
    return selected is null
      ? Array.Empty<string>()
      : selected.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
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

/// <summary>
/// Resolved aggregation inputs for reader/test commands.
/// </summary>
internal sealed record ResolvedAggregationInputs(
  string? SolutionName,
  string? MetricsDir,
  string[] OpenCover,
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

