namespace MetricsReporter.Model;

/// <summary>
/// Provides display metadata for a metric, such as its unit of measurement.
/// </summary>
public sealed class MetricDescriptor
{
  /// <summary>
  /// Unit for the metric (for example <c>percent</c>, <c>count</c>, <c>score</c>).
  /// </summary>
  public string? Unit { get; init; }
}



