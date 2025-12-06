namespace MetricsReporter.Processing;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Represents a compiled set of name patterns with wildcard support.
/// </summary>
/// <remarks>
/// Patterns can contain the wildcard characters <c>*</c> (matches any sequence of characters)
/// and <c>?</c> (matches a single character). When plain-text matching is enabled,
/// patterns without wildcards are treated as exact matches; otherwise they behave as substring matches.
/// </remarks>
public sealed class NamePatternSet
{
  private static readonly char[] PatternDelimiters = { ',', ';' };

  private readonly List<Pattern> _patterns;

  private NamePatternSet(List<Pattern> patterns)
  {
    _patterns = patterns;
  }

  /// <summary>
  /// Gets an empty pattern set that never matches.
  /// </summary>
  public static NamePatternSet Empty { get; } = new([]);

  /// <summary>
  /// Gets the raw pattern strings as provided by the user.
  /// </summary>
  public IReadOnlyList<string> RawPatterns => _patterns.ConvertAll(static p => p.RawPattern);

  /// <summary>
  /// Creates a <see cref="NamePatternSet"/> from a delimited pattern string.
  /// </summary>
  /// <param name="input">Delimited pattern string (comma/semicolon separated).</param>
  /// <param name="plainTextIsExactMatch">
  /// When <see langword="true"/>, patterns without wildcards are treated as exact matches;
  /// otherwise they are treated as substring matches.
  /// </param>
  /// <returns>A compiled <see cref="NamePatternSet"/>.</returns>
  public static NamePatternSet FromString(string? input, bool plainTextIsExactMatch)
  {
    if (string.IsNullOrWhiteSpace(input))
    {
      return Empty;
    }

    var parts = input.Split(
        PatternDelimiters,
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    List<Pattern> patterns = [];
    foreach (var part in parts)
    {
      if (string.IsNullOrWhiteSpace(part))
      {
        continue;
      }

      patterns.Add(Pattern.Create(part, plainTextIsExactMatch));
    }

    return patterns.Count == 0 ? Empty : new NamePatternSet(patterns);
  }

  /// <summary>
  /// Determines whether the specified candidate matches any of the configured patterns.
  /// </summary>
  /// <param name="candidate">The candidate string to test.</param>
  /// <returns>
  /// <see langword="true"/> if the candidate matches at least one pattern; otherwise, <see langword="false"/>.
  /// </returns>
  public bool IsMatch(string candidate)
  {
    if (_patterns.Count == 0 || string.IsNullOrWhiteSpace(candidate))
    {
      return false;
    }

    foreach (var pattern in _patterns)
    {
      if (pattern.IsMatch(candidate))
      {
        return true;
      }
    }

    return false;
  }

  private sealed class Pattern
  {
    public string RawPattern { get; }

    private readonly Func<string, bool> _predicate;

    private Pattern(string rawPattern, Func<string, bool> predicate)
    {
      RawPattern = rawPattern;
      _predicate = predicate;
    }

    public static Pattern Create(string rawPattern, bool plainTextIsExactMatch)
    {
      var normalized = rawPattern;
      var hasWildcard = normalized.Contains('*', StringComparison.Ordinal)
                        || normalized.Contains('?', StringComparison.Ordinal);

      if (!hasWildcard)
      {
        if (plainTextIsExactMatch)
        {
          return new Pattern(normalized, candidate => string.Equals(candidate, normalized, StringComparison.Ordinal));
        }

        return new Pattern(normalized, candidate => candidate.Contains(normalized, StringComparison.Ordinal));
      }

      var regexPattern = "^" + Regex.Escape(normalized)
          .Replace(@"\*", ".*")
          .Replace(@"\?", ".") + "$";

      var regex = new Regex(regexPattern, RegexOptions.CultureInvariant);
      return new Pattern(normalized, candidate => regex.IsMatch(candidate));
    }

    public bool IsMatch(string candidate) => _predicate(candidate);
  }
}



