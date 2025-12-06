namespace MetricsReporter.Rendering;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Builds HTML attributes for metric cells, determining metric status and coordinating all cell-level attributes.
/// Responsible for the business logic of determining metric cell state, including suppression handling,
/// status calculation, and attribute coordination.
/// </summary>
internal sealed class MetricCellAttributeBuilder
{
  private readonly Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>? _suppressedIndex;

  /// <summary>
  /// Initializes a new instance of the <see cref="MetricCellAttributeBuilder"/> class.
  /// </summary>
  /// <param name="suppressedIndex">Optional index of suppressed symbols for lookup.</param>
  public MetricCellAttributeBuilder(Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>? suppressedIndex)
  {
    _suppressedIndex = suppressedIndex;
  }

  /// <summary>
  /// Builds all HTML attributes for a metric cell, determining status and coordinating all cell-level attributes.
  /// </summary>
  /// <param name="node">The metrics node.</param>
  /// <param name="metricId">The metric identifier.</param>
  /// <param name="value">The metric value, may be <see langword="null"/>.</param>
  /// <returns>A tuple containing the status, hasDelta flag, suppressed attribute, suppression data attribute, and breakdown attribute.</returns>
  /// <remarks>
  /// This method is responsible for:
  /// - Determining the metric status (considering suppression state)
  /// - Calculating delta presence
  /// - Coordinating suppression and breakdown attributes
  /// - Validating input data and handling edge cases
  /// </remarks>
  public (string Status, bool HasDelta, string SuppressedAttr, string SuppressionDataAttr, string BreakdownAttr) BuildAttributes(
    MetricsNode node,
    MetricIdentifier metricId,
    MetricValue? value)
  {
    // WHY: Determine suppression first, as it affects status calculation logic
    // Suppressed metrics retain their original status (error/warning) but are marked as suppressed
    // This allows JavaScript to apply suppression styling while preserving the underlying severity
    var suppression = TryGetSuppression(node, metricId);
    var isSuppressed = suppression is not null;

    // WHY: Status is determined from the metric value, regardless of suppression
    // Suppression is a separate concern that affects styling but not the status value itself
    // This allows the UI to show both the original severity and the suppression state
    var status = DetermineStatus(value);

    // WHY: Delta is calculated independently of suppression
    // A suppressed metric can still have a delta, which is useful for tracking changes
    var hasDelta = CalculateHasDelta(value);

    // Build suppression-related attributes
    var suppressedAttr = isSuppressed ? " data-suppressed=\"true\"" : string.Empty;
    var suppressionDataAttr = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Build breakdown attribute for SARIF metrics
    var breakdownAttr = BreakdownAttributeBuilder.BuildDataAttribute(metricId, value);

    return (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr);
  }

  /// <summary>
  /// Determines the status string for a metric value.
  /// </summary>
  /// <param name="value">The metric value, may be <see langword="null"/>.</param>
  /// <returns>
  /// The status string: "na" if value is null, otherwise the lowercase string representation
  /// of the threshold status (e.g., "success", "warning", "error").
  /// </returns>
  private static string DetermineStatus(MetricValue? value)
  {
    if (value is null)
    {
      return "na";
    }

    return value.Status.ToString().ToLowerInvariant();
  }

  /// <summary>
  /// Calculates whether a metric value has a non-zero delta.
  /// </summary>
  /// <param name="value">The metric value, may be <see langword="null"/>.</param>
  /// <returns>
  /// <see langword="true"/> if the value has a non-zero delta, otherwise <see langword="false"/>.
  /// </returns>
  private static bool CalculateHasDelta(MetricValue? value)
  {
    if (value is null)
    {
      return false;
    }

    return value.Delta.HasValue && value.Delta.Value != 0;
  }

  /// <summary>
  /// Attempts to retrieve suppression information for a metric.
  /// </summary>
  /// <param name="node">The metrics node.</param>
  /// <param name="metric">The metric identifier.</param>
  /// <returns>
  /// The suppression information if found, otherwise <see langword="null"/>.
  /// </returns>
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
}


