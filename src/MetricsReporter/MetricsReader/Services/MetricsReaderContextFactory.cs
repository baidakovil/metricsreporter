namespace MetricsReporter.MetricsReader.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Model;
using MetricsReporter.MetricsReader.Settings;

/// <summary>
/// Creates <see cref="MetricsReaderContext"/> instances based on CLI settings.
/// </summary>
internal sealed class MetricsReaderContextFactory
{
  private readonly IJsonReportLoader _reportLoader;
  private readonly IThresholdsFileLoader _thresholdsFileLoader;
  private readonly ISolutionLocator _solutionLocator;
  private readonly IMetricsUpdaterFactory _updaterFactory;

  /// <summary>
  /// Initializes a new instance of the <see cref="MetricsReaderContextFactory"/> class.
  /// </summary>
  /// <param name="reportLoader">The report loader to use.</param>
  /// <param name="thresholdsFileLoader">The thresholds file loader to use.</param>
  /// <param name="solutionLocator">The solution locator to use.</param>
  /// <param name="updaterFactory">The metrics updater factory to use.</param>
  public MetricsReaderContextFactory(
    IJsonReportLoader reportLoader,
    IThresholdsFileLoader thresholdsFileLoader,
    ISolutionLocator solutionLocator,
    IMetricsUpdaterFactory updaterFactory)
  {
    _reportLoader = reportLoader ?? throw new ArgumentNullException(nameof(reportLoader));
    _thresholdsFileLoader = thresholdsFileLoader ?? throw new ArgumentNullException(nameof(thresholdsFileLoader));
    _solutionLocator = solutionLocator ?? throw new ArgumentNullException(nameof(solutionLocator));
    _updaterFactory = updaterFactory ?? throw new ArgumentNullException(nameof(updaterFactory));
  }

  /// <summary>
  /// Creates a MetricsReaderContext asynchronously.
  /// </summary>
  /// <param name="settings">The settings to use for context creation.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <returns>The created MetricsReaderContext.</returns>
  public async Task<MetricsReaderContext> CreateAsync(MetricsReaderSettingsBase settings, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(settings);

    var reportPath = ResolveReportPath(settings.ReportPath, !settings.NoUpdate);
    await UpdateMetricsIfNeededAsync(settings.NoUpdate, reportPath, cancellationToken).ConfigureAwait(false);

    var report = await LoadReportAsync(reportPath, cancellationToken).ConfigureAwait(false);
    var parameters = await BuildContextCreationParameters(report, settings, cancellationToken).ConfigureAwait(false);
    return CreateContext(parameters);
  }

  private async Task UpdateMetricsIfNeededAsync(bool noUpdate, string reportPath, CancellationToken cancellationToken)
  {
    if (noUpdate)
    {
      return;
    }

    var solutionPath = _solutionLocator.FindSolutionPath(reportPath);
    var updater = _updaterFactory.Create(solutionPath);
    await updater.UpdateAsync(cancellationToken).ConfigureAwait(false);
  }

  private async Task<MetricsReport> LoadReportAsync(string reportPath, CancellationToken cancellationToken)
  {
    EnsureReportExists(reportPath);
    var report = await _reportLoader.LoadAsync(reportPath, cancellationToken).ConfigureAwait(false)
                 ?? throw new InvalidOperationException($"Failed to load metrics report: {reportPath}");
    return report;
  }


  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Method builds context creation parameters by extracting data from MetricsReport metadata and loading override thresholds; dependencies on model types (MetricIdentifier, MetricSymbolLevel, MetricThreshold) and settings are necessary for parameter construction.")]
  private async Task<ContextCreationParameters> BuildContextCreationParameters(
    MetricsReport report,
    MetricsReaderSettingsBase settings,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(report);
    var overrideThresholds = await _thresholdsFileLoader.LoadAsync(settings.ThresholdsFile, cancellationToken).ConfigureAwait(false);
    var thresholdsByLevel = new ReadOnlyDictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>>(report.Metadata.ThresholdsByLevel);
    var suppressedSymbols = report.Metadata.SuppressedSymbols;
    return new ContextCreationParameters(
      report,
      thresholdsByLevel,
      overrideThresholds,
      suppressedSymbols,
      settings.IncludeSuppressed);
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Method creates MetricsReaderContext by instantiating threshold provider and suppressed symbol index; dependencies on model types (MetricsThresholdProvider, SuppressedSymbolIndex, MetricsReaderContext) are necessary for context construction.")]
  private static MetricsReaderContext CreateContext(ContextCreationParameters parameters)
  {
    var thresholdProvider = new MetricsThresholdProvider(parameters.ThresholdsByLevel, parameters.OverrideThresholds);
    var suppressedIndex = SuppressedSymbolIndex.Create(parameters.SuppressedSymbols);
    return new MetricsReaderContext(
      parameters.Report,
      thresholdProvider,
      suppressedIndex,
      parameters.IncludeSuppressed);
  }

  private static string ResolveReportPath(string? path, bool allowMissing)
  {
    var resolved = string.IsNullOrWhiteSpace(path) ? string.Empty : path;
    resolved = Path.GetFullPath(resolved);
    if (!allowMissing && !File.Exists(resolved))
    {
      throw new FileNotFoundException($"Metrics report not found: {resolved}", resolved);
    }

    return resolved;
  }

  private static void EnsureReportExists(string path)
  {
    if (!File.Exists(path))
    {
      throw new FileNotFoundException($"Metrics report not found: {path}", path);
    }
  }
}


