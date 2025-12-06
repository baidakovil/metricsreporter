namespace MetricsReporter.MetricsReader.Services;

using System;
using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Resolves user-friendly metric aliases to <see cref="MetricIdentifier"/> values.
/// </summary>
internal static class MetricIdentifierResolver
{
  private static readonly Dictionary<string, MetricIdentifier> Aliases = new(StringComparer.OrdinalIgnoreCase)
  {
    ["Complexity"] = MetricIdentifier.RoslynCyclomaticComplexity,
    ["CyclomaticComplexity"] = MetricIdentifier.RoslynCyclomaticComplexity,
    ["AltCoverComplexity"] = MetricIdentifier.AltCoverCyclomaticComplexity,
    ["Maintainability"] = MetricIdentifier.RoslynMaintainabilityIndex,
    ["MaintainabilityIndex"] = MetricIdentifier.RoslynMaintainabilityIndex,
    ["Coupling"] = MetricIdentifier.RoslynClassCoupling,
    ["ClassCoupling"] = MetricIdentifier.RoslynClassCoupling,
    ["Inheritance"] = MetricIdentifier.RoslynDepthOfInheritance,
    ["DepthOfInheritance"] = MetricIdentifier.RoslynDepthOfInheritance,
    ["SequenceCoverage"] = MetricIdentifier.AltCoverSequenceCoverage,
    ["BranchCoverage"] = MetricIdentifier.AltCoverBranchCoverage,
    ["Coverage"] = MetricIdentifier.AltCoverSequenceCoverage,
    ["NPath"] = MetricIdentifier.AltCoverNPathComplexity,
    ["SourceLines"] = MetricIdentifier.RoslynSourceLines,
    ["ExecutableLines"] = MetricIdentifier.RoslynExecutableLines,
    ["CaViolations"] = MetricIdentifier.SarifCaRuleViolations,
    ["IdeViolations"] = MetricIdentifier.SarifIdeRuleViolations
  };

  public static bool TryResolve(string? value, out MetricIdentifier metric)
  {
    metric = default;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    if (Enum.TryParse(value, ignoreCase: true, out metric))
    {
      return true;
    }

    return Aliases.TryGetValue(value, out metric);
  }
}


