namespace MetricsReporter.Rendering;

using System;
using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Builds an index for efficient lookup of suppressed symbols by fully qualified name and metric.
/// </summary>
internal static class SuppressionIndexBuilder
{
  /// <summary>
  /// Builds a dictionary index mapping (FQN, Metric) pairs to suppressed symbol information.
  /// </summary>
  /// <param name="report">The metrics report containing suppressed symbols metadata.</param>
  /// <returns>
  /// A dictionary keyed by (FQN, MetricIdentifier) tuples, or an empty dictionary if no
  /// suppressed symbols are present. Last-in-wins semantics apply for duplicate keys.
  /// </returns>
  public static Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo> Build(MetricsReport report)
  {
    var result = new Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>();
    foreach (var entry in report.Metadata.SuppressedSymbols)
    {
      if (string.IsNullOrWhiteSpace(entry.FullyQualifiedName) || string.IsNullOrWhiteSpace(entry.Metric))
      {
        continue;
      }

      if (!Enum.TryParse<MetricIdentifier>(entry.Metric, out var metricIdentifier))
      {
        continue;
      }

      var key = (entry.FullyQualifiedName, metricIdentifier);
      // Last-in-wins is acceptable here: multiple suppressions for the same
      // symbol/metric pair are rare and the most recent justification is likely
      // the one users care about.
      result[key] = entry;
    }

    return result;
  }
}


