namespace MetricsReporter.Services;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Model;
using Microsoft.Extensions.Logging;

/// <summary>
/// Manages baseline detection, initialization, loading, and replacement.
/// </summary>
internal sealed class BaselineLifecycleService : IBaselineLifecycleService
{
  private readonly IBaselineManager _baselineManager;

  /// <summary>
  /// Initializes a new instance of the <see cref="BaselineLifecycleService"/> class.
  /// </summary>
  public BaselineLifecycleService()
    : this(new BaselineManager())
  {
  }

  internal BaselineLifecycleService(IBaselineManager baselineManager)
  {
    _baselineManager = baselineManager;
  }
  /// <summary>
  /// Captures the current state of the baseline artefacts before processing begins.
  /// </summary>
  public BaselineRunContext CaptureContext(MetricsReporterOptions options)
  {
    var hadReportAtStart = !string.IsNullOrWhiteSpace(options.OutputJsonPath) && File.Exists(options.OutputJsonPath);
    var hadBaselineAtStart = !string.IsNullOrWhiteSpace(options.BaselinePath) && File.Exists(options.BaselinePath);
    return new BaselineRunContext(hadReportAtStart, hadBaselineAtStart, options.ReplaceMetricsBaseline);
  }

  /// <summary>
  /// Logs diagnostic information about the captured baseline state.
  /// </summary>
  public void LogContext(BaselineRunContext context, MetricsReporterOptions options, ILogger logger)
  {
    logger.LogDebug(
      "Baseline debug ReplaceMetricsBaseline={ReplaceMetricsBaseline} EffectiveReplaceBaseline={EffectiveReplaceBaseline} BaselinePath={BaselinePath} OutputJsonPath={OutputJsonPath} MetricsReportStoragePath={MetricsReportStoragePath} HadReportAtStart={HadReportAtStart} HadBaselineAtStart={HadBaselineAtStart}",
      options.ReplaceMetricsBaseline,
      context.ReplaceBaselineEnabled,
      options.BaselinePath ?? "(null)",
      options.OutputJsonPath,
      options.MetricsReportStoragePath ?? "(null)",
      context.HadReportAtStart,
      context.HadBaselineAtStart);
  }

  /// <summary>
  /// Ensures the baseline exists when baseline replacement is enabled.
  /// </summary>
  public async Task InitializeBaselineAsync(
      BaselineRunContext context,
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    if (!context.ReplaceBaselineEnabled
        || string.IsNullOrWhiteSpace(options.BaselinePath)
        || context.HadBaselineAtStart)
    {
      return;
    }

    if (context.HadReportAtStart)
    {
      logger.LogInformation(
        "Baseline does not exist. Creating baseline from previous report at {ReportPath}",
        options.OutputJsonPath);
      await _baselineManager.CreateBaselineFromPreviousReportAsync(
          options.OutputJsonPath,
          options.BaselinePath,
          logger,
          cancellationToken).ConfigureAwait(false);
      return;
    }

    logger.LogInformation(
      "Baseline does not exist and previous report not found at {ReportPath}. New report will be generated without baseline.",
      options.OutputJsonPath);
  }

  /// <summary>
  /// Loads the current baseline file if it exists.
  /// </summary>
  public Task<MetricsReport?> LoadBaselineAsync(string? baselinePath, CancellationToken cancellationToken)
  {
    return BaselineLoader.LoadAsync(baselinePath, cancellationToken);
  }

  /// <summary>
  /// Replaces the baseline with the newly generated report when applicable.
  /// </summary>
  public async Task ReplaceBaselineAsync(
      BaselineRunContext context,
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    if (!context.ReplaceBaselineEnabled
        || string.IsNullOrWhiteSpace(options.BaselinePath)
        || (!context.HadReportAtStart && !context.HadBaselineAtStart))
    {
      return;
    }

    var success = await _baselineManager.ReplaceBaselineAsync(
        options.OutputJsonPath,
        options.BaselinePath,
        options.MetricsReportStoragePath,
        logger,
        cancellationToken).ConfigureAwait(false);

    if (success)
    {
      var baselineFileName = Path.GetFileName(options.BaselinePath);
      logger.LogInformation("Baseline replaced successfully: {BaselineFileName}", baselineFileName);
    }
  }
}

/// <summary>
/// Baseline state snapshot captured at the beginning of the run.
/// </summary>
/// <param name="HadReportAtStart">Indicates whether a previous report existed.</param>
/// <param name="HadBaselineAtStart">Indicates whether the baseline file existed.</param>
/// <param name="ReplaceBaselineEnabled">Indicates whether baseline replacement is enabled.</param>
internal sealed record BaselineRunContext(bool HadReportAtStart, bool HadBaselineAtStart, bool ReplaceBaselineEnabled);


