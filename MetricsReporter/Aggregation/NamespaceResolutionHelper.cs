namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides helper methods that infer namespaces for fully qualified type names.
/// </summary>
internal static class NamespaceResolutionHelper
{
  /// <summary>
  /// Attempts to find the longest namespace prefix that is already known in the namespace index.
  /// </summary>
  /// <param name="typeFqn">The fully qualified type name.</param>
  /// <param name="namespaceIndex">The namespace index built during aggregation.</param>
  /// <returns>The matching namespace or <see langword="null"/> if nothing matches.</returns>
  public static string? FindKnownNamespace(
      string? typeFqn,
      IReadOnlyDictionary<string, List<NamespaceEntry>> namespaceIndex)
  {
    if (string.IsNullOrWhiteSpace(typeFqn) || namespaceIndex.Count == 0)
    {
      return null;
    }

    var searchValue = typeFqn;
    var lastDot = searchValue.LastIndexOf('.');
    while (lastDot > 0)
    {
      var candidate = searchValue[..lastDot];
      if (namespaceIndex.ContainsKey(candidate))
      {
        return candidate;
      }

      lastDot = candidate.LastIndexOf('.');
    }

    return null;
  }

  /// <summary>
  /// Extracts the namespace portion from a fully qualified type name.
  /// </summary>
  /// <param name="typeFqn">The fully qualified type name.</param>
  /// <returns>The namespace part or "&lt;global&gt;" when the type is declared in the global namespace.</returns>
  public static string ExtractNamespaceFromTypeFqn(string typeFqn)
  {
    if (string.IsNullOrWhiteSpace(typeFqn))
    {
      return "<global>";
    }

    var lastDot = typeFqn.LastIndexOf('.');
    return lastDot <= 0 ? "<global>" : typeFqn[..lastDot];
  }
}


