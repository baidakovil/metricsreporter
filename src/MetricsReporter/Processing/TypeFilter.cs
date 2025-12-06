namespace MetricsReporter.Processing;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Filters out types from metrics reports based on name patterns.
/// </summary>
/// <remarks>
/// This filter excludes types whose fully qualified names match any of the specified exclusion patterns.
/// The filter supports multiple exclusion patterns separated by commas or semicolons and wildcard characters
/// <c>*</c> (any sequence) and <c>?</c> (single character). Matching is case-sensitive.
/// </remarks>
public sealed class TypeFilter
{
  private readonly NamePatternSet _patterns;

  /// <summary>
  /// Initializes a new instance of the <see cref="TypeFilter"/> class with no exclusions.
  /// </summary>
  public TypeFilter()
      : this(NamePatternSet.Empty)
  {
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="TypeFilter"/> class with the specified exclusion patterns.
  /// </summary>
  /// <param name="patterns">The pattern set to use for exclusions. Cannot be null.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="patterns"/> is null.</exception>
  public TypeFilter(NamePatternSet patterns)
  {
    ArgumentNullException.ThrowIfNull(patterns);
    _patterns = patterns;
  }

  /// <summary>
  /// Determines whether a type should be excluded from metrics reports.
  /// </summary>
  /// <param name="typeNameOrFqn">The type name or fully qualified name to check.</param>
  /// <returns>
  /// <see langword="true"/> if the type should be excluded from the report; otherwise, <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// This method checks if the provided name matches any of the configured exclusion patterns.
  /// Matching is case-sensitive and supports wildcard patterns. Returns <see langword="false"/> if the name is null or empty.
  /// </remarks>
  public bool ShouldExcludeType(string? typeNameOrFqn)
  {
    if (string.IsNullOrWhiteSpace(typeNameOrFqn))
    {
      return false;
    }

    return _patterns.IsMatch(typeNameOrFqn);
  }

  /// <summary>
  /// Creates a <see cref="TypeFilter"/> instance from a comma-separated or semicolon-separated string of exclusion patterns.
  /// </summary>
  /// <param name="excludedTypeNamePatterns">
  /// A string containing exclusion patterns separated by commas or semicolons (e.g., "&lt;&gt;c,__DisplayClass").
  /// Whitespace around patterns is trimmed. Empty or null string returns a filter with no exclusions.
  /// </param>
  /// <returns>
  /// A <see cref="TypeFilter"/> instance configured with the specified patterns, or an empty filter if the string is empty or null.
  /// </returns>
  /// <remarks>
  /// This method is useful for parsing exclusion patterns from configuration files or command-line arguments.
  /// Patterns are matched case-sensitively against fully qualified type names using wildcard matching.
  /// </remarks>
  public static TypeFilter FromString(string? excludedTypeNamePatterns)
  {
    var patterns = NamePatternSet.FromString(excludedTypeNamePatterns, plainTextIsExactMatch: false);
    return new TypeFilter(patterns);
  }

  /// <summary>
  /// Gets a comma-separated string of excluded type name patterns.
  /// </summary>
  /// <returns>
  /// A comma-separated string of excluded type name patterns, or an empty string if no patterns are excluded.
  /// </returns>
  /// <remarks>
  /// This method returns the list of excluded type name patterns in a format suitable for display.
  /// The patterns are sorted alphabetically for consistent output.
  /// </remarks>
  public string GetExcludedTypePatternsString()
  {
    var rawPatterns = _patterns.RawPatterns;
    if (rawPatterns.Count == 0)
    {
      return string.Empty;
    }

    var sortedPatterns = rawPatterns.OrderBy(x => x, StringComparer.Ordinal);
    return string.Join(", ", sortedPatterns);
  }
}



