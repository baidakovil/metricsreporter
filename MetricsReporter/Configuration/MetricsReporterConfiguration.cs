using System;
using System.Collections.Generic;

namespace MetricsReporter.Configuration;

/// <summary>
/// Represents the root configuration for the metricsreporter CLI.
/// </summary>
public sealed class MetricsReporterConfiguration
{
  /// <summary>
  /// Gets general settings such as verbosity, timeouts, and working directory.
  /// </summary>
  public GeneralConfiguration General { get; init; } = new();

  /// <summary>
  /// Gets path-related and aggregation settings used by commands.
  /// </summary>
  public PathsConfiguration Paths { get; init; } = new();

  /// <summary>
  /// Gets script execution settings.
  /// </summary>
  public ScriptsConfiguration Scripts { get; init; } = new();

  /// <summary>
  /// Gets optional metric alias mappings keyed by canonical <see cref="MetricsReporter.Model.MetricIdentifier"/>.
  /// </summary>
  public IDictionary<string, string[]>? MetricAliases { get; init; }
}

/// <summary>
/// Holds general (non-command-specific) configuration values.
/// </summary>
public sealed class GeneralConfiguration
{
  /// <summary>
  /// Gets a value indicating whether external scripts should run before command execution.
  /// </summary>
  public bool? RunScripts { get; init; }

  /// <summary>
  /// Gets a value indicating whether aggregation should run after scripts finish.
  /// </summary>
  public bool? AggregateAfterScripts { get; init; }

  /// <summary>
  /// Gets the verbosity level for CLI output (quiet, minimal, normal, detailed).
  /// </summary>
  public string? Verbosity { get; init; }

  /// <summary>
  /// Gets the external process timeout in seconds.
  /// </summary>
  public int? TimeoutSeconds { get; init; }

  /// <summary>
  /// Gets the working directory used for resolving relative paths and running scripts.
  /// </summary>
  public string? WorkingDirectory { get; init; }

  /// <summary>
  /// Gets the maximum number of characters from stdout/stderr to keep when logging failures.
  /// </summary>
  public int? LogTruncationLimit { get; init; }
}

/// <summary>
/// Holds path-oriented configuration as well as aggregation flags that are tied to file locations.
/// </summary>
public sealed class PathsConfiguration
{
  /// <summary>
  /// Gets the metrics working directory used by the generator (equivalent to --metrics-dir).
  /// </summary>
  public string? MetricsDir { get; init; }

  /// <summary>
  /// Gets the solution name displayed in generated reports.
  /// </summary>
  public string? SolutionName { get; init; }

  /// <summary>
  /// Gets the optional baseline reference text.
  /// </summary>
  public string? BaselineReference { get; init; }

  /// <summary>
  /// Gets the output JSON path used by the generate command.
  /// </summary>
  public string? Report { get; init; }

  /// <summary>
  /// Gets the input JSON path used by read/test commands.
  /// </summary>
  public string? ReadReport { get; init; }

  /// <summary>
  /// Gets the thresholds file path used to override defaults.
  /// </summary>
  public string? Thresholds { get; init; }

  /// <summary>
  /// Gets inline thresholds JSON payload if provided directly in configuration.
  /// </summary>
  public string? ThresholdsInline { get; init; }

  /// <summary>
  /// Gets OpenCover coverage paths.
  /// </summary>
  public IReadOnlyList<string>? OpenCover { get; init; }

  /// <summary>
  /// Gets Roslyn metrics XML paths.
  /// </summary>
  public IReadOnlyList<string>? Roslyn { get; init; }

  /// <summary>
  /// Gets SARIF file paths.
  /// </summary>
  public IReadOnlyList<string>? Sarif { get; init; }

  /// <summary>
  /// Gets the baseline JSON path.
  /// </summary>
  public string? Baseline { get; init; }

  /// <summary>
  /// Gets the HTML output path.
  /// </summary>
  public string? OutputHtml { get; init; }

