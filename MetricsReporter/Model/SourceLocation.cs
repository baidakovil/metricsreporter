namespace MetricsReporter.Model;

/// <summary>
/// Describes the source file and line range associated with a metrics node.
/// </summary>
public sealed class SourceLocation
{
  /// <summary>
  /// File path relative to the solution root.
  /// </summary>
  public string? Path { get; init; }

  /// <summary>
  /// First line covered by the node, or <see langword="null"/> when the information is not available.
  /// </summary>
  public int? StartLine { get; init; }

  /// <summary>
  /// Last line covered by the node, or <see langword="null"/> when the information is not available.
  /// </summary>
  public int? EndLine { get; init; }
}


