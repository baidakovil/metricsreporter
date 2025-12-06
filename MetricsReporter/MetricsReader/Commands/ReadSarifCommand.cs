namespace MetricsReporter.MetricsReader.Commands;

using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using Spectre.Console.Cli;

/// <summary>
/// Aggregates SARIF-based metric breakdowns by rule identifier.
/// </summary>
internal sealed class ReadSarifCommand : MetricsReaderCommandBase<SarifMetricSettings>
{
  /// <inheritdoc />
  protected override async Task<int> ExecuteAsync(CommandContext context, SarifMetricSettings settings, CancellationToken cancellationToken)
  {
    var executor = CreateExecutor();
    await executor.ExecuteAsync(settings, cancellationToken).ConfigureAwait(false);
    return 0;
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Factory method creates executor with all required services (aggregator, sorter, filter, result handler); decomposition would fragment factory logic without meaningful architectural benefit.")]
  private static ReadSarifCommandExecutor CreateExecutor()
  {
    var aggregator = new SarifGroupAggregator();
    var sorter = new SarifGroupSorter();
    var filter = new SarifGroupFilter();
    var resultHandler = new ReadSarifCommandResultHandler();
    return new ReadSarifCommandExecutor(CreateEngineAsync, aggregator, sorter, filter, resultHandler);
  }
}



