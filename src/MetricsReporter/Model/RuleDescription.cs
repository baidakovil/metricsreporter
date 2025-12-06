namespace MetricsReporter.Model;

/// <summary>
/// Describes a code analysis rule extracted from SARIF files.
/// Contains metadata about the rule such as descriptions, help URI, and category.
/// </summary>
public sealed class RuleDescription
{
  /// <summary>
  /// Short description of the rule.
  /// </summary>
  public string ShortDescription { get; init; } = string.Empty;

  /// <summary>
  /// Full detailed description of the rule. May be <see langword="null"/> if not provided.
  /// </summary>
  public string? FullDescription { get; init; }

  /// <summary>
  /// URI to the help documentation for this rule. May be <see langword="null"/> if not provided.
  /// </summary>
  public string? HelpUri { get; init; }

  /// <summary>
  /// Category of the rule (e.g., "Design", "Performance", "Security"). May be <see langword="null"/> if not provided.
  /// </summary>
  public string? Category { get; init; }
}


