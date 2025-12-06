namespace MetricsReporter.Rendering;

using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.Model;

/// <summary>
/// Provides ordering functionality for metrics nodes.
/// </summary>
internal static class NodeOrderer
{
  /// <summary>
  /// Gets ordered assemblies from a solution node.
  /// </summary>
  /// <param name="solution">The solution metrics node.</param>
  /// <returns>Ordered enumerable of assembly nodes.</returns>
  public static IEnumerable<AssemblyMetricsNode> GetOrderedAssemblies(SolutionMetricsNode solution)
    => solution.Assemblies.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Gets ordered namespaces from an assembly node.
  /// </summary>
  /// <param name="assembly">The assembly metrics node.</param>
  /// <returns>Ordered enumerable of namespace nodes.</returns>
  public static IEnumerable<NamespaceMetricsNode> GetOrderedNamespaces(AssemblyMetricsNode assembly)
    => assembly.Namespaces.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Gets ordered types from a namespace node.
  /// </summary>
  /// <param name="namespace">The namespace metrics node.</param>
  /// <returns>Ordered enumerable of type nodes.</returns>
  public static IEnumerable<TypeMetricsNode> GetOrderedTypes(NamespaceMetricsNode @namespace)
    => @namespace.Types.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Gets ordered members from a type node.
  /// </summary>
  /// <param name="type">The type metrics node.</param>
  /// <returns>Ordered enumerable of member nodes.</returns>
  public static IEnumerable<MemberMetricsNode> GetOrderedMembers(TypeMetricsNode type)
    => type.Members.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase);
}


