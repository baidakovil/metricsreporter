namespace MetricsReporter.Rendering;
using System.Collections.Generic;
using MetricsReporter.Model;
/// <summary>
/// Encapsulates context and dependencies for table generation.
/// </summary>
internal sealed class TableGeneratorContext
{
  /// <summary>
  /// Gets the ordered list of metric identifiers.
  /// </summary>
  public MetricIdentifier[] MetricOrder { get; }
  /// <summary>
  /// Gets the dictionary mapping metric identifiers to their units.
  /// </summary>
  public IReadOnlyDictionary<MetricIdentifier, string?> MetricUnits { get; }
  /// <summary>
  /// Gets the dictionary mapping (FQN, Metric) tuples to suppression information.
  /// </summary>
  public Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>? SuppressedIndex { get; }
  /// <summary>
  /// Gets the dictionary mapping nodes to their descendant counts.
  /// </summary>
  public Dictionary<MetricsNode, int>? DescendantCountIndex { get; }
  /// <summary>
  /// Gets the optional coverage link builder.
  /// </summary>
  public CoverageLinkBuilder? CoverageLinkBuilder { get; }
  /// <summary>
  /// Initializes a new instance of the <see cref="TableGeneratorContext"/> class.
  /// </summary>
  /// <param name="metricOrder">The ordered list of metric identifiers.</param>
  /// <param name="metricUnits">Dictionary mapping metric identifiers to their units.</param>
  /// <param name="suppressedIndex">Dictionary mapping (FQN, Metric) tuples to suppression information.</param>
  /// <param name="descendantCountIndex">Dictionary mapping nodes to their descendant counts.</param>
  /// <param name="coverageLinkBuilder">Optional coverage link builder.</param>
  public TableGeneratorContext(
      MetricIdentifier[] metricOrder,
      IReadOnlyDictionary<MetricIdentifier, string?> metricUnits,
      Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>? suppressedIndex,
      Dictionary<MetricsNode, int>? descendantCountIndex,
      CoverageLinkBuilder? coverageLinkBuilder)
  {
    MetricOrder = metricOrder;
    MetricUnits = metricUnits;
    SuppressedIndex = suppressedIndex;
    DescendantCountIndex = descendantCountIndex;
    CoverageLinkBuilder = coverageLinkBuilder;
  }
}






