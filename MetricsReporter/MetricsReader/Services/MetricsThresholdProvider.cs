namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Resolves threshold metadata for a specific metric and symbol level.
/// </summary>
internal sealed class MetricsThresholdProvider : IMetricsThresholdProvider
{
  private readonly IReadOnlyDictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>> _reportThresholds;
  private readonly IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>? _overrideDefinitions;

  public MetricsThresholdProvider(
    IReadOnlyDictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>> reportThresholds,
    IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>? overrideDefinitions)
  {
    _reportThresholds = reportThresholds;
    _overrideDefinitions = overrideDefinitions;
  }

  public MetricThreshold? GetThreshold(MetricIdentifier metric, MetricSymbolLevel level)
  {
    if (_overrideDefinitions is not null
      && _overrideDefinitions.TryGetValue(metric, out var overrideDefinition)
      && overrideDefinition.Levels.TryGetValue(level, out var overrideThreshold))
    {
      return overrideThreshold;
    }

    if (_reportThresholds.TryGetValue(metric, out var perLevel)
      && perLevel.TryGetValue(level, out var threshold))
    {
      return threshold;
    }

    return null;
  }
}


