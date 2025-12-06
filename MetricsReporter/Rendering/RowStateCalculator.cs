namespace MetricsReporter.Rendering;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Calculates the state of a metrics row based on its metrics and suppressions.
/// </summary>
internal sealed class RowStateCalculator
{
  private readonly MetricIdentifier[] _metricOrder;
  private readonly Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>? _suppressedIndex;

  /// <summary>
  /// Initializes a new instance of the <see cref="RowStateCalculator"/> class.
  /// </summary>
  /// <param name="metricOrder">The order of metrics to check.</param>
  /// <param name="suppressedIndex">Optional index of suppressed symbols for lookup.</param>
  public RowStateCalculator(
    MetricIdentifier[] metricOrder,
    Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>? suppressedIndex)
  {
    _metricOrder = metricOrder ?? throw new System.ArgumentNullException(nameof(metricOrder));
    _suppressedIndex = suppressedIndex;
  }

  /// <summary>
  /// Calculates the state of a metrics row based on its metrics values and suppressions.
  /// </summary>
  /// <param name="node">The metrics node to calculate state for.</param>
  /// <returns>A <see cref="RowState"/> record containing error, warning, suppressed, and delta flags.</returns>
  public RowState Calculate(MetricsNode node)
  {
    var hasError = false;
    var hasWarning = false;
    var hasSuppressed = false;
    var hasDelta = false;

    foreach (var metricId in _metricOrder)
    {
      var suppression = TryGetSuppression(node, metricId);
      if (suppression is not null)
      {
        hasSuppressed = true;
        continue;
      }

      if (!node.Metrics.TryGetValue(metricId, out var metricValue) || metricValue is null)
      {
        continue;
      }

      if (!hasDelta && metricValue.Delta.HasValue && metricValue.Delta.Value != 0)
      {
        hasDelta = true;
      }

      switch (metricValue.Status)
      {
        case ThresholdStatus.Error:
          hasError = true;
          break;
        case ThresholdStatus.Warning:
          hasWarning = true;
          break;
      }

      if (hasError && hasWarning && hasSuppressed && hasDelta)
      {
        break;
      }
    }

    return new RowState(hasError, hasWarning, hasSuppressed, hasDelta);
  }

  private SuppressedSymbolInfo? TryGetSuppression(MetricsNode node, MetricIdentifier metric)
  {
    if (_suppressedIndex is null)
    {
      return null;
    }

    if (string.IsNullOrWhiteSpace(node.FullyQualifiedName))
    {
      return null;
    }

    return _suppressedIndex.TryGetValue((node.FullyQualifiedName, metric), out var info) ? info : null;
  }

  /// <summary>
  /// Represents the state flags for a metrics row.
  /// </summary>
  /// <param name="HasError">Indicates if the row has any error-level metrics.</param>
  /// <param name="HasWarning">Indicates if the row has any warning-level metrics.</param>
  /// <param name="HasSuppressed">Indicates if the row has any suppressed metrics.</param>
  /// <param name="HasDelta">Indicates if the row has any metrics with non-zero deltas.</param>
  public readonly record struct RowState(bool HasError, bool HasWarning, bool HasSuppressed, bool HasDelta);
}


