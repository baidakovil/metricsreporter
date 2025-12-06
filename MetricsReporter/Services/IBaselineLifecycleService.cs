namespace MetricsReporter.Services;

using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Logging;
using MetricsReporter.Model;

/// <summary>
/// Manages baseline detection, initialization, loading, and replacement.
/// </summary>
internal interface IBaselineLifecycleService
{
  /// <summary>
  /// Captures the current state of the baseline artefacts before processing begins.
  /// </summary>
  /// <param name="options">Metrics reporter options.</param>
  /// <returns>Baseline run context snapshot.</returns>
  BaselineRunContext CaptureContext(MetricsReporterOptions options);

  /// <summary>
  /// Logs diagnostic information about the captured baseline state.
  /// </summary>
  /// <param name="context">Baseline run context.</param>
  /// <param name="options">Metrics reporter options.</param>
  /// <param name="logger">Logger instance.</param>
  void LogContext(BaselineRunContext context, MetricsReporterOptions options, ILogger logger);

  /// <summary>
  /// Ensures the baseline exists when baseline replacement is enabled.
  /// </summary>
  /// <param name="context">Baseline run context.</param>
  /// <param name="options">Metrics reporter options.</param>
  /// <param name="logger">Logger instance.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  Task InitializeBaselineAsync(
      BaselineRunContext context,
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken);

  /// <summary>
  /// Loads the current baseline file if it exists.
  /// </summary>
  /// <param name="baselinePath">Path to baseline file.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Baseline report or <see langword="null"/> if not found.</returns>
  Task<MetricsReport?> LoadBaselineAsync(string? baselinePath, CancellationToken cancellationToken);

  /// <summary>
  /// Replaces the baseline with the newly generated report when applicable.
  /// </summary>
  /// <param name="context">Baseline run context.</param>
  /// <param name="options">Metrics reporter options.</param>
  /// <param name="logger">Logger instance.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  Task ReplaceBaselineAsync(
      BaselineRunContext context,
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken);
}


