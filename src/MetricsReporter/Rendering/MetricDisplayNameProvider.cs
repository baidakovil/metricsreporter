namespace MetricsReporter.Rendering;

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
      => identifier switch
      {
        MetricIdentifier.AltCoverSequenceCoverage => "Sequence Coverage",
        MetricIdentifier.AltCoverBranchCoverage => "Branch Coverage",
        MetricIdentifier.AltCoverCyclomaticComplexity => "Cyclomatic (AltCover)",
        MetricIdentifier.AltCoverNPathComplexity => "NPath",
        MetricIdentifier.RoslynMaintainabilityIndex => "Maintainability",
        MetricIdentifier.RoslynCyclomaticComplexity => "Cyclomatic (Roslyn)",
        MetricIdentifier.RoslynClassCoupling => "Class Coupling",
        MetricIdentifier.RoslynDepthOfInheritance => "Depth of Inheritance",
        MetricIdentifier.RoslynSourceLines => "Source Lines",
        MetricIdentifier.RoslynExecutableLines => "Executable Lines",
        MetricIdentifier.SarifCaRuleViolations => "CA Violations",
        MetricIdentifier.SarifIdeRuleViolations => "IDE Violations",
        _ => identifier.ToString()
      };
}

