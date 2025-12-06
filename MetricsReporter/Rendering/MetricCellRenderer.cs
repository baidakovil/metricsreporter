namespace MetricsReporter.Rendering;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Renders metric cells for a metrics node in the HTML table.
/// </summary>
internal sealed class MetricCellRenderer
{
  private readonly MetricIdentifier[] _metricOrder;
  private readonly IReadOnlyDictionary<MetricIdentifier, string?> _metricUnits;
  private readonly MetricCellAttributeBuilder _attributeBuilder;

  /// <summary>
  /// Initializes a new instance of the <see cref="MetricCellRenderer"/> class.
  /// </summary>
  /// <param name="metricOrder">The order of metrics to display in columns.</param>
  /// <param name="metricUnits">Units associated with each metric.</param>
  /// <param name="suppressedIndex">Optional index of suppressed symbols for lookup.</param>
  public MetricCellRenderer(
    MetricIdentifier[] metricOrder,
    IReadOnlyDictionary<MetricIdentifier, string?> metricUnits,
    Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>? suppressedIndex)
  {
    _metricOrder = metricOrder ?? throw new System.ArgumentNullException(nameof(metricOrder));
    _metricUnits = metricUnits ?? throw new System.ArgumentNullException(nameof(metricUnits));
    _attributeBuilder = new MetricCellAttributeBuilder(suppressedIndex);
  }

  /// <summary>
  /// Appends metric cells for the specified node to the string builder.
  /// </summary>
  /// <param name="node">The metrics node to render cells for.</param>
  /// <param name="metricTag">The HTML tag to use for metric cells (th or td).</param>
  /// <param name="builder">The string builder to append HTML to.</param>
  public void AppendCells(MetricsNode node, string metricTag, System.Text.StringBuilder builder)
  {
    foreach (var mid in _metricOrder)
    {
      node.Metrics.TryGetValue(mid, out var val);
      _metricUnits.TryGetValue(mid, out var unit);
      var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) = _attributeBuilder.BuildAttributes(node, mid, val);
      builder.AppendLine($"      <{metricTag} class=\"metric\" data-col=\"{mid}\" data-status=\"{status}\" data-has-delta=\"{(hasDelta ? "true" : "false")}\" data-metric-id=\"{mid}\"{suppressedAttr}{suppressionDataAttr}{breakdownAttr}>{MetricValueRenderer.Render(val, unit)}</{metricTag}>");
    }
  }
}

