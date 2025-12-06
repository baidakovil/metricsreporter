namespace MetricsReporter.Model;

using System.Collections.Generic;

/// <summary>
/// Describes a metric threshold definition including descriptive text and per-level threshold values.
/// </summary>
public sealed class MetricThresholdDefinition
{
  /// <summary>
  /// Human-readable description of the metric purpose.
  /// </summary>
  public string? Description { get; init; }
      = null;

  /// <summary>
  /// Threshold values grouped by symbol level.
  /// </summary>
  public IDictionary<MetricSymbolLevel, MetricThreshold> Levels { get; init; }
      = new Dictionary<MetricSymbolLevel, MetricThreshold>();
}



