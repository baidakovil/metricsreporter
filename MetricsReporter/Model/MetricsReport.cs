namespace MetricsReporter.Model;

/// <summary>
/// Root serialisable type produced by the reporter.
/// </summary>
public sealed class MetricsReport
{
  /// <summary>
  /// Metadata describing the report generation.
  /// </summary>
  public ReportMetadata Metadata { get; init; } = new();

  /// <summary>
  /// Solution-level node that contains the full metrics hierarchy.
  /// </summary>
  public SolutionMetricsNode Solution { get; init; } = new();
}


