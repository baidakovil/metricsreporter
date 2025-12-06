namespace MetricsReporter.MetricsReader.Commands;

using System.Threading.Tasks;
using System.Threading;
using MetricsReporter.MetricsReader.Output;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;
using Spectre.Console.Cli;

/// <summary>
/// Provides helpers shared by all metrics-reader commands.
/// </summary>
internal abstract class MetricsReaderCommandBase<TSettings> : AsyncCommand<TSettings>
  where TSettings : MetricsReaderSettingsBase
{
  protected static async Task<MetricsReaderEngine> CreateEngineAsync(TSettings settings, CancellationToken cancellationToken)
  {
    using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, MetricsReaderCancellation.Token);
    var factory = CreateFactory();
    var context = await factory.CreateAsync(settings, linkedSource.Token).ConfigureAwait(false);
    return CreateEngine(context);
  }

  private static MetricsReaderContextFactory CreateFactory()
  {
    var reportLoader = new JsonReportLoaderAdapter();
    var thresholdsParser = new ThresholdsParserAdapter();
    var thresholdsFileLoader = new ThresholdsFileLoader(thresholdsParser);
    var solutionLocator = new SolutionLocatorAdapter();
    var updaterFactory = new MetricsUpdaterFactory();
    return new MetricsReaderContextFactory(reportLoader, thresholdsFileLoader, solutionLocator, updaterFactory);
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Factory method creates MetricsReaderEngine by instantiating all required services (node enumerator, snapshot builder, violation aggregator/orderer); decomposition would fragment factory logic without meaningful architectural benefit.")]
  private static MetricsReaderEngine CreateEngine(MetricsReaderContext context)
  {
    var nodeEnumerator = new MetricsNodeEnumerator(context.Report);
    var snapshotBuilder = new SymbolSnapshotBuilder(context.ThresholdProvider, context.SuppressedSymbolIndex);
    var violationAggregator = new SarifViolationAggregator(context.SuppressedSymbolIndex);
    var violationOrderer = new SarifViolationOrderer();
    return new MetricsReaderEngine(nodeEnumerator, snapshotBuilder, violationAggregator, violationOrderer, context.Report);
  }
}


