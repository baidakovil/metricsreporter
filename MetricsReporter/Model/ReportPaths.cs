namespace MetricsReporter.Model;

/// <summary>
/// Contains well-known file system paths used by the dashboard.
/// </summary>
public sealed class ReportPaths
{
  /// <summary>
  /// Absolute or relative path to the metrics directory.
  /// </summary>
  public string MetricsDirectory { get; init; } = string.Empty;

  /// <summary>
  /// Optional path to the baseline file.
  /// </summary>
  public string? Baseline { get; init; }
      = null;

  /// <summary>
  /// Path to the generated JSON report.
  /// </summary>
  public string Report { get; init; } = string.Empty;

  /// <summary>
  /// Path to the generated HTML dashboard.
  /// </summary>
  public string Html { get; init; } = string.Empty;

  /// <summary>
  /// Optional path to the thresholds definition file used for the report.
  /// </summary>
  public string? Thresholds { get; init; }
      = null;
}


