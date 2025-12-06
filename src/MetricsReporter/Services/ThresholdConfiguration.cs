namespace MetricsReporter.Services;

using System.Collections.Generic;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;

/// <summary>
/// Wraps parsed threshold definitions to hide implementation details from orchestrating code.
/// </summary>
internal sealed class ThresholdConfiguration
{
  private readonly IDictionary<MetricIdentifier, MetricThresholdDefinition> _thresholds;

  private ThresholdConfiguration(IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds)
  {
    _thresholds = thresholds;
  }

  public static ThresholdConfiguration Empty { get; } = new ThresholdConfiguration(new Dictionary<MetricIdentifier, MetricThresholdDefinition>());

  public static ThresholdConfiguration From(IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds)
  {
    return new ThresholdConfiguration(thresholds);
  }

  public IDictionary<MetricIdentifier, MetricThresholdDefinition> AsDictionary()
  {
    return _thresholds;
  }
}

/// <summary>
/// Represents the outcome of loading threshold configuration.
/// </summary>
/// <param name="ExitCode">Exit code representing load status.</param>
/// <param name="Configuration">Parsed threshold configuration.</param>
internal sealed record ThresholdLoadResult(MetricsReporterExitCode ExitCode, ThresholdConfiguration Configuration);


