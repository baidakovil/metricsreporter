namespace MetricsReporter.Rendering;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MetricsReporter.Model;
/// <summary>
/// Builds indices for metrics report processing.
/// </summary>
internal static class IndexBuilder
{
  /// <summary>
  /// Builds an index mapping suppressed symbols to their suppression information.
  /// </summary>
  /// <param name="report">The metrics report containing suppressed symbols metadata.</param>
  /// <returns>Dictionary mapping (FQN, Metric) tuples to suppression information.</returns>
  public static Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo> BuildSuppressedIndex(MetricsReport report)
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
  /// <summary>
  /// Builds an index mapping metrics nodes to their descendant counts.
  /// </summary>
  /// <param name="report">The metrics report.</param>
  /// <returns>Dictionary mapping nodes to their descendant counts.</returns>
  public static Dictionary<MetricsNode, int> BuildDescendantCountIndex(MetricsReport report)
  {
    var index = new Dictionary<MetricsNode, int>(MetricsNodeReferenceComparer.Instance);
    if (report.Solution is MetricsNode root)
    {
      PopulateDescendantCounts(root, index);
    }
    return index;
  }
  private static int PopulateDescendantCounts(MetricsNode node, IDictionary<MetricsNode, int> index)
  {
    var total = 0;
    foreach (var child in EnumerateChildren(node))
    {
      var childDescendants = PopulateDescendantCounts(child, index);
      total += 1 + childDescendants;
    }
    index[node] = total;
    return total;
  }
  private static IEnumerable<MetricsNode> EnumerateChildren(MetricsNode node)
  {
    switch (node)
    {
      case SolutionMetricsNode solution when solution.Assemblies is not null:
        foreach (var assembly in solution.Assemblies)
        {
          yield return assembly;
        }
        break;
      case AssemblyMetricsNode assembly when assembly.Namespaces is not null:
        foreach (var ns in assembly.Namespaces)
        {
          yield return ns;
        }
        break;
      case NamespaceMetricsNode @namespace when @namespace.Types is not null:
        foreach (var type in @namespace.Types)
        {
          yield return type;
        }
        break;
      case TypeMetricsNode type when type.Members is not null:
        foreach (var member in type.Members)
        {
          yield return member;
        }
        break;
    }
  }
  private sealed class MetricsNodeReferenceComparer : IEqualityComparer<MetricsNode>
  {
    public static MetricsNodeReferenceComparer Instance { get; } = new();
    public bool Equals(MetricsNode? x, MetricsNode? y)
      => ReferenceEquals(x, y);
    public int GetHashCode(MetricsNode obj)
      => RuntimeHelpers.GetHashCode(obj);
  }
}






