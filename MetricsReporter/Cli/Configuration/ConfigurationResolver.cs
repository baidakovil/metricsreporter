using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetricsReporter.Configuration;

namespace MetricsReporter.Cli.Configuration;

/// <summary>
/// Resolves effective configuration from CLI, environment, and config file sources.
/// </summary>
internal static class ConfigurationResolver
{
  private const int DefaultTimeoutSeconds = 900;
  private const int DefaultLogTruncationLimit = 4000;
  private const string DefaultVerbosity = "normal";

  /// <summary>
  /// Resolves general options using precedence: CLI &gt; env &gt; config &gt; defaults.
  /// </summary>
  public static ResolvedGeneralOptions ResolveGeneral(
    string? cliVerbosity,
    int? cliTimeoutSeconds,
    string? cliWorkingDirectory,
    int? cliLogTruncation,
    bool? cliRunScripts,
    bool? cliAggregateAfterScripts,
    MetricsReporterConfiguration envConfig,
    MetricsReporterConfiguration fileConfig)
  {
    ArgumentNullException.ThrowIfNull(envConfig);
    ArgumentNullException.ThrowIfNull(fileConfig);

    var workingDir = ResolveWorkingDirectory(cliWorkingDirectory, envConfig, fileConfig);
    var timeoutSeconds = ResolveTimeoutSeconds(cliTimeoutSeconds, envConfig, fileConfig);
    var truncationLimit = ResolveTruncationLimit(cliLogTruncation, envConfig, fileConfig);
    var runScripts = ResolveRunScripts(cliRunScripts, envConfig, fileConfig);
    var aggregateAfterScripts = ResolveAggregateAfterScripts(cliAggregateAfterScripts, envConfig, fileConfig);
    var verbosity = ResolveVerbosity(cliVerbosity, envConfig, fileConfig);

    return new ResolvedGeneralOptions(
      Verbosity: verbosity,
      Timeout: TimeSpan.FromSeconds(timeoutSeconds),
      WorkingDirectory: Path.GetFullPath(workingDir),
      LogTruncationLimit: truncationLimit,
      RunScripts: runScripts,
      AggregateAfterScripts: aggregateAfterScripts);
  }

  /// <summary>
  /// Resolves script lists with precedence: CLI &gt; env &gt; config.
  /// </summary>
  public static ResolvedScripts ResolveScripts(
    IReadOnlyList<string> cliGenerate,
    IReadOnlyList<string> cliReadAny,
    IReadOnlyList<(string Metric, string Path)> cliMetricScripts,
    IReadOnlyList<string> cliTestAny,
    IReadOnlyList<(string Metric, string Path)> cliTestMetricScripts,
    ScriptsConfiguration envScripts,
    ScriptsConfiguration fileScripts)
  {
    ArgumentNullException.ThrowIfNull(envScripts);
    ArgumentNullException.ThrowIfNull(fileScripts);

    var generate = cliGenerate.Count > 0
      ? cliGenerate
      : envScripts.Generate ?? fileScripts.Generate ?? Array.Empty<string>();

    var readAny = cliReadAny.Count > 0
      ? cliReadAny
      : envScripts.Read.Any ?? fileScripts.Read.Any ?? Array.Empty<string>();

    var byMetric = cliMetricScripts.Count > 0
      ? cliMetricScripts.Select(x => new MetricScript { Metrics = new[] { x.Metric }, Path = x.Path }).ToArray()
      : envScripts.Read.ByMetric.Count > 0
        ? envScripts.Read.ByMetric
        : fileScripts.Read.ByMetric;

    var testAny = cliTestAny.Count > 0
      ? cliTestAny
      : envScripts.Test.Any ?? fileScripts.Test.Any ?? Array.Empty<string>();

    var testByMetric = cliTestMetricScripts.Count > 0
      ? cliTestMetricScripts.Select(x => new MetricScript { Metrics = new[] { x.Metric }, Path = x.Path }).ToArray()
      : envScripts.Test.ByMetric.Count > 0
        ? envScripts.Test.ByMetric
        : fileScripts.Test.ByMetric;

    return new ResolvedScripts(generate, readAny, byMetric, testAny, testByMetric);
  }

  private static string ResolveWorkingDirectory(
    string? cliWorkingDirectory,
    MetricsReporterConfiguration envConfig,
    MetricsReporterConfiguration fileConfig)
  {
    return FirstNonEmpty(
             cliWorkingDirectory,
             envConfig.General.WorkingDirectory,
             fileConfig.General.WorkingDirectory,
             Environment.CurrentDirectory)
           ?? Environment.CurrentDirectory;
  }

  private static int ResolveTimeoutSeconds(
    int? cliTimeoutSeconds,
    MetricsReporterConfiguration envConfig,
    MetricsReporterConfiguration fileConfig)
  {
    var timeoutSeconds = cliTimeoutSeconds
                         ?? envConfig.General.TimeoutSeconds
                         ?? fileConfig.General.TimeoutSeconds
                         ?? DefaultTimeoutSeconds;

    return timeoutSeconds > 0 ? timeoutSeconds : DefaultTimeoutSeconds;
  }

  private static int ResolveTruncationLimit(
    int? cliLogTruncation,
    MetricsReporterConfiguration envConfig,
    MetricsReporterConfiguration fileConfig)
  {
    var truncationLimit = cliLogTruncation
                          ?? envConfig.General.LogTruncationLimit
                          ?? fileConfig.General.LogTruncationLimit
                          ?? DefaultLogTruncationLimit;

    return truncationLimit > 0 ? truncationLimit : DefaultLogTruncationLimit;
  }

  private static bool ResolveRunScripts(
    bool? cliRunScripts,
    MetricsReporterConfiguration envConfig,
    MetricsReporterConfiguration fileConfig)
  {
    return cliRunScripts
           ?? envConfig.General.RunScripts
           ?? fileConfig.General.RunScripts
           ?? true;
  }

  private static bool ResolveAggregateAfterScripts(
    bool? cliAggregateAfterScripts,
    MetricsReporterConfiguration envConfig,
    MetricsReporterConfiguration fileConfig)
  {
    return cliAggregateAfterScripts
           ?? envConfig.General.AggregateAfterScripts
           ?? fileConfig.General.AggregateAfterScripts
           ?? true;
  }

  private static string ResolveVerbosity(
    string? cliVerbosity,
    MetricsReporterConfiguration envConfig,
    MetricsReporterConfiguration fileConfig)
  {
    return (cliVerbosity ?? envConfig.General.Verbosity ?? fileConfig.General.Verbosity ?? DefaultVerbosity).Trim();
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
/// Resolved general options after precedence is applied.
/// </summary>
internal sealed record ResolvedGeneralOptions(
  string Verbosity,
  TimeSpan Timeout,
  string WorkingDirectory,
  int LogTruncationLimit,
  bool RunScripts,
  bool AggregateAfterScripts);

/// <summary>
/// Resolved scripts after precedence is applied.
/// </summary>
internal sealed record ResolvedScripts(
  IReadOnlyList<string> Generate,
  IReadOnlyList<string> ReadAny,
  IReadOnlyList<MetricScript> ReadByMetric,
  IReadOnlyList<string> TestAny,
  IReadOnlyList<MetricScript> TestByMetric);

