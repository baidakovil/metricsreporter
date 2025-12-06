namespace MetricsReporter.Aggregation;
using System;
using System.Collections.Generic;
using MetricsReporter.Model;
/// <summary>
/// Processes metrics by applying baseline values and threshold evaluation.
/// </summary>
internal static class MetricsBaselineProcessor
{
  /// <summary>
  /// Applies baseline values and thresholds to metrics dictionary.
  /// </summary>
  /// <param name="metrics">Current metrics dictionary.</param>
  /// <param name="baselineMetrics">Baseline metrics dictionary.</param>
  /// <param name="thresholds">Threshold definitions.</param>
  /// <param name="symbolLevel">Symbol level for threshold evaluation.</param>
  /// <returns>Processed metrics dictionary with deltas and statuses.</returns>
  public static Dictionary<MetricIdentifier, MetricValue> Process(
      IDictionary<MetricIdentifier, MetricValue> metrics,
      IDictionary<MetricIdentifier, MetricValue> baselineMetrics,
      IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds,
      MetricSymbolLevel symbolLevel)
  {
    var context = CreateContext(metrics, baselineMetrics, thresholds, symbolLevel);
    return BuildResult(context);
  }
  private static ProcessingContext CreateContext(
      IDictionary<MetricIdentifier, MetricValue> metrics,
      IDictionary<MetricIdentifier, MetricValue> baselineMetrics,
      IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds,
      MetricSymbolLevel symbolLevel)
  {
    return new ProcessingContext(metrics, baselineMetrics, thresholds, symbolLevel);
  }
  private static Dictionary<MetricIdentifier, MetricValue> BuildResult(ProcessingContext context)
  {
    var result = new Dictionary<MetricIdentifier, MetricValue>();
    foreach (var identifier in GetAllMetricIdentifiers())
    {
      var metricValue = ProcessMetric(identifier, context);
      if (metricValue is not null)
      {
        result[identifier] = metricValue;
      }
    }
    return result;
  }
  private static MetricIdentifier[] GetAllMetricIdentifiers()
      => Enum.GetValues<MetricIdentifier>();
  private static MetricValue? ProcessMetric(MetricIdentifier identifier, ProcessingContext context)
  {
    var data = ExtractData(identifier, context);
    var status = ThresholdEvaluator.Evaluate(identifier, data.CurrentValue, context.Thresholds, context.SymbolLevel);
    if (status == ThresholdStatus.NotApplicable)
    {
      return null;
    }
    return CreateMetricValue(data, status);
  }
  private static MetricData ExtractData(MetricIdentifier identifier, ProcessingContext context)
  {
    context.Metrics.TryGetValue(identifier, out var current);
    context.BaselineMetrics.TryGetValue(identifier, out var baseline);
    var value = current?.Value;
    var delta = DeltaCalculator.Calculate(value, baseline?.Value);
    return new MetricData(value, delta, current?.Breakdown);
  }
  private static MetricValue CreateMetricValue(MetricData data, ThresholdStatus status)
  {
    var clonedBreakdown = SarifBreakdownHelper.Clone(data.Breakdown);
    return new MetricValue
    {
      Value = data.CurrentValue,
      Delta = data.Delta,
      Status = status,
      Breakdown = clonedBreakdown
    };
  }
  private sealed record ProcessingContext(
      IDictionary<MetricIdentifier, MetricValue> Metrics,
      IDictionary<MetricIdentifier, MetricValue> BaselineMetrics,
      IDictionary<MetricIdentifier, MetricThresholdDefinition> Thresholds,
      MetricSymbolLevel SymbolLevel);
  private sealed record MetricData(
      decimal? CurrentValue,
      decimal? Delta,
      Dictionary<string, SarifRuleBreakdownEntry>? Breakdown);
}






