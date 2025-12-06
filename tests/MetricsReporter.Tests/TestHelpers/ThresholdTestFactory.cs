namespace MetricsReporter.Tests.TestHelpers;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Provides helper methods for constructing threshold dictionaries in tests.
/// </summary>
internal static class ThresholdTestFactory
{
  public static IDictionary<MetricIdentifier, MetricThresholdDefinition> CreateDefinitions(
      params (MetricIdentifier Metric, decimal? Warning, decimal? Error, bool HigherIsBetter, string? Description)[] entries)
  {
    var result = new Dictionary<MetricIdentifier, MetricThresholdDefinition>();
    foreach (var (metric, warning, error, higherIsBetter, description) in entries)
    {
      result[metric] = CreateDefinition(warning, error, higherIsBetter, description);
    }

    return result;
  }

  public static IDictionary<MetricIdentifier, MetricThresholdDefinition> CreateUniformThresholds(
      params (MetricIdentifier Metric, decimal? Warning, decimal? Error, bool HigherIsBetter)[] entries)
  {
    var result = new Dictionary<MetricIdentifier, MetricThresholdDefinition>();
    foreach (var (metric, warning, error, higherIsBetter) in entries)
    {
      result[metric] = CreateDefinition(warning, error, higherIsBetter);
    }

    return result;
  }

  public static MetricThresholdDefinition CreateDefinition(
      decimal? warning,
      decimal? error,
      bool higherIsBetter,
      string? description = null)
      => new()
      {
        Description = description,
        Levels = CreateUniformThresholdSet(warning, error, higherIsBetter)
      };

  public static IDictionary<MetricSymbolLevel, MetricThreshold> CreateUniformThresholdSet(
      decimal? warning,
      decimal? error,
      bool higherIsBetter)
  {
    return new Dictionary<MetricSymbolLevel, MetricThreshold>
    {
      [MetricSymbolLevel.Solution] = CreateThreshold(warning, error, higherIsBetter),
      [MetricSymbolLevel.Assembly] = CreateThreshold(warning, error, higherIsBetter),
      [MetricSymbolLevel.Namespace] = CreateThreshold(warning, error, higherIsBetter),
      [MetricSymbolLevel.Type] = CreateThreshold(warning, error, higherIsBetter),
      [MetricSymbolLevel.Member] = CreateThreshold(warning, error, higherIsBetter)
    };
  }

  public static MetricThreshold CreateThreshold(
      decimal? warning,
      decimal? error,
      bool higherIsBetter,
      bool positiveDeltaNeutral = false)
      => new()
      {
        Warning = warning,
        Error = error,
        HigherIsBetter = higherIsBetter,
        PositiveDeltaNeutral = positiveDeltaNeutral
      };
}



