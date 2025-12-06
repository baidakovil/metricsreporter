namespace MetricsReporter.Rendering;

using MetricsReporter.Model;

/// <summary>
/// Provides human-readable node kind descriptions for metrics nodes.
/// </summary>
internal static class NodeKindProvider
{
  /// <summary>
  /// Gets the human-readable kind description for the specified metrics node.
  /// </summary>
  /// <param name="node">The metrics node.</param>
  /// <returns>The kind description (e.g., "Solution", "Assembly", "Namespace", "Type", "Member").</returns>
  public static string GetKind(MetricsNode node)
      => node switch
      {
        SolutionMetricsNode _ => "Solution",
        AssemblyMetricsNode _ => "Assembly",
        NamespaceMetricsNode _ => "Namespace",
        TypeMetricsNode _ => "Type",
        _ => "Member"
      };
}

