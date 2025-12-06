namespace MetricsReporter.Rendering;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MetricsReporter.Model;

/// <summary>
/// Builds an index for efficient lookup of descendant counts for metrics nodes.
/// </summary>
internal static class DescendantCountIndexBuilder
{
  /// <summary>
  /// Builds a dictionary index mapping metrics nodes to their descendant counts.
  /// </summary>
  /// <param name="report">The metrics report containing the solution node hierarchy.</param>
  /// <returns>
  /// A dictionary keyed by node reference (using reference equality) with descendant counts as values.
  /// </returns>
  public static Dictionary<MetricsNode, int> Build(MetricsReport report)
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

  private static System.Collections.Generic.IEnumerable<MetricsNode> EnumerateChildren(MetricsNode node)
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


