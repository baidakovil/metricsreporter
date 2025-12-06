namespace MetricsReporter.MetricsReader.Commands;

using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using Spectre.Console.Cli;

/// <summary>
/// Implements the metrics-reader readany command that unifies the former list and most-problematic flows.
/// </summary>
internal sealed class ReadAnyCommand : MetricsReaderCommandBase<NamespaceMetricSettings>
{
  /// <inheritdoc />
  public override async Task<int> ExecuteAsync(CommandContext context, NamespaceMetricSettings settings)
  {
    var cancellationToken = MetricsReaderCancellation.Token;
    var executor = CreateExecutor();
    await executor.ExecuteAsync(settings, cancellationToken).ConfigureAwait(false);
    return 0;
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Factory method creates executor with all required services (query service, orderer, result handler); decomposition would fragment factory logic without meaningful architectural benefit.")]
  private static ReadAnyCommandExecutor CreateExecutor()
  {
    var queryService = new SymbolQueryService();
    var orderer = new SymbolSnapshotOrderer();
    var resultHandler = new ReadAnyCommandResultHandler();
    return new ReadAnyCommandExecutor(CreateEngineAsync, queryService, orderer, resultHandler);
  }
}



