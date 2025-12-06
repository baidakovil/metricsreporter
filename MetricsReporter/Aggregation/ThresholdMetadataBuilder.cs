namespace MetricsReporter.Aggregation;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Converts threshold definitions into metadata structures needed by reports.
/// </summary>
internal static class ThresholdMetadataBuilder
{
  /// <summary>
  /// Builds metadata dictionaries from the provided threshold definitions.
  /// </summary>
  /// <param name="thresholds">Threshold definitions to convert.</param>
  /// <returns>
  /// A tuple containing:
  /// <list type="bullet">
  /// <item><description>Threshold levels grouped by metric identifier</description></item>
  /// <item><description>Metric descriptions</description></item>
  /// </list>
  /// </returns>
  public static (Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>> ThresholdsByLevel,
      Dictionary<MetricIdentifier, string?> Descriptions) Build(
      IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds)
  {
    var perLevelResult = new Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>>();
    var descriptions = new Dictionary<MetricIdentifier, string?>();

    foreach (var (identifier, definition) in thresholds)
    {
      descriptions[identifier] = definition.Description;
      perLevelResult[identifier] = CloneThresholdLevels(definition.Levels);
    }

    return (perLevelResult, descriptions);
  }

  private static Dictionary<MetricSymbolLevel, MetricThreshold> CloneThresholdLevels(
      IDictionary<MetricSymbolLevel, MetricThreshold> levels)
  {
    var clonedLevels = new Dictionary<MetricSymbolLevel, MetricThreshold>();
    foreach (var (level, threshold) in levels)
    {
      clonedLevels[level] = CloneThreshold(threshold);
    }

    return clonedLevels;
  }

  private static MetricThreshold CloneThreshold(MetricThreshold threshold)
      => new()
      {
        Warning = threshold.Warning,
        Error = threshold.Error,
        HigherIsBetter = threshold.HigherIsBetter,
        PositiveDeltaNeutral = threshold.PositiveDeltaNeutral
      };
}


