namespace MetricsReporter.Model;

using System.Collections.Generic;

/// <summary>
/// Represents a type-level node (class, struct, record, etc.).
/// </summary>
public sealed class TypeMetricsNode : MetricsNode
{
  /// <summary>
  /// Initialises a type node.
  /// </summary>
  public TypeMetricsNode()
      => Kind = CodeElementKind.Type;

  /// <summary>
  /// Members that belong to the type.
  /// </summary>
  public IList<MemberMetricsNode> Members { get; init; } = [];
}


