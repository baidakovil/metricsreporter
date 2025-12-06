namespace MetricsReporter.Rendering;

using System.Net;
using System.Text;
using MetricsReporter.Model;

/// <summary>
/// Renders metric values as HTML markup.
/// </summary>
internal static class MetricValueRenderer
{
  /// <summary>
  /// Renders a metric value as HTML, including the value, delta (if any), and appropriate styling.
  /// </summary>
  /// <param name="value">The metric value to render. Can be <see langword="null"/>.</param>
  /// <returns>HTML markup for the metric value.</returns>
  public static string Render(MetricValue? value, string? unit)
  {
    if (value is null)
    {
      return "<span class=\"metric-value\">-</span>";
    }

    var displayValue = value.Value.HasValue
        ? FormatValue(value.Value.Value, unit)
        : "-";

    var builder = new StringBuilder();
    builder.Append($"<span class=\"metric-value\">{WebUtility.HtmlEncode(displayValue)}</span>");

    if (value.Delta.HasValue && value.Delta.Value != 0)
    {
      var deltaText = FormatDelta(value.Delta.Value, unit);
      // WHY: JavaScript applies correct colors (delta-improving/delta-degrading) based on
      // higherIsBetter flag from threshold data. Temporary classes are used only to mark
      // delta elements for JavaScript processing.
      var deltaClass = value.Delta.Value >= 0 ? "delta-positive" : "delta-negative";
      builder.Append($"<sup class=\"{deltaClass}\">{WebUtility.HtmlEncode(deltaText)}</sup>");
    }

    return builder.ToString();
  }

  /// <summary>
  /// Formats a numeric value with the appropriate unit.
  /// </summary>
  /// <param name="value">The numeric value.</param>
  /// <param name="unit">The unit (e.g., "percent").</param>
  /// <returns>Formatted string representation of the value.</returns>
  private static string FormatValue(decimal value, string? unit)
      => unit switch
      {
        "percent" => $"{value:0}%",
        _ => $"{value:0.##}"
      };

  /// <summary>
  /// Formats a delta value with the appropriate unit and sign.
  /// </summary>
  /// <param name="delta">The delta value (can be positive or negative).</param>
  /// <param name="unit">The unit (e.g., "percent").</param>
  /// <returns>Formatted string representation of the delta with sign prefix.</returns>
  private static string FormatDelta(decimal delta, string? unit)
  {
    var formattedValue = unit switch
    {
      "percent" => $"{delta:0}%",
      _ => $"{delta:0.##}"
    };

    return delta > 0 ? $"+{formattedValue}" : formattedValue;
  }
}

