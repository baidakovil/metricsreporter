namespace MetricsReporter.MetricsReader.Services;

using MetricsReporter.Model;

/// <summary>
/// Builds symbol metric snapshots from metrics nodes.
/// </summary>
internal interface ISymbolSnapshotBuilder
{
  /// <summary>
  /// Builds a snapshot from a metrics node for a specific metric.
  /// </summary>
  /// <param name="node">The metrics node to build a snapshot from.</param>
  /// <param name="metric">The metric identifier to build the snapshot for.</param>
  /// <returns>A snapshot if the node has the metric; otherwise, <see langword="null"/>.</returns>
  SymbolMetricSnapshot? BuildSnapshot(MetricsNode node, MetricIdentifier metric);
}


