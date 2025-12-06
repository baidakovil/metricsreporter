namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.Model;

internal static class SuppressedSymbolMetricBinder
{
  public static void Bind(SolutionMetricsNode solution, IList<SuppressedSymbolInfo> suppressedSymbols)
  {
    ArgumentNullException.ThrowIfNull(solution);

    if (suppressedSymbols.Count == 0)
    {
      return;
    }

    var lookup = MetricsNodeLookup.Create(solution);

    foreach (var suppressed in suppressedSymbols)
    {
      if (string.IsNullOrWhiteSpace(suppressed.FullyQualifiedName))
      {
        continue;
      }

      if (SuppressedMetricResolver.IsKnownMetric(suppressed.Metric))
      {
        continue;
      }

      if (!lookup.TryGetNode(suppressed.FullyQualifiedName, out var node))
      {
        continue;
      }

      if (SuppressedMetricResolver.TryResolve(node, suppressed.RuleId, out var metricIdentifier))
      {
        suppressed.Metric = metricIdentifier.ToString();
      }
    }
  }
}


