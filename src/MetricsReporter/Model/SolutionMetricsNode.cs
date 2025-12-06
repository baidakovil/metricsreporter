namespace MetricsReporter.Model;

using System.Collections.Generic;

/// <summary>
/// Root node that aggregates metrics for the entire solution.
/// </summary>
public sealed class SolutionMetricsNode : MetricsNode
{
  /// <summary>
  /// Initialises the solution node.
  /// </summary>
  public SolutionMetricsNode()
      => Kind = CodeElementKind.Solution;

  /// <summary>
  /// Assemblies included in the report.
  /// </summary>
  public IList<AssemblyMetricsNode> Assemblies { get; init; } = [];
}


