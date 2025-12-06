namespace MetricsReporter.Rendering;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using MetricsReporter.Model;

/// <summary>
/// Builds HTML data attributes for suppressed metrics.
/// </summary>
internal static class SuppressionAttributeBuilder
{
  private static readonly JsonSerializerOptions BreakdownSerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  private static readonly string[] ParagraphSeparators = { "\r\n\r\n", "\n\n", "\r\r" };

  /// <summary>
  /// Builds a data-suppression-info attribute for a suppressed metric.
  /// </summary>
  /// <param name="suppression">The suppressed symbol information, or <see langword="null"/> if not suppressed.</param>
  /// <returns>
  /// A data-suppression-info attribute string with JSON-encoded suppression data, or an empty string
  /// if <paramref name="suppression"/> is <see langword="null"/>.
  /// </returns>
  public static string BuildDataAttribute(SuppressedSymbolInfo? suppression)
  {
    if (suppression is null)
    {
      return string.Empty;
    }

    var formattedJustification = FormatJustificationText(suppression.Justification);
    var data = new
    {
      ruleId = suppression.RuleId,
      justification = formattedJustification
    };

    var json = JsonSerializer.Serialize(data, BreakdownSerializerOptions);

    return $" data-suppression-info=\"{WebUtility.HtmlEncode(json)}\"";
  }

  private static string FormatJustificationText(string? justification)
  {
    if (string.IsNullOrWhiteSpace(justification))
    {
      return "Suppressed via SuppressMessage.";
    }

    // WHY: Format justification text for better readability:
    // - Preserve paragraph breaks (split on double newlines)
    // - Escape HTML to prevent XSS

    var text = justification.Trim();
    var parts = new List<string>();

    // Split by double newlines to preserve paragraph structure
    var paragraphs = text.Split(ParagraphSeparators, StringSplitOptions.RemoveEmptyEntries);

    foreach (var paragraph in paragraphs)
    {
      var escaped = WebUtility.HtmlEncode(paragraph.Trim());
      parts.Add(escaped);
    }

    return string.Join("<br/><br/>", parts);
  }
}


