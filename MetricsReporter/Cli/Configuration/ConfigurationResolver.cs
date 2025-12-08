using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetricsReporter.Configuration;
using MetricsReporter.Model;

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

    var generate = ResolveScriptList(cliGenerate, envScripts.Generate, fileScripts.Generate);
    var readAny = ResolveScriptList(cliReadAny, envScripts.Read.Any, fileScripts.Read.Any);
    var byMetric = ResolveMetricScripts(cliMetricScripts, envScripts.Read.ByMetric, fileScripts.Read.ByMetric);
    var testAny = ResolveScriptList(cliTestAny, envScripts.Test.Any, fileScripts.Test.Any);
    var testByMetric = ResolveMetricScripts(cliTestMetricScripts, envScripts.Test.ByMetric, fileScripts.Test.ByMetric);

    return new ResolvedScripts(generate, readAny, byMetric, testAny, testByMetric);
  }

  private static IReadOnlyList<string> ResolveScriptList(
    IReadOnlyList<string> cliScripts,
    IReadOnlyList<string>? envScripts,
    IReadOnlyList<string>? fileScripts)
  {
    if (cliScripts.Count > 0)
    {
      return cliScripts;
    }

    return envScripts ?? fileScripts ?? Array.Empty<string>();
  }

  private static IReadOnlyList<MetricScript> ResolveMetricScripts(
    IReadOnlyList<(string Metric, string Path)> cliScripts,
    IReadOnlyList<MetricScript> envScripts,
    IReadOnlyList<MetricScript> fileScripts)
  {
    if (cliScripts.Count > 0)
    {
      return ToMetricScripts(cliScripts);
    }

    if (envScripts.Count > 0)
    {
      return envScripts;
    }

    return fileScripts;
  }

  private static MetricScript[] ToMetricScripts(IReadOnlyList<(string Metric, string Path)> cliScripts)
  {
    return cliScripts
      .Select(script => new MetricScript { Metrics = new[] { script.Metric }, Path = script.Path })
      .ToArray();
  }

  /// <summary>
  /// Resolves metric alias mappings using precedence: CLI &gt; env &gt; file.
  /// </summary>
  /// <param name="cliAliases">Optional CLI-provided aliases keyed by metric identifier.</param>
  /// <param name="envConfig">Environment configuration.</param>
  /// <param name="fileConfig">File configuration.</param>
  /// <returns>Normalized alias map keyed by <see cref="MetricIdentifier"/>.</returns>
  public static IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> ResolveMetricAliases(
    IDictionary<string, string[]>? cliAliases,
    MetricsReporterConfiguration envConfig,
    MetricsReporterConfiguration fileConfig)
  {
    ArgumentNullException.ThrowIfNull(envConfig);
    ArgumentNullException.ThrowIfNull(fileConfig);

    var result = new Dictionary<MetricIdentifier, IReadOnlyList<string>>();
    MergeAliases(fileConfig.MetricAliases, result);
    MergeAliases(envConfig.MetricAliases, result);
    MergeAliases(cliAliases, result);
    return result;
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

  private static void MergeAliases(
    IDictionary<string, string[]>? source,
    Dictionary<MetricIdentifier, IReadOnlyList<string>> target)
  {
    if (source is null)
    {
      return;
    }

    foreach (var (metricName, aliases) in source)
    {
      if (!Enum.TryParse<MetricIdentifier>(metricName, ignoreCase: true, out var identifier))
      {
        continue;
      }

      var normalizedAliases = NormalizeAliases(aliases);
      if (normalizedAliases.Count == 0)
      {
        target.Remove(identifier);
        continue;
      }

      target[identifier] = normalizedAliases;
    }
  }

  private static IReadOnlyList<string> NormalizeAliases(IEnumerable<string>? aliases)
  {
    if (aliases is null)
    {
      return Array.Empty<string>();
    }

    return aliases
      .Select(alias => alias?.Trim())
      .Where(alias => !string.IsNullOrWhiteSpace(alias))
      .Select(alias => alias!)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();
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

