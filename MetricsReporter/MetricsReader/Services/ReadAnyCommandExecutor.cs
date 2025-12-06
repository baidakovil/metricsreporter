namespace MetricsReporter.MetricsReader.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.MetricsReader.Settings;

/// <summary>
/// Executes the ReadAny command logic.
/// </summary>
internal sealed class ReadAnyCommandExecutor : IReadAnyCommandExecutor
{
  private readonly Func<NamespaceMetricSettings, CancellationToken, Task<MetricsReaderEngine>> _engineFactory;
  private readonly ISymbolQueryService _queryService;
  private readonly ISymbolSnapshotOrderer _orderer;
  private readonly IReadAnyCommandResultHandler _resultHandler;

  /// <summary>
  /// Initializes a new instance of the <see cref="ReadAnyCommandExecutor"/> class.
  /// </summary>
  /// <param name="engineFactory">Factory for creating metrics reader engines.</param>
  /// <param name="queryService">The symbol query service to use.</param>
  /// <param name="orderer">The symbol snapshot orderer to use.</param>
  /// <param name="resultHandler">The result handler to use.</param>
  public ReadAnyCommandExecutor(
    Func<NamespaceMetricSettings, CancellationToken, Task<MetricsReaderEngine>> engineFactory,
    ISymbolQueryService queryService,
    ISymbolSnapshotOrderer orderer,
    IReadAnyCommandResultHandler resultHandler)
  {
    _engineFactory = engineFactory ?? throw new ArgumentNullException(nameof(engineFactory));
    _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    _orderer = orderer ?? throw new ArgumentNullException(nameof(orderer));
    _resultHandler = resultHandler ?? throw new ArgumentNullException(nameof(resultHandler));
  }

  /// <inheritdoc/>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "ReadAny command executor orchestrates specialized services (query service, orderer, result handler) and creates lightweight DTOs for coordination; further decomposition would fragment the orchestration logic and degrade maintainability.")]
  public async Task ExecuteAsync(NamespaceMetricSettings settings, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(settings);

    var engine = await _engineFactory(settings, cancellationToken).ConfigureAwait(false);
    var trimmedNamespace = settings.Namespace.Trim();
    var query = _queryService.GetProblematicSymbols(
      engine,
      trimmedNamespace,
      settings.ResolvedMetric,
      settings.SymbolKind,
      settings.IncludeSuppressed);

    var orderingParameters = new SymbolSnapshotOrderingParameters(settings.SymbolKind);
    var ordered = _orderer.Order(query, orderingParameters);

    var resultParameters = new ReadAnyCommandResultParameters(
      settings.Metric,
      trimmedNamespace,
      settings.SymbolKind.ToString(),
      settings.ShowAll,
      settings.IncludeSuppressed,
      settings.EffectiveGroupBy);
    _resultHandler.HandleResults(ordered, resultParameters);
  }
}

