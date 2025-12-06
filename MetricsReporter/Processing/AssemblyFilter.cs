namespace MetricsReporter.Processing;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Filters out assemblies from metrics reports based on name patterns.
/// </summary>
/// <remarks>
/// This filter excludes assemblies whose names contain any of the specified exclusion patterns.
/// The filter supports multiple exclusion patterns separated by commas or semicolons.
/// Matching is case-insensitive and checks if the assembly name contains the pattern.
/// </remarks>
public sealed class AssemblyFilter
{
  private readonly HashSet<string> _excludedPatterns;

  /// <summary>
  /// Initializes a new instance of the <see cref="AssemblyFilter"/> class with no exclusions.
  /// </summary>
  public AssemblyFilter()
      : this(new HashSet<string>(StringComparer.OrdinalIgnoreCase))
  {
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="AssemblyFilter"/> class with the specified exclusion patterns.
  /// </summary>
  /// <param name="excludedPatterns">The set of patterns to exclude. Cannot be null.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="excludedPatterns"/> is null.</exception>
  public AssemblyFilter(HashSet<string> excludedPatterns)
  {
    ArgumentNullException.ThrowIfNull(excludedPatterns);
    _excludedPatterns = new HashSet<string>(excludedPatterns, StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Determines whether an assembly should be excluded from metrics reports.
  /// </summary>
  /// <param name="assemblyName">The assembly name to check.</param>
  /// <returns>
  /// <see langword="true"/> if the assembly should be excluded from the report; otherwise, <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// This method checks if the assembly name contains any of the exclusion patterns.
  /// Matching is case-insensitive. Returns <see langword="false"/> if the assembly name is null or empty.
  /// </remarks>
  public bool ShouldExcludeAssembly(string? assemblyName)
  {
    if (string.IsNullOrWhiteSpace(assemblyName))
    {
      return false;
    }

    return _excludedPatterns.Any(pattern => assemblyName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Creates an <see cref="AssemblyFilter"/> instance from a comma-separated or semicolon-separated string of exclusion patterns.
  /// </summary>
  /// <param name="excludedAssemblyNamesString">
  /// A string containing exclusion patterns separated by commas or semicolons (e.g., "Tests,Test" or "Tests;Test").
  /// Whitespace around patterns is trimmed. Empty or null string returns a filter with no exclusions.
  /// </param>
  /// <returns>
  /// An <see cref="AssemblyFilter"/> instance configured with the specified patterns, or an empty filter if the string is empty or null.
  /// </returns>
  /// <remarks>
  /// This method is useful for parsing exclusion patterns from configuration files or command-line arguments.
  /// Patterns are matched case-insensitively against assembly names using substring matching.
  /// </remarks>
  public static AssemblyFilter FromString(string? excludedAssemblyNamesString)
  {
    if (string.IsNullOrWhiteSpace(excludedAssemblyNamesString))
    {
      return new AssemblyFilter();
    }

    var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var separators = new[] { ',', ';' };
    var parts = excludedAssemblyNamesString.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    foreach (var part in parts)
    {
      if (!string.IsNullOrWhiteSpace(part))
      {
        patterns.Add(part);
      }
    }

    return new AssemblyFilter(patterns);
  }

  /// <summary>
  /// Gets a comma-separated string of excluded assembly name patterns.
  /// </summary>
  /// <returns>
  /// A comma-separated string of excluded assembly name patterns, or an empty string if no patterns are excluded.
  /// </returns>
  /// <remarks>
  /// This method returns the list of excluded assembly name patterns in a format suitable for display.
  /// The patterns are sorted alphabetically for consistent output.
  /// </remarks>
  public string GetExcludedAssemblyPatternsString()
  {
    if (_excludedPatterns.Count == 0)
    {
      return string.Empty;
    }

    var sortedPatterns = _excludedPatterns.OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
    return string.Join(", ", sortedPatterns);
  }
}


