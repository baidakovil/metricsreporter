namespace MetricsReporter.Rendering;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Provides display names for metric identifiers.
/// </summary>
internal static class MetricDisplayNameProvider
{
  /// <summary>
  /// Gets the human-readable display name for the specified metric identifier.
  /// </summary>
  /// <param name="identifier">The metric identifier.</param>
  /// <returns>The display name for the metric.</returns>
  public static string GetDisplayName(MetricIdentifier identifier)
  {
    return DisplayNames.TryGetValue(identifier, out var displayName)
      ? displayName
      : identifier.ToString();
  }

  private static readonly Dictionary<MetricIdentifier, string> DisplayNames =
    new()
    {
      [MetricIdentifier.AltCoverSequenceCoverage] = "Sequence Coverage",
      [MetricIdentifier.AltCoverBranchCoverage] = "Branch Coverage",
      [MetricIdentifier.AltCoverCyclomaticComplexity] = "Cyclomatic (AltCover)",
      [MetricIdentifier.AltCoverNPathComplexity] = "NPath",
      [MetricIdentifier.RoslynMaintainabilityIndex] = "Maintainability",
      [MetricIdentifier.RoslynCyclomaticComplexity] = "Cyclomatic (Roslyn)",
      [MetricIdentifier.RoslynClassCoupling] = "Class Coupling",
      [MetricIdentifier.RoslynDepthOfInheritance] = "Depth of Inheritance",
      [MetricIdentifier.RoslynSourceLines] = "Source Lines",
      [MetricIdentifier.RoslynExecutableLines] = "Executable Lines",
      [MetricIdentifier.SarifCaRuleViolations] = "CA Violations",
      [MetricIdentifier.SarifIdeRuleViolations] = "IDE Violations"
    };
}

