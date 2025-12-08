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
      using var document = JsonDocument.Parse(payload);
      var root = document.RootElement;
      if (root.ValueKind != JsonValueKind.Object)
      {
        throw new ArgumentException("Metric aliases must be a JSON object.");
      }

      var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
      foreach (var property in root.EnumerateObject())
      {
        if (property.Value.ValueKind != JsonValueKind.Array)
        {
          throw new ArgumentException($"metricAliases.{property.Name} must be an array.");
        }

        var aliases = new List<string>();
        foreach (var item in property.Value.EnumerateArray())
        {
          if (item.ValueKind != JsonValueKind.String)
          {
            throw new ArgumentException($"metricAliases.{property.Name} must contain only strings.");
          }

          var alias = item.GetString();
          if (string.IsNullOrWhiteSpace(alias))
          {
            throw new ArgumentException($"metricAliases.{property.Name} must not contain empty strings.");
          }

          aliases.Add(alias.Trim());
        }

        if (aliases.Count == 0)
        {
          throw new ArgumentException($"metricAliases.{property.Name} must be a non-empty array of strings.");
        }

        result[property.Name] = aliases.ToArray();
      }

      return result.Count == 0 ? null : result;
    }
    catch (JsonException ex)
    {
      throw new ArgumentException($"Invalid metricAliases JSON: {ex.Message}", ex);
    }
  }
}


