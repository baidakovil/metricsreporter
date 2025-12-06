namespace MetricsReporter.Model;

using System.Text.Json.Serialization;

/// <summary>
/// Describes the threshold status of a metric.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThresholdStatus
{
  /// <summary>
  /// No threshold defined or not applicable.
  /// </summary>
  NotApplicable,

  /// <summary>
  /// Metric value is within the acceptable range.
  /// </summary>
  Success,

  /// <summary>
  /// Threshold breached but not critically – developer attention required.
  /// </summary>
  Warning,

  /// <summary>
  /// Critical threshold violation.
  /// </summary>
  Error,
}


