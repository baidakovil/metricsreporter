using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Parses metric alias mappings supplied via CLI options.
/// </summary>
internal static class MetricAliasParser
{
  /// <summary>
  /// Parses a JSON object of the form { "MetricId": ["alias1","alias2"] } into a dictionary.
  /// </summary>
  /// <param name="payload">Raw JSON provided via CLI.</param>
  /// <returns>Dictionary keyed by metric identifier with arrays of alias strings, or <see langword="null"/> when input is null/empty.</returns>
  /// <exception cref="ArgumentException">Thrown when the JSON cannot be parsed or does not match the expected shape.</exception>
  public static Dictionary<string, string[]>? Parse(string? payload)
  {
    if (string.IsNullOrWhiteSpace(payload))
    {
      return null;
    }

    try
    {
      return ParsePayload(payload);
    }
    catch (JsonException ex)
    {
      throw new ArgumentException($"Invalid metricAliases JSON: {ex.Message}", ex);
    }
  }

  private static Dictionary<string, string[]>? ParsePayload(string payload)
  {
    using var document = JsonDocument.Parse(payload);
    var root = document.RootElement;
    EnsureObjectRoot(root);

    var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
    foreach (var property in root.EnumerateObject())
    {
      result[property.Name] = ParseAliases(property);
    }

    return result.Count == 0 ? null : result;
  }

  private static void EnsureObjectRoot(JsonElement root)
  {
    if (root.ValueKind != JsonValueKind.Object)
    {
      throw new ArgumentException("Metric aliases must be a JSON object.");
    }
  }

  private static string[] ParseAliases(JsonProperty property)
  {
    if (property.Value.ValueKind != JsonValueKind.Array)
    {
      throw new ArgumentException($"metricAliases.{property.Name} must be an array.");
    }

    var aliases = new List<string>();
    foreach (var item in property.Value.EnumerateArray())
    {
      aliases.Add(ParseAliasValue(property.Name, item));
    }

    if (aliases.Count == 0)
    {
      throw new ArgumentException($"metricAliases.{property.Name} must be a non-empty array of strings.");
    }

    return aliases.ToArray();
  }

  private static string ParseAliasValue(string propertyName, JsonElement item)
  {
    if (item.ValueKind != JsonValueKind.String)
    {
      throw new ArgumentException($"metricAliases.{propertyName} must contain only strings.");
    }

    var alias = item.GetString();
    if (string.IsNullOrWhiteSpace(alias))
    {
      throw new ArgumentException($"metricAliases.{propertyName} must not contain empty strings.");
    }

    return alias.Trim();
  }
}


