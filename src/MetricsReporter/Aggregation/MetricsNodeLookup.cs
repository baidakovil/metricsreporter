namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MetricsReporter.Model;

internal sealed class MetricsNodeLookup
{
  private readonly Dictionary<string, MetricsNode> _index;

  private MetricsNodeLookup(Dictionary<string, MetricsNode> index)
  {
    _index = index ?? throw new ArgumentNullException(nameof(index));
  }

  public static MetricsNodeLookup Create(SolutionMetricsNode solution)
  {
    ArgumentNullException.ThrowIfNull(solution);

    var index = new Dictionary<string, MetricsNode>(StringComparer.Ordinal);

    void AddNode(MetricsNode? node)
    {
      if (node is null || string.IsNullOrWhiteSpace(node.FullyQualifiedName))
      {
        return;
      }

      index[node.FullyQualifiedName] = node;
    }

    foreach (var assembly in solution.Assemblies)
    {
      AddNode(assembly);
      foreach (var ns in assembly.Namespaces)
      {
        AddNode(ns);
        foreach (var type in ns.Types)
        {
          AddNode(type);
          foreach (var member in type.Members)
          {
            AddNode(member);
          }
        }
      }
    }

    return new MetricsNodeLookup(index);
  }

  public bool TryGetNode(string fullyQualifiedName, [NotNullWhen(true)] out MetricsNode? node)
  {
    if (string.IsNullOrWhiteSpace(fullyQualifiedName))
    {
      node = null;
      return false;
    }

    if (_index.TryGetValue(fullyQualifiedName, out var foundNode))
    {
      node = foundNode;
      return true;
    }

    node = null;
    return false;
  }
}


