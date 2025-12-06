namespace MetricsReporter.Aggregation;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MetricsReporter.Model;

/// <summary>
/// Evaluates metric values against threshold definitions to determine status.
/// </summary>
internal static class ThresholdEvaluator
{
  /// <summary>
  /// Evaluates the threshold status for a metric value.
  /// </summary>
  /// <param name="identifier">The metric identifier.</param>
  /// <param name="value">The metric value to evaluate.</param>
  /// <param name="thresholds">Threshold definitions.</param>
  /// <param name="symbolLevel">Symbol level for threshold evaluation.</param>
  /// <returns>The threshold status.</returns>
  public static ThresholdStatus Evaluate(
      MetricIdentifier identifier,
      decimal? value,
      IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds,
      MetricSymbolLevel symbolLevel)
  {
    if (!value.HasValue)
    {
      return ThresholdStatus.NotApplicable;
    }

    if (!thresholds.TryGetValue(identifier, out var definition))
    {
      return ThresholdStatus.Success;
    }

    if (!TryGetThresholdForLevel(definition.Levels, symbolLevel, out var threshold))
    {
      return ThresholdStatus.Success;
    }

    if (!threshold.Warning.HasValue && !threshold.Error.HasValue)
    {
      return ThresholdStatus.Success;
    }

    return threshold.HigherIsBetter
        ? EvaluateHigherIsBetter(value.Value, threshold)
        : EvaluateLowerIsBetter(value.Value, threshold);
  }

  private static bool TryGetThresholdForLevel(
      IDictionary<MetricSymbolLevel, MetricThreshold> levels,
      MetricSymbolLevel requestedLevel,
      [NotNullWhen(true)] out MetricThreshold? threshold)
  {
    if (levels.TryGetValue(requestedLevel, out var foundThreshold))
    {
      threshold = foundThreshold;
      return true;
    }

    if (levels.TryGetValue(MetricSymbolLevel.Type, out foundThreshold))
    {
      threshold = foundThreshold;
      return true;
    }

    threshold = null;
    return false;
  }

  private static ThresholdStatus EvaluateHigherIsBetter(decimal value, MetricThreshold threshold)
  {
    if (threshold.Error.HasValue && value < threshold.Error)
    {
      return ThresholdStatus.Error;
    }

    if (threshold.Warning.HasValue && value < threshold.Warning)
    {
      return ThresholdStatus.Warning;
    }

    return ThresholdStatus.Success;
  }

  private static ThresholdStatus EvaluateLowerIsBetter(decimal value, MetricThreshold threshold)
  {
    if (threshold.Error.HasValue && value > threshold.Error)
    {
      return ThresholdStatus.Error;
    }

    if (threshold.Warning.HasValue && value > threshold.Warning)
    {
      return ThresholdStatus.Warning;
    }

    return ThresholdStatus.Success;
  }
}


