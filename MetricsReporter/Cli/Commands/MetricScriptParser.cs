using System;
using System.Collections.Generic;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Parses metric script mappings from CLI inputs.
/// </summary>
internal static class MetricScriptParser
{
  /// <summary>
  /// Parses metric-to-script mappings in the form <c>metric=path</c>.
  /// </summary>
  /// <param name="inputs">Raw CLI inputs.</param>
  /// <param name="separators">Separators used to split metric and path.</param>
  /// <returns>List of parsed metric/path pairs.</returns>
  public static List<(string Metric, string Path)> Parse(IEnumerable<string> inputs, char[] separators)
  {
    var result = new List<(string Metric, string Path)>();
    foreach (var input in inputs)
    {
      if (string.IsNullOrWhiteSpace(input))
      {
        continue;
      }

      var parts = input.Split(separators, 2, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length != 2)
      {
        continue;
      }

      var metric = parts[0].Trim();
      var path = parts[1].Trim();
      if (metric.Length == 0 || path.Length == 0)
      {
        continue;
      }

      result.Add((metric, path));
    }

    return result;
  }
}

