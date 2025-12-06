namespace MetricsReporter.Model;

using System.Collections.Generic;

/// <summary>
/// Base class for every node inside the metrics hierarchy.
/// </summary>
public abstract class MetricsNode
{
  /// <summary>
  /// Display name of the node shown in the UI.
  /// </summary>
  public string Name { get; init; } = string.Empty;

  /// <summary>
  /// Node kind (solution, assembly, namespace, type, or member).
  /// </summary>
  public CodeElementKind Kind { get; init; }
      = CodeElementKind.Member;

  /// <summary>
  /// Fully qualified name. May be <see langword="null"/> for solution/namespace nodes.
  /// </summary>
  public string? FullyQualifiedName { get; init; }
      = null;

  /// <summary>
  /// Optional source location information, used to match SARIF entries and show hints in HTML.
  /// </summary>
  public SourceLocation? Source { get; set; }
      = null;

  /// <summary>
  /// Indicates that the node was not present in the baseline report.
  /// </summary>
  public bool IsNew { get; set; }
      = false;

  /// <summary>
  /// Collection of metric values associated with the node.
  /// </summary>
  public IDictionary<MetricIdentifier, MetricValue> Metrics { get; set; }
      = new Dictionary<MetricIdentifier, MetricValue>();
}