  /// <summary>
  /// Gets the input JSON path used for HTML-only generation.
  /// </summary>
  public string? InputJson { get; init; }

  /// <summary>
  /// Gets the directory that hosts HTML coverage reports.
  /// </summary>
  public string? CoverageHtmlDir { get; init; }

  /// <summary>
  /// Gets the directory where archived baselines are stored.
  /// </summary>
  public string? BaselineStoragePath { get; init; }

  /// <summary>
  /// Gets the path to the suppressed symbols payload.
  /// </summary>
  public string? SuppressedSymbols { get; init; }

  /// <summary>
  /// Gets the solution directory used for suppressed-symbol scanning.
  /// </summary>
  public string? SolutionDirectory { get; init; }

  /// <summary>
  /// Gets source code folder roots for suppressed-symbol scanning.
  /// </summary>
  public IReadOnlyList<string>? SourceCodeFolders { get; init; }

  /// <summary>
  /// Gets excluded member name patterns.
  /// </summary>
  public string? ExcludedMembers { get; init; }

  /// <summary>
  /// Gets excluded assembly name patterns.
  /// </summary>
  public string? ExcludedAssemblies { get; init; }

  /// <summary>
  /// Gets excluded type name patterns.
  /// </summary>
  public string? ExcludedTypes { get; init; }

  /// <summary>
  /// Gets a value indicating whether methods should be excluded.
  /// </summary>
  public bool? ExcludeMethods { get; init; }

  /// <summary>
  /// Gets a value indicating whether properties should be excluded.
  /// </summary>
  public bool? ExcludeProperties { get; init; }

  /// <summary>
  /// Gets a value indicating whether fields should be excluded.
  /// </summary>
  public bool? ExcludeFields { get; init; }

  /// <summary>
  /// Gets a value indicating whether events should be excluded.
  /// </summary>
  public bool? ExcludeEvents { get; init; }

  /// <summary>
  /// Gets a value indicating whether suppressed symbol analysis should be performed.
  /// </summary>
  public bool? AnalyzeSuppressedSymbols { get; init; }

  /// <summary>
  /// Gets a value indicating whether the baseline should be replaced when differences are detected.
  /// </summary>
  public bool? ReplaceBaseline { get; init; }
}

/// <summary>
/// Configures pre/post command script execution.
/// </summary>
public sealed class ScriptsConfiguration
{
  /// <summary>
  /// Gets scripts executed by the generate command before aggregation starts.
  /// </summary>
  public IReadOnlyList<string>? Generate { get; init; }

  /// <summary>
  /// Gets scripts executed by read/readsarif/test commands.
  /// </summary>
  public ReadScriptsConfiguration Read { get; init; } = new();

  /// <summary>
  /// Gets scripts executed by the test command.
  /// </summary>
  public ReadScriptsConfiguration Test { get; init; } = new();
}

/// <summary>
/// Scripts executed by read-oriented commands.
/// </summary>
public sealed class ReadScriptsConfiguration
{
  /// <summary>
  /// Gets scripts that always execute for read commands regardless of the metric requested.
  /// </summary>
  public IReadOnlyList<string>? Any { get; init; }

  /// <summary>
  /// Gets scripts that execute only when specified metrics are requested.
  /// </summary>
  public IReadOnlyList<MetricScript> ByMetric { get; init; } = Array.Empty<MetricScript>();
}

/// <summary>
/// Maps scripts to the metrics that should trigger them.
/// </summary>
public sealed class MetricScript
{
  /// <summary>
  /// Gets metric identifiers that trigger the script (e.g., RoslynCyclomaticComplexity).
  /// </summary>
  public IReadOnlyList<string> Metrics { get; init; } = Array.Empty<string>();

  /// <summary>
  /// Gets the PowerShell script path.
  /// </summary>
  public string? Path { get; init; }
}

