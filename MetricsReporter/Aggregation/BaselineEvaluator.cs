namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Applies baseline and threshold data to a metrics tree, computing deltas and statuses.
/// </summary>
internal sealed class BaselineEvaluator
{
  /// <summary>
  /// Applies the baseline metrics and thresholds to <paramref name="root"/> recursively.
  /// </summary>
  /// <param name="root">The root metrics node (solution, assembly, namespace, etc.).</param>
  /// <param name="baselineRoot">The optional baseline tree to compare against.</param>
  /// <param name="thresholds">Threshold definitions for metrics evaluation.</param>
  public void Apply(
      MetricsNode root,
      MetricsNode? baselineRoot,
      IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds)
  {
    var baselineLookup = CreateBaselineLookup(baselineRoot);
    ApplyRecursive(root, baselineLookup, thresholds, root.Name);
  }

  private static Dictionary<string, MetricsNode> CreateBaselineLookup(MetricsNode? baselineRoot)
  {
    var result = new Dictionary<string, MetricsNode>(StringComparer.Ordinal);
    if (baselineRoot is null)
    {
      return result;
    }

    TraverseBaseline(baselineRoot, baselineRoot.Name, result);
    return result;
  }

  private static void TraverseBaseline(MetricsNode node, string path, IDictionary<string, MetricsNode> lookup)
  {
    lookup[path] = node;

    foreach (var (child, childPath) in EnumerateChildren(node, path))
    {
      TraverseBaseline(child, childPath, lookup);
    }
  }

  private static IEnumerable<(MetricsNode Node, string Path)> EnumerateChildren(
      MetricsNode node,
      string path)
  {
    return node switch
    {
      SolutionMetricsNode solution => SelectChildren(solution.Assemblies, path),
      AssemblyMetricsNode assembly => SelectChildren(assembly.Namespaces, path),
      NamespaceMetricsNode @namespace => SelectChildren(@namespace.Types, path),
      TypeMetricsNode type => SelectChildren(type.Members, path),
      _ => Array.Empty<(MetricsNode Node, string Path)>()
    };
  }

  private static IEnumerable<(MetricsNode Node, string Path)> SelectChildren<TChild>(
      IEnumerable<TChild> children,
      string path)
      where TChild : MetricsNode
  {
    foreach (var child in children)
    {
      yield return (child, $"{path}/{child.Name}");
    }
  }

  private void ApplyRecursive(
      MetricsNode node,
      IReadOnlyDictionary<string, MetricsNode> baselineLookup,
      IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds,
      string path)
  {
    var context = new ApplyContext(baselineLookup, thresholds);
    ApplyRecursiveWithContext(node, context, path);
  }

  private void ApplyRecursiveWithContext(MetricsNode node, ApplyContext context, string path)
  {
    ApplyToNode(node, context, path);
    ApplyToChildren(node, context, path);
  }

  private sealed record ApplyContext(
      IReadOnlyDictionary<string, MetricsNode> BaselineLookup,
      IDictionary<MetricIdentifier, MetricThresholdDefinition> Thresholds);

  private static void ApplyToNode(MetricsNode node, ApplyContext context, string path)
  {
    context.BaselineLookup.TryGetValue(path, out var baselineNode);

    if (node is not SolutionMetricsNode)
    {
      node.IsNew = baselineNode is null;
    }

    var symbolLevel = DetermineSymbolLevel(node);
    node.Metrics = MetricsBaselineProcessor.Process(
        node.Metrics,
        baselineNode?.Metrics ?? new Dictionary<MetricIdentifier, MetricValue>(),
        context.Thresholds,
        symbolLevel);
  }

  private void ApplyToChildren(MetricsNode node, ApplyContext context, string path)
  {
    switch (node)
    {
      case SolutionMetricsNode solution:
        ApplyToAssemblies(solution, context, path);
        break;
      case AssemblyMetricsNode assembly:
        ApplyToNamespaces(assembly, context, path);
        break;
      case NamespaceMetricsNode @namespace:
        ApplyToTypes(@namespace, context, path);
        break;
      case TypeMetricsNode type:
        ApplyToMembers(type, context, path);
        break;
    }
  }

  private void ApplyToAssemblies(SolutionMetricsNode solution, ApplyContext context, string path)
  {
    foreach (var assembly in solution.Assemblies)
    {
      ApplyRecursiveWithContext(assembly, context, $"{path}/{assembly.Name}");
    }
  }

  private void ApplyToNamespaces(AssemblyMetricsNode assembly, ApplyContext context, string path)
  {
    foreach (var ns in assembly.Namespaces)
    {
      ApplyRecursiveWithContext(ns, context, $"{path}/{ns.Name}");
    }
  }

  private void ApplyToTypes(NamespaceMetricsNode @namespace, ApplyContext context, string path)
  {
    foreach (var type in @namespace.Types)
    {
      ApplyRecursiveWithContext(type, context, $"{path}/{type.Name}");
    }
  }

  private void ApplyToMembers(TypeMetricsNode type, ApplyContext context, string path)
  {
    foreach (var member in type.Members)
    {
      ApplyRecursiveWithContext(member, context, $"{path}/{member.Name}");
    }
  }


  private static MetricSymbolLevel DetermineSymbolLevel(MetricsNode node)
      => node switch
      {
        SolutionMetricsNode => MetricSymbolLevel.Solution,
        AssemblyMetricsNode => MetricSymbolLevel.Assembly,
        NamespaceMetricsNode => MetricSymbolLevel.Namespace,
        TypeMetricsNode => MetricSymbolLevel.Type,
        MemberMetricsNode => MetricSymbolLevel.Member,
        _ => MetricSymbolLevel.Member
      };
}


