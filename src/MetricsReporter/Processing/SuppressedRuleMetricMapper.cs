namespace MetricsReporter.Processing;

using System;
using MetricsReporter.Model;

/// <summary>
/// Maps Roslyn rule identifiers (for example, <c>CA1506</c>) to
/// <see cref="MetricIdentifier"/> values used by the reporter.
/// </summary>
/// <remarks>
/// The mapping is intentionally explicit and limited to rules that directly
/// correspond to the Roslyn code metrics surfaced in the report:
/// <list type="bullet">
/// <item><description><c>CA1505</c> → <see cref="MetricIdentifier.RoslynMaintainabilityIndex"/></description></item>
/// <item><description><c>CA1502</c> → <see cref="MetricIdentifier.RoslynCyclomaticComplexity"/></description></item>
/// <item><description><c>CA1506</c> → <see cref="MetricIdentifier.RoslynClassCoupling"/></description></item>
/// <item><description><c>CA1501</c> → <see cref="MetricIdentifier.RoslynDepthOfInheritance"/></description></item>
/// </list>
/// Other Roslyn metrics such as <see cref="MetricIdentifier.RoslynSourceLines"/> and
/// <see cref="MetricIdentifier.RoslynExecutableLines"/> do not have dedicated CA rules
/// and are therefore intentionally left unmapped.
/// </remarks>
internal static class SuppressedRuleMetricMapper
{
  /// <summary>
  /// Attempts to map a CA rule identifier to a <see cref="MetricIdentifier"/>.
  /// </summary>
  /// <param name="ruleId">Rule identifier (for example, <c>CA1506</c>).</param>
  /// <param name="metricIdentifier">Mapped metric identifier when successful.</param>
  /// <returns><see langword="true"/> when the rule is known; otherwise, <see langword="false"/>.</returns>
  public static bool TryGetMetricIdentifier(string? ruleId, out MetricIdentifier metricIdentifier)
  {
    metricIdentifier = default;
    if (string.IsNullOrWhiteSpace(ruleId))
    {
      return false;
    }

    // Normalize to upper invariant and strip any description part after ':'
    // to make the mapping resilient to typical "CA1506:Description" forms.
    var normalized = ruleId.Trim();
    var colonIndex = normalized.IndexOf(':');
    if (colonIndex > 0)
    {
      normalized = normalized[..colonIndex];
    }

    normalized = normalized.ToUpperInvariant();

    return normalized switch
    {
      "CA1505" => Set(MetricIdentifier.RoslynMaintainabilityIndex, out metricIdentifier),
      "CA1502" => Set(MetricIdentifier.RoslynCyclomaticComplexity, out metricIdentifier),
      "CA1506" => Set(MetricIdentifier.RoslynClassCoupling, out metricIdentifier),
      "CA1501" => Set(MetricIdentifier.RoslynDepthOfInheritance, out metricIdentifier),
      _ => false
    };
  }

  /// <summary>
  /// Attempts to map a CA rule identifier to the corresponding metric name string.
  /// </summary>
  /// <param name="ruleId">Rule identifier (for example, <c>CA1506</c>).</param>
  /// <param name="metricName">
  /// The <see cref="MetricIdentifier"/> converted to string (for example,
  /// <c>RoslynClassCoupling</c>) when successful.
  /// </param>
  /// <returns><see langword="true"/> when a mapping exists; otherwise, <see langword="false"/>.</returns>
  public static bool TryGetMetricName(string? ruleId, out string? metricName)
  {
    metricName = null;
    if (!TryGetMetricIdentifier(ruleId, out var identifier))
    {
      return false;
    }

    metricName = identifier.ToString();
    return true;
  }

  private static bool Set(MetricIdentifier identifier, out MetricIdentifier output)
  {
    output = identifier;
    return true;
  }
}



