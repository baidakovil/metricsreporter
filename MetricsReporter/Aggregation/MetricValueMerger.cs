namespace MetricsReporter.Aggregation;

using System.Collections.Generic;
using MetricsReporter.Model;
using MetricsReporter.Processing;

/// <summary>
/// Handles merging metric values and source locations for structural metrics nodes.
/// </summary>
internal static class MetricValueMerger
{
  /// <summary>
  /// Merges metrics and source location from a parsed element into an existing node.
  /// </summary>
  /// <param name="node">The target metrics node.</param>
  /// <param name="element">The parsed element whose metrics and source to merge.</param>
  internal static void MergeElement(MetricsNode node, ParsedCodeElement element)
  {
    MergeMetrics(node.Metrics, element.Metrics);
    MergeSource(node, element.Source);
  }

  /// <summary>
  /// Merges metrics from a source dictionary into a target dictionary.
  /// </summary>
  /// <param name="target">The target dictionary to merge into.</param>
  /// <param name="source">The source dictionary to merge from.</param>
  internal static void MergeMetrics(IDictionary<MetricIdentifier, MetricValue> target, IDictionary<MetricIdentifier, MetricValue> source)
  {
    foreach (var pair in source)
    {
      if (target.TryGetValue(pair.Key, out var existing))
      {
        MergeExistingMetric(target, pair.Key, existing, pair.Value);
      }
      else
      {
        AddNewMetric(target, pair.Key, pair.Value);
      }
    }
  }

  /// <summary>
  /// Merges an incoming source location into a metrics node when appropriate.
  /// </summary>
  /// <param name="node">The node whose source location may be updated.</param>
  /// <param name="source">The incoming source location.</param>
  internal static void MergeSource(MetricsNode node, SourceLocation? source)
  {
    if (source is null)
    {
      return;
    }

    if (ShouldReplaceSource(node.Source, source))
    {
      node.Source = source;
    }
  }

  private static void MergeExistingMetric(
    IDictionary<MetricIdentifier, MetricValue> target,
    MetricIdentifier key,
    MetricValue existing,
    MetricValue incoming)
  {
    if (IsAggregatableMetric(key) && incoming.Value.HasValue)
    {
      AggregateMetricValue(target, key, existing, incoming);
    }
    else if (!existing.Value.HasValue && incoming.Value.HasValue)
    {
      ReplaceNullMetricValue(target, key, incoming);
    }
  }

  private static void AggregateMetricValue(
    IDictionary<MetricIdentifier, MetricValue> target,
    MetricIdentifier key,
    MetricValue existing,
    MetricValue incoming)
  {
    var sum = (existing.Value ?? 0m) + incoming.Value!.Value;

    // WHY: We merge breakdown dictionaries when aggregating metrics to preserve
    // the detailed breakdown of rule violations. This is especially important for
    // SARIF metrics where we want to track individual rule IDs across the hierarchy.
    var mergedBreakdown = SarifBreakdownHelper.Merge(existing.Breakdown, incoming.Breakdown);

    target[key] = new MetricValue
    {
      Value = sum,
      Status = ThresholdStatus.NotApplicable,
      Breakdown = mergedBreakdown
    };
  }

  private static void ReplaceNullMetricValue(
    IDictionary<MetricIdentifier, MetricValue> target,
    MetricIdentifier key,
    MetricValue incoming)
  {
    // WHY: When replacing a null value with a real value, we preserve the breakdown
    // from the incoming value to ensure SARIF breakdown information is not lost.
    // We create a new MetricValue to ensure the breakdown dictionary is properly copied.
    target[key] = new MetricValue
    {
      Value = incoming.Value,
      Delta = incoming.Delta,
      Status = incoming.Status,
      Breakdown = SarifBreakdownHelper.Clone(incoming.Breakdown)
    };
  }

  private static void AddNewMetric(
    IDictionary<MetricIdentifier, MetricValue> target,
    MetricIdentifier key,
    MetricValue value)
  {
    // WHY: When adding a metric for the first time, we preserve the breakdown if present.
    // This ensures that SARIF metrics with breakdown information are correctly stored
    // even on the first assignment. We create a new MetricValue to ensure the breakdown
    // dictionary is properly copied.
    target[key] = new MetricValue
    {
      Value = value.Value,
      Delta = value.Delta,
      Status = value.Status,
      Breakdown = SarifBreakdownHelper.Clone(value.Breakdown)
    };
  }

  private static bool ShouldReplaceSource(SourceLocation? existing, SourceLocation incoming)
  {
    if (existing is null)
    {
      return true;
    }

    if (!existing.StartLine.HasValue && incoming.StartLine.HasValue)
    {
      return true;
    }

    return existing.StartLine.HasValue
        && incoming.StartLine.HasValue
        && incoming.EndLine.HasValue
        && !existing.EndLine.HasValue;
  }

  private static bool IsAggregatableMetric(MetricIdentifier identifier)
      => identifier is MetricIdentifier.SarifCaRuleViolations or MetricIdentifier.SarifIdeRuleViolations;
}
