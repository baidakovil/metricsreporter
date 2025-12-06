namespace MetricsReporter.Model;

using System;

/// <summary>
/// Describes a single symbol that has been explicitly suppressed via
/// <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>.
/// </summary>
/// <remarks>
/// This model is intentionally minimal and is used in two places:
/// <list type="bullet">
/// <item>
/// <description>
/// In <c>SuppressedSymbols.json</c>, which is produced by a lightweight Roslyn
/// scan when suppression analysis is enabled in MSBuild.
/// </description>
/// </item>
/// <item>
/// <description>
/// Inside the <see cref="ReportMetadata"/> of <c>metrics-report.json</c>, so that
/// the HTML dashboard and external tools can correlate suppressed metrics with
/// specific symbols without re-running the scan.
/// </description>
/// </item>
/// </list>
/// </remarks>
public sealed class SuppressedSymbolInfo
{
  /// <summary>
  /// Path to the C# source file that contains the suppression attribute.
  /// </summary>
  /// <remarks>
  /// Paths are stored relative to the solution directory when possible so that
  /// reports are portable across machines. Consumers should not assume a
  /// particular directory separator.
  /// </remarks>
  public string FilePath { get; init; } = string.Empty;

  /// <summary>
  /// Normalized fully qualified name of the suppressed symbol.
  /// </summary>
  /// <remarks>
  /// For methods this corresponds to the normalized format used by
  /// <see cref="Processing.SymbolNormalizer.NormalizeFullyQualifiedMethodName(string?)"/>
  /// (for example, <c>Namespace.Type.Method(...)</c>). For types the format is
  /// <c>Namespace.Type</c>. Using the same normalization scheme allows
  /// direct matching against <see cref="MetricsNode.FullyQualifiedName"/> values.
  /// </remarks>
  public string FullyQualifiedName { get; init; } = string.Empty;

  /// <summary>
  /// Identifier of the suppressed rule (for example, <c>CA1506</c>).
  /// </summary>
  public string RuleId { get; init; } = string.Empty;

  /// <summary>
  /// Metrics Reporter identifier of the metric that conceptually corresponds
  /// to the suppressed rule (for example, <c>RoslynClassCoupling</c>).
  /// </summary>
  /// <remarks>
  /// This is stored as a string rather than <see cref="MetricIdentifier"/> to
  /// keep <c>SuppressedSymbols.json</c> schema simple and decoupled from the
  /// exact enum surface. Consumers can convert it back to
  /// <see cref="MetricIdentifier"/> via <see cref="Enum.Parse(string)"/> if needed.
  /// </remarks>
  public string Metric { get; set; } = string.Empty;

  /// <summary>
  /// Human-readable justification text taken from the suppression attribute.
  /// </summary>
  /// <remarks>
  /// This text is surfaced as a tooltip in the HTML dashboard when hovering
  /// over suppressed metric values so that reviewers can quickly understand
  /// why a particular warning has been intentionally ignored.
  /// </remarks>
  public string? Justification { get; init; }
}



