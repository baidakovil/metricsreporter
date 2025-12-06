namespace MetricsReporter.Rendering;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using MetricsReporter.Model;

/// <summary>
/// Builds HTML data attributes for metric breakdown data (SARIF violations).
/// </summary>
internal static class BreakdownAttributeBuilder
{
  private static readonly JsonSerializerOptions BreakdownSerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  /// <summary>
  /// Builds a data-breakdown attribute for SARIF metrics with non-zero values and breakdown data.
  /// </summary>
  /// <param name="metricId">The metric identifier.</param>
  /// <param name="value">The metric value, may be <see langword="null"/>.</param>
  /// <returns>
  /// A data-breakdown attribute string with JSON-encoded breakdown data, or an empty string
  /// if the metric is not a SARIF metric, has no value, or has no breakdown data.
  /// </returns>
  public static string BuildDataAttribute(MetricIdentifier metricId, MetricValue? value)
  {
    // Only add breakdown for SARIF metrics
    if (metricId != MetricIdentifier.SarifCaRuleViolations && metricId != MetricIdentifier.SarifIdeRuleViolations)
    {
      return string.Empty;
    }

    // Only add breakdown if value is non-zero and breakdown exists
    if (value is null || !value.Value.HasValue || value.Value.Value == 0 || value.Breakdown is null || value.Breakdown.Count == 0)
    {
      return string.Empty;
    }

    // Serialize breakdown to JSON and HTML-encode it
    var json = JsonSerializer.Serialize(value.Breakdown, BreakdownSerializerOptions);
    var encoded = WebUtility.HtmlEncode(json);
    return $" data-breakdown=\"{encoded}\"";
  }
}

