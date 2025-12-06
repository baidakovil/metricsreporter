namespace MetricsReporter.Processing;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Represents a code element discovered in a raw metrics source.
/// </summary>
public sealed class ParsedCodeElement
{
  /// <summary>
  /// Initialises a new instance of <see cref="ParsedCodeElement"/>.
  /// </summary>
  /// <param name="kind">Hierarchy level of the element.</param>
  /// <param name="name">Display name.</param>
  /// <param name="fullyQualifiedName">Fully qualified name or <see langword="null"/>.</param>
  public ParsedCodeElement(CodeElementKind kind, string name, string? fullyQualifiedName)
  {
    Kind = kind;
    Name = name;
    FullyQualifiedName = fullyQualifiedName;
  }

  /// <summary>
  /// Hierarchy level (assembly, type, member, etc.).
  /// </summary>
  public CodeElementKind Kind { get; }

  /// <summary>
  /// Display name of the element.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Fully qualified name or <see langword="null"/>.
  /// </summary>
  public string? FullyQualifiedName { get; }

  /// <summary>
  /// Fully qualified name of the parent element.
  /// </summary>
  public string? ParentFullyQualifiedName { get; init; }

  /// <summary>
  /// Fully qualified name of the assembly that contains this element, when known.
  /// </summary>
  public string? ContainingAssemblyName { get; init; }

  /// <summary>
  /// Source location (if available).
  /// </summary>
  public SourceLocation? Source { get; init; }

  /// <summary>
  /// Metrics provided by the parser for this element.
  /// </summary>
  public IDictionary<MetricIdentifier, MetricValue> Metrics { get; init; } = new Dictionary<MetricIdentifier, MetricValue>();
}


