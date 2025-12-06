namespace MetricsReporter.Configuration;

using System;
using System.Collections.Generic;
using System.Text.Json;
using MetricsReporter.Model;

/// <summary>
/// Processes symbol-level thresholds from JSON and applies them to threshold definitions.
/// </summary>
/// <remarks>
/// This class encapsulates the logic for parsing and applying symbol thresholds,
/// reducing coupling in the main parser class.
/// </remarks>
internal static class SymbolThresholdProcessor
{
  /// <summary>
  /// Symbol levels supported by the thresholds document.
  /// </summary>
  private static readonly MetricSymbolLevel[] SupportedLevels =
  {
    MetricSymbolLevel.Solution,
    MetricSymbolLevel.Assembly,
    MetricSymbolLevel.Namespace,
    MetricSymbolLevel.Type,
    MetricSymbolLevel.Member
  };

  /// <summary>
  /// Applies symbol-level thresholds from a JSON element to a threshold definition.
  /// </summary>
  /// <param name="metricElement">The JSON element containing symbol thresholds.</param>
  /// <param name="definition">The threshold definition to update.</param>
  /// <param name="createThreshold">Function to create threshold values from warning/error values.</param>
  public static void ApplySymbolThresholds(
      JsonElement metricElement,
      MetricThresholdDefinition definition,
      Func<decimal?, decimal?, MetricThreshold> createThreshold)
  {
    ProcessSymbolThresholdsFromJson(metricElement, definition, createThreshold);
    EnsureAllLevelsPresent(definition, createThreshold);
  }

  private static void ProcessSymbolThresholdsFromJson(
      JsonElement metricElement,
      MetricThresholdDefinition definition,
      Func<decimal?, decimal?, MetricThreshold> createThreshold)
  {
    if (!metricElement.TryGetProperty("symbolThresholds", out var symbolThresholdsElement) ||
        symbolThresholdsElement.ValueKind != JsonValueKind.Object)
    {
      return;
    }

    foreach (var property in symbolThresholdsElement.EnumerateObject())
    {
      ProcessSymbolThresholdProperty(property, definition, createThreshold);
    }
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Microsoft.Maintainability",
      "CA1506:AvoidExcessiveClassCoupling",
      Justification = "This method processes a single JSON property to extract symbol threshold values. The coupling is inherent to its responsibility of coordinating JSON parsing, level parsing, decimal reading, and threshold creation. Further reduction would require dummy wrapper methods that would harm readability.")]
  private static void ProcessSymbolThresholdProperty(
      System.Text.Json.JsonProperty property,
      MetricThresholdDefinition definition,
      Func<decimal?, decimal?, MetricThreshold> createThreshold)
  {
    if (!TryParseSymbolLevel(property.Name, out var level) ||
        property.Value.ValueKind != JsonValueKind.Object)
    {
      return;
    }

    var warning = ReadNullableDecimal(property.Value, "warning", ReadDecimalValue);
    var error = ReadNullableDecimal(property.Value, "error", ReadDecimalValue);
    definition.Levels[level] = createThreshold(warning, error);
  }

  private static void EnsureAllLevelsPresent(
      MetricThresholdDefinition definition,
      Func<decimal?, decimal?, MetricThreshold> createThreshold)
  {
    foreach (var level in SupportedLevels)
    {
      if (definition.Levels.TryGetValue(level, out var existing))
      {
        definition.Levels[level] = createThreshold(existing.Warning, existing.Error);
      }
      else
      {
        definition.Levels[level] = createThreshold(null, null);
      }
    }
  }

  /// <summary>
  /// Attempts to parse a symbol level from a string value.
  /// </summary>
  /// <param name="value">The string value to parse.</param>
  /// <param name="level">When this method returns, contains the parsed level if successful.</param>
  /// <returns><see langword="true"/> if parsing was successful; otherwise, <see langword="false"/>.</returns>
  private static bool TryParseSymbolLevel(ReadOnlySpan<char> value, out MetricSymbolLevel level)
  {
    foreach (var candidate in Enum.GetValues<MetricSymbolLevel>())
    {
      if (value.Equals(candidate.ToString(), StringComparison.OrdinalIgnoreCase))
      {
        level = candidate;
        return true;
      }
    }

    level = default;
    return false;
  }

  /// <summary>
  /// Reads a decimal value from a JSON element.
  /// </summary>
  /// <param name="element">The JSON element to read from.</param>
  /// <returns>The decimal value, or <see langword="null"/> if not a valid number.</returns>
  private static decimal? ReadDecimalValue(JsonElement element)
  {
    if (element.ValueKind == JsonValueKind.Null)
    {
      return null;
    }

    if (element.ValueKind == JsonValueKind.Number)
    {
      return element.GetDecimal();
    }

    return null;
  }

  /// <summary>
  /// Reads a nullable decimal value from a JSON element.
  /// </summary>
  /// <param name="parent">The parent JSON element.</param>
  /// <param name="propertyName">The name of the property to read.</param>
  /// <param name="readDecimal">Function to read decimal values from JSON elements.</param>
  /// <returns>The decimal value, or <see langword="null"/> if not found or invalid.</returns>
  private static decimal? ReadNullableDecimal(JsonElement parent, string propertyName, Func<JsonElement, decimal?> readDecimal)
  {
    if (!parent.TryGetProperty(propertyName, out var property))
    {
      return null;
    }

    return readDecimal(property);
  }
}


