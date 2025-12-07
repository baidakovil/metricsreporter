using System;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.MetricsReader.Services;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Creates executors for the readsarif command.
/// </summary>
internal interface IReadSarifExecutorFactory
{
  /// <summary>
  /// Creates a new executor instance for SARIF aggregation.
  /// </summary>
  /// <returns>Configured SARIF executor.</returns>
  ReadSarifCommandExecutor Create();
}

/// <summary>
/// Factory that composes collaborators for SARIF group aggregation and output.
/// </summary>
internal sealed class ReadSarifExecutorFactory : IReadSarifExecutorFactory
{
  /// <inheritdoc />
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Factory composes SARIF aggregation components; coupling reflects intentional construction of required collaborators.")]
  public ReadSarifCommandExecutor Create()
  {
    var aggregator = new SarifGroupAggregator();
    var sorter = new SarifGroupSorter();
    var filter = new SarifGroupFilter();
    var resultHandler = new ReadSarifCommandResultHandler();
    return new ReadSarifCommandExecutor(MetricsReaderCommandHelper.CreateEngineAsync, aggregator, sorter, filter, resultHandler);
  }
}

