namespace MetricsReporter.Model;

/// <summary>
/// Describes warning and error thresholds for a specific metric.
/// </summary>
public sealed class MetricThreshold
{
  /// <summary>
  /// Warning threshold. For metrics where a higher value is desirable, treat this as the minimal acceptable value.
  /// </summary>
  public decimal? Warning { get; init; }
      = null;

  /// <summary>
  /// Error threshold. For metrics where a higher value is desirable, treat this as the minimal acceptable value.
  /// </summary>
  public decimal? Error { get; init; }
      = null;

  /// <summary>
  /// Indicates whether higher values are considered better (<see langword="true"/>) or worse (<see langword="false"/>).
  /// </summary>
  public bool HigherIsBetter { get; init; }
      = true;

  /// <summary>
  /// When <see langword="true"/>, prevents positive deltas from being rendered in the degrading color
  /// even when the metric is configured as higher-worse (commonly needed for informational size metrics).
  /// </summary>
  public bool PositiveDeltaNeutral { get; init; }
      = false;
}


