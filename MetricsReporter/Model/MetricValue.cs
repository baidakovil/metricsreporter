namespace MetricsReporter.Model;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a single metric value, its delta compared to baseline, and the threshold status.
/// </summary>
public sealed class MetricValue
{
  /// <summary>
  /// Actual metric value. Use <see langword="null"/> when the value is not available.
  /// </summary>
  public decimal? Value { get; init; }

  /// <summary>
  /// Difference from baseline. Use <see langword="null"/> for new members or when no baseline exists.
  /// </summary>
  public decimal? Delta { get; init; }

  /// <summary>
  /// Threshold status for the value.
  /// </summary>
  public ThresholdStatus Status { get; init; } = ThresholdStatus.NotApplicable;

  /// <summary>
  /// Legacy per-metric unit. JSON serialization ignores this value; use metadata descriptors instead.
  /// </summary>
  [JsonIgnore]
  public string? Unit { get; init; }

  /// <summary>
  /// Optional breakdown of rule violations by rule ID (e.g., CA1502, IDE0051).
  /// Only present for <see cref="MetricIdentifier.SarifCaRuleViolations"/> and
  /// <see cref="MetricIdentifier.SarifIdeRuleViolations"/> metrics.
  /// Keys must match the pattern <c>CA####</c> or <c>IDE####</c> where <c>####</c> is a 4-digit number,
  /// and values expose both aggregated counts and violation metadata required for tooltips.
  /// </summary>
  public Dictionary<string, SarifRuleBreakdownEntry>? Breakdown { get; init; }
}


