namespace MetricsReporter.MetricsReader.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Executes the ReadSarif command logic.
/// </summary>
internal sealed class ReadSarifCommandExecutor : IReadSarifCommandExecutor
{
  private readonly Func<SarifMetricSettings, CancellationToken, Task<MetricsReaderEngine>> _engineFactory;
  private readonly ISarifGroupAggregator _aggregator;
  private readonly ISarifGroupSorter _sorter;
  private readonly ISarifGroupFilter _filter;
  private readonly IReadSarifCommandResultHandler _resultHandler;

  /// <summary>
  /// Initializes a new instance of the <see cref="ReadSarifCommandExecutor"/> class.
  /// </summary>
  /// <param name="engineFactory">Factory for creating metrics reader engines.</param>
  /// <param name="aggregator">The SARIF group aggregator to use.</param>
  /// <param name="sorter">The SARIF group sorter to use.</param>
  /// <param name="filter">The SARIF group filter to use.</param>
  /// <param name="resultHandler">The result handler to use.</param>
  public ReadSarifCommandExecutor(
    Func<SarifMetricSettings, CancellationToken, Task<MetricsReaderEngine>> engineFactory,
    ISarifGroupAggregator aggregator,
    ISarifGroupSorter sorter,
    ISarifGroupFilter filter,
    IReadSarifCommandResultHandler resultHandler)
  {
    _engineFactory = engineFactory ?? throw new ArgumentNullException(nameof(engineFactory));
    _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
    _sorter = sorter ?? throw new ArgumentNullException(nameof(sorter));
    _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    _resultHandler = resultHandler ?? throw new ArgumentNullException(nameof(resultHandler));
  }

  /// <inheritdoc/>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "ReadSarif command executor orchestrates specialized services (aggregator, sorter, filter, result handler) and accesses settings properties; further decomposition would fragment the orchestration logic and degrade maintainability.")]
  public async Task ExecuteAsync(SarifMetricSettings settings, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(settings);

    if (!settings.TryResolveSarifMetrics(out var metrics))
    {
      _resultHandler.WriteInvalidMetricError(settings.EffectiveMetricName);
      return;
    }

    var trimmedNamespace = settings.Namespace.Trim();
    var engine = await _engineFactory(settings, cancellationToken).ConfigureAwait(false);
    IReadOnlyList<MetricIdentifier> metricList = metrics ?? Array.Empty<MetricIdentifier>();

    var aggregatedGroups = _aggregator.AggregateGroups(
      engine,
      trimmedNamespace,
      metricList,
      settings.SymbolKind,
      settings.IncludeSuppressed);

    var sortedGroups = _sorter.SortByCountAndRuleId(aggregatedGroups);
    var filteredGroups = _filter.Filter(sortedGroups, settings.RuleId);

    if (filteredGroups.Count == 0)
    {
      _resultHandler.WriteNoViolationsFound(
        settings.EffectiveMetricName,
        trimmedNamespace,
        settings.SymbolKind.ToString(),
        settings.RuleId);
      return;
    }

    _resultHandler.WriteResponse(settings, filteredGroups);
  }
}

