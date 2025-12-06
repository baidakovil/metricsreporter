namespace MetricsReporter.Rendering;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MetricsReporter.Model;

/// <summary>
/// Provides helper methods for rendering node hierarchy and children.
/// </summary>
internal static class NodeHierarchyRenderer
{
  /// <summary>
  /// Determines if a node has children.
  /// </summary>
  /// <param name="node">The metrics node.</param>
  /// <returns><see langword="true"/> if the node has children, otherwise <see langword="false"/>.</returns>
  public static bool HasChildren(MetricsNode node)
      => node switch
      {
        SolutionMetricsNode s => s.Assemblies.Any(),
        AssemblyMetricsNode a => a.Namespaces.Any(),
        NamespaceMetricsNode n => n.Types.Any(),
        TypeMetricsNode t => t.Members.Any(),
        _ => false
      };

  /// <summary>
  /// Gets the role string for a node.
  /// </summary>
  /// <param name="node">The metrics node.</param>
  /// <returns>The role string (e.g., "assembly", "namespace", "type", "member").</returns>
  public static string GetNodeRole(MetricsNode node)
      => node switch
      {
        AssemblyMetricsNode => "assembly",
        NamespaceMetricsNode => "namespace",
        TypeMetricsNode => "type",
        MemberMetricsNode => "member",
        _ => "node"
      };

  /// <summary>
  /// Updates the assembly name based on the current node.
  /// </summary>
  /// <param name="node">The metrics node.</param>
  /// <param name="currentAssembly">The current assembly name.</param>
  /// <returns>The updated assembly name.</returns>
  public static string? UpdateAssemblyName(MetricsNode node, string? currentAssembly)
      => node is AssemblyMetricsNode assemblyNode ? assemblyNode.Name : currentAssembly;

  /// <summary>
  /// Updates the type name based on the current node.
  /// </summary>
  /// <param name="node">The metrics node.</param>
  /// <param name="currentType">The current type name.</param>
  /// <returns>The updated type name.</returns>
  public static string? UpdateTypeName(MetricsNode node, string? currentType)
      => node is TypeMetricsNode typeNode ? typeNode.Name : currentType;

  /// <summary>
  /// Renders children of a node in sorted order.
  /// </summary>
  /// <param name="node">The parent node.</param>
  /// <param name="level">The current hierarchy level.</param>
  /// <param name="parentId">The parent node ID.</param>
  /// <param name="builder">The string builder to append to.</param>
  /// <param name="assemblyName">The current assembly name.</param>
  /// <param name="typeName">The current type name.</param>
  /// <param name="renderNodeRows">Action to render a single node row.</param>
  public static void RenderChildren(
      MetricsNode node,
      int level,
      string parentId,
      StringBuilder builder,
      string? assemblyName,
      string? typeName,
      Action<MetricsNode, int, string?, StringBuilder, string?, string?> renderNodeRows)
  {
    switch (node)
    {
      case SolutionMetricsNode solution:
        foreach (var assembly in NodeSorter.SortAssemblies(solution.Assemblies))
        {
          renderNodeRows(assembly, level + 1, parentId, builder, assembly.Name, null);
        }
        break;
      case AssemblyMetricsNode assembly:
        foreach (var ns in NodeSorter.SortNamespaces(assembly.Namespaces))
        {
          renderNodeRows(ns, level + 1, parentId, builder, assemblyName, null);
        }
        break;
      case NamespaceMetricsNode @namespace:
        foreach (var type in NodeSorter.SortTypes(@namespace.Types))
        {
          renderNodeRows(type, level + 1, parentId, builder, assemblyName, null);
        }
        break;
      case TypeMetricsNode type:
        foreach (var member in NodeSorter.SortMembers(type.Members))
        {
          renderNodeRows(member, level + 1, parentId, builder, assemblyName, typeName);
        }
        break;
    }
  }
}





