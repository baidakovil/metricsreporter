namespace MetricsReporter.Rendering;

using System.Linq;
using System.Net;
using System.Text;
using MetricsReporter.Model;

/// <summary>
/// Generates HTML table header markup for the metrics report.
/// </summary>
internal static class TableHeaderGenerator
{
  /// <summary>
  /// Generates the table header HTML markup.
  /// </summary>
  /// <param name="metricOrder">The ordered list of metric identifiers.</param>
  /// <param name="builder">The string builder to append to.</param>
  public static void GenerateHeader(MetricIdentifier[] metricOrder, StringBuilder builder)
  {
    builder.AppendLine("  <thead>");
    // First header row: group labels (OpenCover, Roslyn, Sarif)
    builder.AppendLine("    <tr>");
    builder.AppendLine("      <th data-col=\"symbol\" rowspan=\"2\">Symbol</th>");
    builder.AppendLine("      <th colspan=\"4\" data-col-group=\"OpenCover\">OpenCover</th>");
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
  }
}









