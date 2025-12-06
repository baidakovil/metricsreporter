namespace MetricsReporter.Model;

using System;
using System.Collections.Generic;

/// <summary>
/// Metadata attached to the metrics report.
/// </summary>
public sealed class ReportMetadata
{
  /// <summary>
  /// Report generation timestamp in UTC.
  /// </summary>
  public DateTime GeneratedAtUtc { get; init; }
      = DateTime.UtcNow;

  /// <summary>
  /// Optional reference describing the baseline source (for example, git commit).
  /// </summary>
  public string? BaselineReference { get; init; }
      = null;

  /// <summary>
  /// Paths to the main artefacts.
  /// </summary>
  public ReportPaths Paths { get; init; } = new();

  /// <summary>
  /// Threshold definitions grouped by symbol level.
  /// </summary>
  public IDictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>> ThresholdsByLevel { get; init; }
      = new Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>>();

  /// <summary>
  /// Metric descriptions sourced from the thresholds definition.
  /// </summary>
  public IDictionary<MetricIdentifier, string?> ThresholdDescriptions { get; init; }
      = new Dictionary<MetricIdentifier, string?>();

  /// <summary>
  /// Descriptor metadata (unit of measurement etc.) for each metric.
  /// </summary>
  public IDictionary<MetricIdentifier, MetricDescriptor> MetricDescriptors { get; init; }
      = new Dictionary<MetricIdentifier, MetricDescriptor>();

  /// <summary>
  /// Comma-separated list of excluded member name patterns used when generating this report.
  /// </summary>
  /// <remarks>
  /// This property stores the list of member name patterns that were excluded from the metrics report.
  /// It is used for display purposes in the HTML report header.
  /// </remarks>
  public string? ExcludedMemberNamesPatterns { get; init; }

  /// <summary>
  /// Comma-separated list of excluded assembly name patterns used when generating this report.
  /// </summary>
  /// <remarks>
  /// This property stores the list of assembly name patterns that were excluded from the metrics report.
  /// It is used for display purposes in the HTML report header.
  /// </remarks>
  public string? ExcludedAssemblyNames { get; init; }

  /// <summary>
  /// Comma-separated list of type name patterns that were excluded from the metrics report.
  /// </summary>
  /// <remarks>
  /// Patterns are matched against fully qualified type names using substring matching.
  /// This property is used for display purposes in the HTML report header.
  /// </remarks>
  public string? ExcludedTypeNamePatterns { get; init; }

  /// <summary>
  /// Optional collection of suppressed symbol entries that were taken into account
  /// when computing this report.
  /// </summary>
  /// <remarks>
  /// Each entry originates from a <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>
  /// and is keyed by normalized fully qualified name and metric identifier. The HTML
  /// renderer uses this information to visually distinguish metrics that are suppressed
  /// (for example, by rendering them in a light azure color) and to surface the
  /// justification text as a tooltip on hover without re-running analysis.
  /// </remarks>
  public IList<SuppressedSymbolInfo> SuppressedSymbols { get; init; } = [];

  /// <summary>
  /// Rule descriptions extracted from SARIF files.
  /// Keyed by rule ID (e.g., "CA1502", "IDE0051").
  /// </summary>
  /// <remarks>
  /// This dictionary contains metadata about code analysis rules that appear in the report.
  /// Descriptions are extracted from SARIF files during parsing and are used to provide
  /// context about rule violations when displaying breakdown information. Each rule ID
  /// maps to its description, help URI, and category information.
  /// </remarks>
  public IDictionary<string, RuleDescription> RuleDescriptions { get; init; }
      = new Dictionary<string, RuleDescription>();
}


