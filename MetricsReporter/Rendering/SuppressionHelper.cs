namespace MetricsReporter.Rendering;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using MetricsReporter.Model;

/// <summary>
/// Provides helper methods for working with suppressed symbols.
/// </summary>
internal static class SuppressionHelper
{
  private static readonly JsonSerializerOptions BreakdownSerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  private static readonly string[] ParagraphSeparators = { "\r\n\r\n", "\n\n", "\r\r" };

  /// <summary>
  /// Attempts to retrieve suppression information for a node and metric.
  /// </summary>
  /// <param name="node">The metrics node.</param>
  /// <param name="metric">The metric identifier.</param>
  /// <param name="suppressedIndex">Dictionary mapping (FQN, Metric) tuples to suppression information.</param>
  /// <returns>Suppression information if found, otherwise <see langword="null"/>.</returns>
  public static SuppressedSymbolInfo? TryGetSuppression(
      MetricsNode node,
      MetricIdentifier metric,
      IReadOnlyDictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>? suppressedIndex)
  {
    if (suppressedIndex is null)
    {
      return null;
    }

    if (string.IsNullOrWhiteSpace(node.FullyQualifiedName))
    {
      return null;
    }

    return suppressedIndex.TryGetValue((node.FullyQualifiedName, metric), out var info) ? info : null;
  }

  /// <summary>
  /// Builds a data-suppression-info HTML attribute for a suppressed symbol.
  /// </summary>
  /// <param name="suppression">The suppression information, or <see langword="null"/>.</param>
  /// <returns>HTML data attribute string, or empty string if suppression is null.</returns>
  public static string BuildSuppressionDataAttribute(SuppressedSymbolInfo? suppression)
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

  /// <summary>
  /// Formats justification text for HTML display, preserving paragraph breaks.
  /// </summary>
  /// <param name="justification">The justification text, or <see langword="null"/>.</param>
  /// <returns>Formatted HTML string with paragraph breaks preserved.</returns>
  /// <remarks>
  /// Formats justification text for better readability:
  /// - Preserves paragraph breaks (split on double newlines)
  /// - Escapes HTML to prevent XSS
  /// </remarks>
  public static string FormatJustificationText(string? justification)
  {
    if (string.IsNullOrWhiteSpace(justification))
    {
      return "Suppressed via SuppressMessage.";
    }

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









