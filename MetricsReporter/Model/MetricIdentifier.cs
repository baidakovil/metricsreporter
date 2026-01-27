namespace MetricsReporter.Model;

using System.Text.Json.Serialization;

/// <summary>
/// Enumerates all metric identifiers supported by the consolidated report.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MetricIdentifier
{
  /// <summary>
  /// OpenCover sequence coverage percentage.
  /// </summary>
  OpenCoverSequenceCoverage,

  /// <summary>
  /// OpenCover branch coverage percentage.
  /// </summary>
  OpenCoverBranchCoverage,

  /// <summary>
  /// OpenCover cyclomatic complexity.
  /// </summary>
  OpenCoverCyclomaticComplexity,

  /// <summary>
  /// OpenCover NPath complexity.
  /// </summary>
  OpenCoverNPathComplexity,

  /// <summary>
  /// Maintainability index reported by Microsoft.CodeAnalysis.Metrics.
  /// </summary>
  RoslynMaintainabilityIndex,

  /// <summary>
  /// Cyclomatic complexity reported by Microsoft.CodeAnalysis.Metrics.
  /// </summary>
  RoslynCyclomaticComplexity,

  /// <summary>
  /// Class coupling reported by Microsoft.CodeAnalysis.Metrics.
  /// </summary>
  RoslynClassCoupling,

  /// <summary>
  /// Depth of inheritance reported by Microsoft.CodeAnalysis.Metrics.
  /// </summary>
  RoslynDepthOfInheritance,

  /// <summary>
  /// Source lines of code reported by Microsoft.CodeAnalysis.Metrics.
  /// </summary>
  RoslynSourceLines,

  /// <summary>
  /// Executable lines of code reported by Microsoft.CodeAnalysis.Metrics.
  /// </summary>
  RoslynExecutableLines,

  /// <summary>
  /// Count of analyzer violations that start with the CA prefix (SARIF).
  /// </summary>
  SarifCaRuleViolations,

  /// <summary>
  /// Count of analyzer violations that start with the IDE prefix (SARIF).
  /// </summary>
  SarifIdeRuleViolations,
}


