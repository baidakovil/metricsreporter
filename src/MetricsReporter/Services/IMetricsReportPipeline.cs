namespace MetricsReporter.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Configuration;
using MetricsReporter.Logging;
using MetricsReporter.Model;

/// <summary>
/// Handles parsing metrics sources, building aggregation input, and writing final reports.
/// </summary>
internal interface IMetricsReportPipeline
{
  /// <summary>
  /// Executes the metrics report pipeline: parses documents, generates report, and writes output files.
  /// </summary>
  /// <param name="options">Metrics reporter options.</param>
  /// <param name="thresholdsResult">Loaded threshold configuration result.</param>
  /// <param name="baseline">Baseline report for delta calculation.</param>
  /// <param name="suppressedSymbols">List of suppressed symbols.</param>
  /// <param name="logger">Logger instance.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Exit code indicating success or failure.</returns>
  Task<MetricsReporterExitCode> ExecuteAsync(
      MetricsReporterOptions options,
      ThresholdLoadResult thresholdsResult,
      MetricsReport? baseline,
      List<SuppressedSymbolInfo> suppressedSymbols,
      ILogger logger,
      CancellationToken cancellationToken);
}


