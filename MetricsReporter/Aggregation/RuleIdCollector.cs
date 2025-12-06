namespace MetricsReporter.Aggregation;
using System.Collections.Generic;
using MetricsReporter.Model;
/// <summary>
/// Collects rule IDs from breakdown dictionaries in the metrics tree.
/// </summary>
internal static class RuleIdCollector
{
  /// <summary>
  /// Recursively traverses the metrics tree and collects rule IDs from breakdown dictionaries.
  /// </summary>
  /// <param name="node">The current node to process.</param>
  /// <param name="usedRuleIds">The set to accumulate rule IDs into.</param>
  public static void CollectRecursive(MetricsNode node, HashSet<string> usedRuleIds)
  {
    CollectFromNode(node, usedRuleIds);
    CollectFromChildren(node, usedRuleIds);
  }
  private static void CollectFromNode(MetricsNode node, HashSet<string> usedRuleIds)
  {
    CollectFromMetric(node.Metrics, MetricIdentifier.SarifCaRuleViolations, usedRuleIds);
    CollectFromMetric(node.Metrics, MetricIdentifier.SarifIdeRuleViolations, usedRuleIds);
  }
  private static void CollectFromMetric(
      IDictionary<MetricIdentifier, MetricValue> metrics,
      MetricIdentifier metricIdentifier,
      HashSet<string> usedRuleIds)
  {
    if (metrics.TryGetValue(metricIdentifier, out var metric) && metric.Breakdown is not null)
    {
      foreach (var ruleId in metric.Breakdown.Keys)
      {
        usedRuleIds.Add(ruleId);
      }
    }
  }
  private static void CollectFromChildren(MetricsNode node, HashSet<string> usedRuleIds)
  {
    switch (node)
    {
      case SolutionMetricsNode solutionNode:
        CollectFromAssemblies(solutionNode, usedRuleIds);
        break;
      case AssemblyMetricsNode assemblyNode:
        CollectFromNamespaces(assemblyNode, usedRuleIds);
        break;
      case NamespaceMetricsNode namespaceNode:
        CollectFromTypes(namespaceNode, usedRuleIds);
        break;
      case TypeMetricsNode typeNode:
        CollectFromMembers(typeNode, usedRuleIds);
        break;
    }
  }
  private static void CollectFromAssemblies(SolutionMetricsNode solutionNode, HashSet<string> usedRuleIds)
  {
    foreach (var assembly in solutionNode.Assemblies)
    {
      CollectRecursive(assembly, usedRuleIds);
    }
  }
  private static void CollectFromNamespaces(AssemblyMetricsNode assemblyNode, HashSet<string> usedRuleIds)
  {
    foreach (var ns in assemblyNode.Namespaces)
    {
      CollectRecursive(ns, usedRuleIds);
    }
  }
  private static void CollectFromTypes(NamespaceMetricsNode namespaceNode, HashSet<string> usedRuleIds)
  {
    foreach (var type in namespaceNode.Types)
    {
      CollectRecursive(type, usedRuleIds);
    }
  }
  private static void CollectFromMembers(TypeMetricsNode typeNode, HashSet<string> usedRuleIds)
  {
    foreach (var member in typeNode.Members)
    {
      CollectRecursive(member, usedRuleIds);
    }
  }
}






