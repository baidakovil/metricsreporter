namespace MetricsReporter.MetricsReader.Services;

using MetricsReporter.Model;

/// <summary>
/// Provides threshold metadata for metrics.
/// </summary>
internal interface IMetricsThresholdProvider
{
  /// <summary>
  /// Gets the threshold for a specific metric and symbol level.
  /// </summary>
  /// <param name="metric">The metric identifier.</param>
  /// <param name="level">The symbol level.</param>
  /// <returns>The threshold if found; otherwise, <see langword="null"/>.</returns>
  MetricThreshold? GetThreshold(MetricIdentifier metric, MetricSymbolLevel level);
}


