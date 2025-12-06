namespace MetricsReporter.Rendering;

using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.Model;

/// <summary>
/// Provides sorting functionality for metrics node collections.
/// </summary>
/// <remarks>
/// This class encapsulates the logic for sorting different types of node collections,
/// reducing coupling in rendering classes.
/// </remarks>
internal static class NodeSorter
{
  /// <summary>
  /// Sorts assemblies by name in a case-insensitive manner.
  /// </summary>
  /// <param name="assemblies">The assemblies to sort.</param>
  /// <returns>The sorted assemblies.</returns>
  public static IEnumerable<AssemblyMetricsNode> SortAssemblies(IEnumerable<AssemblyMetricsNode> assemblies)
  {
    return assemblies.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Sorts namespaces by name in a case-insensitive manner.
  /// </summary>
  /// <param name="namespaces">The namespaces to sort.</param>
  /// <returns>The sorted namespaces.</returns>
  public static IEnumerable<NamespaceMetricsNode> SortNamespaces(IEnumerable<NamespaceMetricsNode> namespaces)
  {
    return namespaces.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Sorts types by name in a case-insensitive manner.
  /// </summary>
  /// <param name="types">The types to sort.</param>
  /// <returns>The sorted types.</returns>
  public static IEnumerable<TypeMetricsNode> SortTypes(IEnumerable<TypeMetricsNode> types)
  {
    return types.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Sorts members by name in a case-insensitive manner.
  /// </summary>
  /// <param name="members">The members to sort.</param>
  /// <returns>The sorted members.</returns>
  public static IEnumerable<MemberMetricsNode> SortMembers(IEnumerable<MemberMetricsNode> members)
  {
    return members.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase);
  }
}


