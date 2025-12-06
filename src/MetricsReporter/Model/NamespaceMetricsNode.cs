namespace MetricsReporter.Model;

using System.Collections.Generic;

/// <summary>
/// Represents a namespace-level node.
/// </summary>
public sealed class NamespaceMetricsNode : MetricsNode
{
  /// <summary>
  /// Initialises a namespace node.
  /// </summary>
  public NamespaceMetricsNode()
      => Kind = CodeElementKind.Namespace;

  /// <summary>
  /// Collection of types that belong to the namespace.
  /// </summary>
  public IList<TypeMetricsNode> Types { get; init; } = [];
}


