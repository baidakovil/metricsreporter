namespace MetricsReporter.Processing;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Filters out compiler-generated and constructor methods from metrics reports.
/// </summary>
/// <remarks>
/// This filter excludes methods that are not relevant for code quality metrics:
/// - Constructors (.ctor, .cctor) - they are typically boilerplate and don't represent meaningful code complexity.
/// - Compiler-generated methods (MoveNext, SetStateMachine, MoveNextAsync, DisposeAsync) - these are generated
///   by the compiler for async/await state machines and enumerators, and don't represent actual user code.
/// 
/// The list of excluded methods can be configured via MSBuild property ExcludedMemberNamesPatterns.
/// Patterns support <c>*</c> and <c>?</c> wildcards. Pattern strings without wildcards are treated
/// as exact method names (for example, <c>ctor</c> matches only <c>ctor</c>, not <c>OrderConstructor</c>).
/// Default values are provided if no configuration is supplied.
/// </remarks>
public sealed class MemberFilter
{
  private readonly NamePatternSet _patterns;
  private readonly HashSet<string> _exactNames;

  /// <summary>
  /// Initializes a new instance of the <see cref="MemberFilter"/> class with no predefined exclusions.
  /// </summary>
  public MemberFilter()
      : this(NamePatternSet.Empty)
  {
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="MemberFilter"/> class with the specified excluded method patterns.
  /// </summary>
  /// <param name="patterns">The pattern set describing methods that should be excluded. Cannot be null.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="patterns"/> is null.</exception>
  public MemberFilter(NamePatternSet patterns)
  {
    ArgumentNullException.ThrowIfNull(patterns);
    _patterns = patterns;
    _exactNames = new HashSet<string>(
      patterns.RawPatterns.Where(p => !p.Contains('*') && !p.Contains('?')),
      StringComparer.Ordinal);
  }

  /// <summary>
  /// Gets a value indicating whether any exclusion patterns are configured.
  /// </summary>
  public bool HasPatterns => _patterns.RawPatterns.Count > 0;

  /// <summary>
  /// Determines whether a method should be excluded from metrics reports based on its simple name.
  /// </summary>
  /// <param name="methodName">The normalized method name (e.g., "ctor", "MoveNext", "DoWork").</param>
  /// <returns>
  /// <see langword="true"/> if the method should be excluded from the report; otherwise, <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// This method checks if the method name is in the excluded set. The method name should be
  /// normalized (e.g., ".ctor" should be passed as "ctor" without the leading dot).
  /// </remarks>
  public bool ShouldExcludeMethod(string? methodName)
  {
    if (string.IsNullOrWhiteSpace(methodName))
    {
      return false;
    }

    // Handle constructor names with leading dot (e.g., ".ctor" -> "ctor")
    var normalizedName = methodName.StartsWith('.')
        ? methodName[1..]
        : methodName;

    if (_exactNames.Contains(normalizedName))
    {
      return true;
    }

    return _patterns.IsMatch(normalizedName);
  }

  /// <summary>
  /// Determines whether a method should be excluded from metrics reports based on its fully qualified name.
  /// </summary>
  /// <param name="fullyQualifiedMethodName">
  /// The fully qualified method name (e.g., "Namespace.Type.Method(...)", "Namespace.Type..ctor(...)").
  /// </param>
  /// <returns>
  /// <see langword="true"/> if the method should be excluded from the report; otherwise, <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// This method extracts the method name from the fully qualified name and checks if it should be excluded.
  /// It handles normalized FQN format where parameters are replaced with "...".
  /// It also handles Roslyn-style constructors where the method name matches the type name.
  /// </remarks>
  public bool ShouldExcludeMethodByFqn(string? fullyQualifiedMethodName)
  {
    if (string.IsNullOrWhiteSpace(fullyQualifiedMethodName))
    {
      return false;
    }

    // Extract method name from FQN
    // Format: "Namespace.Type.Method(...)" or "Namespace.Type..ctor(...)"
    var methodName = SymbolNormalizer.ExtractMethodName(fullyQualifiedMethodName);

    // Check if method name is in the excluded set
    if (ShouldExcludeMethod(methodName))
    {
      return true;
    }

    // Check if this is a Roslyn-style constructor (method name matches type name)
    // Format: "Namespace.Type.Type(...)" where the last "Type" before "(" is the method name
    // This happens when Roslyn represents constructors as "TypeName.TypeName(...)"
    var typeName = ExtractTypeNameFromFqn(fullyQualifiedMethodName);
    if (!string.IsNullOrWhiteSpace(typeName) && !string.IsNullOrWhiteSpace(methodName))
    {
      // In Roslyn format, constructors have the pattern "TypeName.TypeName(...)"
      // where the method name (after the last dot before parameters) matches the type name
      // So if methodName == typeName, it's a constructor and should be excluded
      if (string.Equals(methodName, typeName, StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Creates a <see cref="MemberFilter"/> instance from a comma-separated or semicolon-separated string of method patterns.
  /// </summary>
  /// <param name="excludedMemberNamesPatterns">
  /// A string containing method name patterns separated by commas or semicolons (e.g., "ctor,cctor,*b__*").
  /// Whitespace around names is trimmed. Empty or null string returns a filter with default excluded methods.
  /// </param>
  /// <returns>
  /// A <see cref="MemberFilter"/> instance configured with the specified patterns, or default excluded methods if the string is empty or null.
  /// </returns>
  /// <remarks>
  /// This method is useful for parsing method patterns from configuration files or command-line arguments.
  /// Method names are normalized when evaluated (leading dots are removed, e.g., ".ctor" becomes "ctor").
  /// </remarks>
  public static MemberFilter FromString(string? excludedMemberNamesPatterns)
  {
    if (string.IsNullOrWhiteSpace(excludedMemberNamesPatterns))
    {
      return new MemberFilter();
    }

    // Normalize patterns by removing leading dots when present (e.g., ".ctor" -> "ctor").
    var normalizedParts = new List<string>();
    var separators = new[] { ',', ';' };
    var parts = excludedMemberNamesPatterns.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    foreach (var part in parts)
    {
      if (string.IsNullOrWhiteSpace(part))
      {
        continue;
      }

      var normalized = part.StartsWith('.') ? part[1..] : part;
      if (!string.IsNullOrWhiteSpace(normalized))
      {
        normalizedParts.Add(normalized);
      }
    }

    var normalizedString = string.Join(",", normalizedParts);
    var patternSet = string.IsNullOrWhiteSpace(normalizedString)
        ? NamePatternSet.Empty
        : NamePatternSet.FromString(normalizedString, plainTextIsExactMatch: true);

    return new MemberFilter(patternSet);
  }

  /// <summary>
  /// Gets a comma-separated string of excluded member name patterns.
  /// </summary>
  /// <returns>
  /// A comma-separated string of excluded member name patterns, or an empty string if no patterns are excluded.
  /// </returns>
  /// <remarks>
  /// This method returns the list of excluded member name patterns in a format suitable for display.
  /// The names are sorted alphabetically for consistent output.
  /// </remarks>
  public string GetExcludedMemberNamesPatternsString()
  {
    var rawPatterns = _patterns.RawPatterns;
    if (rawPatterns.Count == 0)
    {
      return string.Empty;
    }

    var sortedNames = rawPatterns.OrderBy(x => x, StringComparer.Ordinal);
    return string.Join(", ", sortedNames);
  }

  /// <summary>
  /// Extracts the type name from a fully qualified method name.
  /// </summary>
  /// <param name="fullyQualifiedMethodName">
  /// The fully qualified method name (e.g., "Namespace.Type.Method(...)").
  /// </param>
  /// <returns>
  /// The type name (e.g., "Type") or <see langword="null"/> if extraction fails.
  /// </returns>
  /// <remarks>
  /// This method extracts the last part of the namespace/type path before the method name.
  /// For "Namespace.Type.Method(...)", it returns "Type".
  /// </remarks>
  private static string? ExtractTypeNameFromFqn(string fullyQualifiedMethodName)
  {
    if (string.IsNullOrWhiteSpace(fullyQualifiedMethodName))
    {
      return null;
    }

    // Find the parameter list start
    var paramStart = fullyQualifiedMethodName.IndexOf('(');
    var searchEnd = paramStart >= 0 ? paramStart : fullyQualifiedMethodName.Length;

    // Find the last dot before the method name (before parameters)
    var lastDot = fullyQualifiedMethodName.LastIndexOf('.', searchEnd - 1);
    if (lastDot < 0)
    {
      return null;
    }

    // Extract the part before the last dot (this is the type FQN)
    var typeFqn = fullyQualifiedMethodName[..lastDot];

    // Extract the type name (the last part after the last dot in the type FQN)
    var typeNameLastDot = typeFqn.LastIndexOf('.');
    return typeNameLastDot >= 0 ? typeFqn[(typeNameLastDot + 1)..] : typeFqn;
  }
}

