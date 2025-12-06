namespace MetricsReporter.Model;

using System.Collections.Generic;

/// <summary>
/// Represents an assembly-level (MSBuild project) node.
/// </summary>
public sealed class AssemblyMetricsNode : MetricsNode
{
  /// <summary>
  /// Initialises a new assembly node.
  /// </summary>
  public AssemblyMetricsNode()
      => Kind = CodeElementKind.Assembly;

  /// <summary>
  /// Namespaces contained inside the assembly.
  /// </summary>
  public IList<NamespaceMetricsNode> Namespaces { get; init; } = [];
}


