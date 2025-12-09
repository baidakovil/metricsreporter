namespace MetricsReporter.Aggregation;

using System;
using MetricsReporter.Processing;

/// <summary>
/// Extracts filter pattern strings from filter instances.
/// </summary>
internal static class FilterPatternExtractor
{
  /// <summary>
  /// Extracts filter pattern strings from the provided filters.
  /// </summary>
  /// <param name="memberFilter">The member filter.</param>
  /// <param name="memberKindFilter">The member kind filter.</param>
  /// <param name="assemblyFilter">The assembly filter.</param>
  /// <param name="typeFilter">The type filter.</param>
  /// <returns>A record containing the extracted pattern strings.</returns>
  public static FilterPatterns Extract(
      MemberFilter memberFilter,
      MemberKindFilter memberKindFilter,
      AssemblyFilter assemblyFilter,
      TypeFilter typeFilter)
  {
    ArgumentNullException.ThrowIfNull(memberFilter);
    ArgumentNullException.ThrowIfNull(memberKindFilter);
    ArgumentNullException.ThrowIfNull(assemblyFilter);
    ArgumentNullException.ThrowIfNull(typeFilter);

    return new FilterPatterns(
        memberFilter.GetExcludedMemberNamesPatternsString(),
        memberKindFilter.ExcludeMethods,
        memberKindFilter.ExcludeProperties,
        memberKindFilter.ExcludeFields,
        memberKindFilter.ExcludeEvents,
        assemblyFilter.GetExcludedAssemblyPatternsString(),
        typeFilter.GetExcludedTypePatternsString());
  }

  /// <summary>
  /// Contains the extracted filter pattern strings.
  /// </summary>
  /// <param name="MemberNamesPatterns">The member names patterns string.</param>
  /// <param name="ExcludeMethods">Whether methods were excluded.</param>
  /// <param name="ExcludeProperties">Whether properties were excluded.</param>
  /// <param name="ExcludeFields">Whether fields were excluded.</param>
  /// <param name="ExcludeEvents">Whether events were excluded.</param>
  /// <param name="AssemblyNamesPatterns">The assembly names patterns string.</param>
  /// <param name="TypeNamesPatterns">The type names patterns string.</param>
  public sealed record FilterPatterns(
      string? MemberNamesPatterns,
      bool ExcludeMethods,
      bool ExcludeProperties,
      bool ExcludeFields,
      bool ExcludeEvents,
      string? AssemblyNamesPatterns,
      string? TypeNamesPatterns);
}


