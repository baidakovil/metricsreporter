namespace MetricsReporter.Rendering;

using System.Text;
using MetricsReporter.Model;

/// <summary>
/// Builds the complete table content (structure + body) for the metrics report.
/// </summary>
internal static class TableContentBuilder
{
  /// <summary>
  /// Builds the complete table HTML content.
  /// </summary>
  /// <param name="metricOrder">The order of metrics to display in columns.</param>
  /// <param name="report">The metrics report.</param>
  /// <param name="tableGenerator">The table generator instance for rendering rows.</param>
  /// <param name="builder">The string builder to append HTML to.</param>
  public static void Build(
    MetricIdentifier[] metricOrder,
    MetricsReport report,
    HtmlTableGenerator tableGenerator,
    StringBuilder builder)
  {
    TableStructureBuilder.AppendTableContainerAndActions(builder);
    TableStructureBuilder.AppendTableHeader(metricOrder, builder);
    tableGenerator.RenderTableBody(report, builder);
    TableStructureBuilder.AppendTableClose(builder);
  }
}


