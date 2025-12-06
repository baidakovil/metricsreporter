namespace MetricsReporter.MetricsReader.Services;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Updates metrics reports by running MSBuild targets.
/// </summary>
internal interface IMetricsUpdater
{
  /// <summary>
  /// Updates the metrics report by running the GenerateMetricsDashboard MSBuild target, then collects code coverage if enabled.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <remarks>
  /// This method runs two MSBuild targets in sequence:
  /// 1. Build target with GenerateMetricsDashboard=true to regenerate metrics report
  /// 2. CollectCoverage target to collect code coverage (only runs if AltCoverEnabled=true in code-metrics.props)
  /// The CollectCoverage target condition ensures it only runs when AltCoverEnabled is true.
  /// </remarks>
  Task UpdateAsync(CancellationToken cancellationToken);
}

