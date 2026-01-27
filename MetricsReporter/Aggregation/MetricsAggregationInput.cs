namespace MetricsReporter.Aggregation;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MetricsReporter.Model;
using MetricsReporter.Processing;

/// <summary>
/// Data required to build the consolidated metrics report.
/// </summary>
public sealed class MetricsAggregationInput
{
  /// <summary>
  /// Solution name displayed in the report.
  /// </summary>
  public string SolutionName { get; init; } = "UnknownSolution";

  /// <summary>
  /// OpenCover documents.
  /// </summary>
  public IList<ParsedMetricsDocument> OpenCoverDocuments { get; init; } = [];

  /// <summary>
  /// Roslyn code metrics documents.
  /// </summary>
  public IList<ParsedMetricsDocument> RoslynDocuments { get; init; } = [];

  /// <summary>
  /// SARIF documents.
  /// </summary>
  public IList<ParsedMetricsDocument> SarifDocuments { get; init; } = [];

  /// <summary>
  /// Baseline report used to compute deltas. Can be <see langword="null"/>.
  /// </summary>
  public MetricsReport? Baseline { get; init; }

  /// <summary>
  /// Metric thresholds grouped by symbol level.
  /// </summary>
  public IDictionary<MetricIdentifier, MetricThresholdDefinition> Thresholds { get; init; }
      = new Dictionary<MetricIdentifier, MetricThresholdDefinition>();

  /// <summary>
  /// Paths to the key artefacts.
  /// </summary>
  public ReportPaths Paths { get; init; } = new();

  /// <summary>
  /// Optional textual description of the baseline (for example, git commit hash).
  /// </summary>
  public string? BaselineReference { get; init; }

  /// <summary>
  /// Optional collection of suppressed symbol entries that should be attached to
  /// the resulting report metadata.
  /// </summary>
  /// <remarks>
  /// Aggregation itself does not change metric values based on suppression; instead
  /// this information is propagated into <see cref="ReportMetadata.SuppressedSymbols"/>
  /// so that the HTML renderer and external tools can adjust visualisation and
  /// tooling behaviour without altering the underlying metrics.
  /// </remarks>
  [SuppressMessage(
      "Style",
      "IDE0028:Collection initialization can be simplified",
      Justification = "The property must stay a concrete List<T> for serialization and downstream consumers, and we initialize it in the constructor rather than via inline collection syntax.")]
  public List<SuppressedSymbolInfo> SuppressedSymbols { get; init; }

  /// <summary>
  /// Metric alias mappings keyed by canonical identifier.
  /// </summary>
  public IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> MetricAliases { get; init; }
    = new Dictionary<MetricIdentifier, IReadOnlyList<string>>();

  /// <summary>
  /// Initializes a new instance of <see cref="MetricsAggregationInput"/>.
  /// </summary>
  public MetricsAggregationInput()
  {
    SuppressedSymbols = new List<SuppressedSymbolInfo>();
  }
}


