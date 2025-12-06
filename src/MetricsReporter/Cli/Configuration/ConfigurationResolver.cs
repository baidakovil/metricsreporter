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
    MetricsReporterConfiguration envConfig,
    MetricsReporterConfiguration fileConfig)
  {
    ArgumentNullException.ThrowIfNull(envConfig);
    ArgumentNullException.ThrowIfNull(fileConfig);

    var workingDir = FirstNonEmpty(
        cliWorkingDirectory,
        envConfig.General.WorkingDirectory,
        fileConfig.General.WorkingDirectory,
        Environment.CurrentDirectory)!;

    var timeoutSeconds = cliTimeoutSeconds
                         ?? envConfig.General.TimeoutSeconds
                         ?? fileConfig.General.TimeoutSeconds
                         ?? DefaultTimeoutSeconds;
    if (timeoutSeconds <= 0)
    {
      timeoutSeconds = DefaultTimeoutSeconds;
    }

    var truncationLimit = cliLogTruncation
                          ?? envConfig.General.LogTruncationLimit
                          ?? fileConfig.General.LogTruncationLimit
                          ?? DefaultLogTruncationLimit;
    if (truncationLimit <= 0)
    {
      truncationLimit = DefaultLogTruncationLimit;
    }

    return new ResolvedGeneralOptions(
      Verbosity: (cliVerbosity ?? envConfig.General.Verbosity ?? fileConfig.General.Verbosity ?? DefaultVerbosity).Trim(),
      Timeout: TimeSpan.FromSeconds(timeoutSeconds),
      WorkingDirectory: Path.GetFullPath(workingDir),
      LogTruncationLimit: truncationLimit);
  }

  /// <summary>
  /// Resolves script lists with precedence: CLI &gt; env &gt; config.
  /// </summary>
  public static ResolvedScripts ResolveScripts(
    IReadOnlyList<string> cliGenerate,
    IReadOnlyList<string> cliReadAny,
    IReadOnlyList<(string Metric, string Path)> cliMetricScripts,
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

    return new ResolvedScripts(generate, readAny, byMetric);
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
  int LogTruncationLimit);

/// <summary>
/// Resolved scripts after precedence is applied.
/// </summary>
internal sealed record ResolvedScripts(
  IReadOnlyList<string> Generate,
  IReadOnlyList<string> ReadAny,
  IReadOnlyList<MetricScript> ReadByMetric);

