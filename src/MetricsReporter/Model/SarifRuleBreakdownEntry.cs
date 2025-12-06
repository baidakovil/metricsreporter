namespace MetricsReporter.Model;

using System.Collections.Generic;

/// <summary>
/// Aggregated breakdown information for a single SARIF rule within a metric.
/// </summary>
public sealed class SarifRuleBreakdownEntry
{
  /// <summary>
  /// Total number of SARIF results associated with this rule after aggregation.
  /// </summary>
  public int Count { get; set; }

  /// <summary>
  /// Detailed list of violations that contributed to the <see cref="Count"/>.
  /// </summary>
  /// <remarks>
  /// The collection can be empty when violation metadata is not available (for example,
  /// when metrics are loaded from a legacy report that did not capture details).
  /// </remarks>
  public List<SarifRuleViolationDetail> Violations { get; set; } = [];
}



