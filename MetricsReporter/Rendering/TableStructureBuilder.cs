namespace MetricsReporter.Rendering;

using System;
using System.Linq;
using System.Net;
using System.Text;
using MetricsReporter.Model;

/// <summary>
/// Builds the HTML structure for the metrics table (actions panel, header, etc.).
/// </summary>
internal static class TableStructureBuilder
{
  /// <summary>
  /// Appends the table container and actions panel HTML to the string builder.
  /// </summary>
  /// <param name="builder">The string builder to append HTML to.</param>
  public static void AppendTableContainerAndActions(StringBuilder builder)
  {
    builder.AppendLine("<div class=\"table-container\"> ");
    // Action buttons inside table-container for proper sticky positioning
    builder.AppendLine("<div class=\"table-actions\"> ");
    builder.AppendLine("  <div class=\"status-badges\">");
    builder.AppendLine("    <span class=\"badge status-warning\">Warning</span>");
    builder.AppendLine("    <span class=\"badge status-error\">Error</span>");
    builder.AppendLine("  </div>");
    builder.AppendLine("  <div style=\"flex:1\"></div>");
    builder.AppendLine("  <div class=\"state-filters\" role=\"group\" aria-label=\"Row filters\">");
    builder.AppendLine("    <span class=\"state-filters-label\">Filter to:</span>");
    builder.AppendLine("    <label class=\"state-filter-option\">");
    builder.AppendLine("      <input type=\"checkbox\" id=\"filter-new\" aria-label=\"Show only new rows\" />");
    builder.AppendLine("      <span>new</span>");
    builder.AppendLine("    </label>");
    builder.AppendLine("    <label class=\"state-filter-option\">");
    builder.AppendLine("      <input type=\"checkbox\" id=\"filter-changes\" aria-label=\"Show only rows with metric changes\" />");
    builder.AppendLine("      <span>changes</span>");
    builder.AppendLine("    </label>");
    builder.AppendLine("    <label class=\"state-filter-option\">");
    builder.AppendLine("      <input type=\"checkbox\" id=\"filter-suppressed\" aria-label=\"Show only rows with suppressed metrics\" />");
    builder.AppendLine("      <span>suppressed</span>");
    builder.AppendLine("    </label>");
    builder.AppendLine("  </div>");
    builder.AppendLine("  <div class=\"awareness-control\">");
    builder.AppendLine("    <label for=\"awareness-level\" class=\"awareness-label\">Awareness:</label>");
    builder.AppendLine("    <input type=\"range\" id=\"awareness-level\" min=\"1\" max=\"3\" step=\"1\" value=\"1\" aria-valuemin=\"1\" aria-valuemax=\"3\" aria-valuenow=\"1\" aria-label=\"Awareness level\" />");
    builder.AppendLine("    <span id=\"awareness-label\" class=\"awareness-value\">All</span>");
    builder.AppendLine("  </div>");
    builder.AppendLine("  <div class=\"filter-control\" style=\"margin-right: 50px;\">");
    builder.AppendLine("    <div class=\"filter-input-wrapper\">");
    builder.AppendLine("      <input type=\"text\" id=\"filter-input\" class=\"filter-input\" placeholder=\"Filter:\" aria-label=\"Filter rows by name\" />");
    builder.AppendLine("      <button type=\"button\" id=\"filter-clear\" class=\"filter-clear\" aria-label=\"Clear filter\" style=\"display: none;\">×</button>");
    builder.AppendLine("    </div>");
    builder.AppendLine("  </div>");
    builder.AppendLine("  <div class=\"detail-control\">");
    builder.AppendLine("    <label for=\"detail-level\" class=\"detail-label\">Detailing:</label>");
    builder.AppendLine("    <input type=\"range\" id=\"detail-level\" min=\"1\" max=\"3\" step=\"1\" value=\"2\" aria-valuemin=\"1\" aria-valuemax=\"3\" aria-valuenow=\"2\" aria-label=\"Detail level\" />");
    builder.AppendLine("    <span id=\"detail-label\" class=\"detail-value\">Type</span>");
    builder.AppendLine("  </div>");
    builder.AppendLine("  <button id=\"expand-all\">Expand all</button>");
    builder.AppendLine("  <button id=\"collapse-all\">Collapse all</button>");
    builder.AppendLine("</div>");
  }

  /// <summary>
  /// Appends the table header HTML to the string builder.
  /// </summary>
  /// <param name="metricOrder">The order of metrics to display in columns.</param>
  /// <param name="builder">The string builder to append HTML to.</param>
  public static void AppendTableHeader(MetricIdentifier[] metricOrder, StringBuilder builder)
  {
    builder.AppendLine("<table id=\"metrics-table\" class=\"metrics stripped\"> ");
    builder.AppendLine("  <thead>");
    // First header row: group labels (AltCover, Roslyn, Sarif)
    builder.AppendLine("    <tr>");
    builder.AppendLine("      <th data-col=\"symbol\" rowspan=\"2\">Symbol</th>");
    builder.AppendLine("      <th colspan=\"4\" data-col-group=\"AltCover\">AltCover</th>");
    builder.AppendLine("      <th colspan=\"6\" data-col-group=\"Roslyn\">Roslyn</th>");
    builder.AppendLine("      <th colspan=\"2\" data-col-group=\"Sarif\">Sarif</th>");
    builder.AppendLine("    </tr>");
    // Second header row: individual metric names
    builder.AppendLine("    <tr>");
    foreach (var id in metricOrder)
    {
      builder.AppendLine($"      <th data-col=\"{id}\" data-metric-id=\"{id}\">{WebUtility.HtmlEncode(MetricDisplayNameProvider.GetDisplayName(id))}</th>");
    }
    builder.AppendLine("    </tr>");
    builder.AppendLine("  </thead>");
    builder.AppendLine("  <tbody>");
  }

  /// <summary>
  /// Appends the closing tags for the table and container.
  /// </summary>
  /// <param name="builder">The string builder to append HTML to.</param>
  public static void AppendTableClose(StringBuilder builder)
  {
    builder.AppendLine("  </tbody>");
    builder.AppendLine("</table>");
    builder.AppendLine("</div>");
  }
}


