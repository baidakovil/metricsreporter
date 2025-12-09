namespace MetricsReporter.Model;

/// <summary>
/// Represents a member-level node.
/// </summary>
public sealed class MemberMetricsNode : MetricsNode
{
  /// <summary>
  /// Initialises a member node.
  /// </summary>
  public MemberMetricsNode()
      => Kind = CodeElementKind.Member;

  /// <summary>
  /// Gets or sets a value indicating whether this member's coverage includes
  /// data from a compiler-generated iterator state machine type (for example,
  /// a nested type with name pattern <c>&lt;Method&gt;d__N</c>).
  /// </summary>
  /// <remarks>
  /// When this flag is <see langword="true"/>, the HTML renderer will annotate the
  /// member name with a neutral indicator glyph to signal that some or all coverage
  /// values are aggregated from a nested state machine rather than the source method
  /// body itself. This makes it easier to understand where coverage originates while
  /// still keeping the UI focused on user-defined methods.
  /// </remarks>
  public bool IncludesIteratorStateMachineCoverage { get; set; }

  /// <summary>
  /// Gets or sets the member kind (method, property, field, event).
  /// </summary>
  public MemberKind MemberKind { get; set; } = MemberKind.Unknown;

  /// <summary>
  /// Gets or sets a value indicating whether this member carries SARIF violations.
  /// </summary>
  public bool HasSarifViolations { get; set; }
}


