using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.MetricsReader.Services;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Creates executors for the read command.
/// </summary>
internal interface IReadCommandExecutorFactory
{
  /// <summary>
  /// Creates a new executor instance for namespace metric reads.
  /// </summary>
  /// <returns>Configured read command executor.</returns>
  ReadAnyCommandExecutor Create();
}

/// <summary>
/// Factory that composes collaborators for the read command executor.
/// </summary>
internal sealed class ReadCommandExecutorFactory : IReadCommandExecutorFactory
{
  /// <inheritdoc />
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Factory composes query service, ordering, and result handler required for read command execution; coupling is inherent to assembling these collaborators.")]
  public ReadAnyCommandExecutor Create()
  {
    var queryService = new SymbolQueryService();
    var orderer = new SymbolSnapshotOrderer();
    var resultHandler = new ReadAnyCommandResultHandler();
    return new ReadAnyCommandExecutor(MetricsReaderCommandHelper.CreateEngineAsync, queryService, orderer, resultHandler);
  }
}

