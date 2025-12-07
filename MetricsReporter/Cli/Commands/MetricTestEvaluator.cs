using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.MetricsReader.Output;

namespace MetricsReporter.Cli.Commands;

internal sealed class MetricTestEvaluator
{
  private readonly MetricTestResultFactory _resultFactory;

  /// <summary>
  /// Initializes a new instance of the <see cref="MetricTestEvaluator"/> class.
  /// </summary>
  /// <param name="resultFactory">Factory that builds output DTOs from metric snapshots.</param>
  public MetricTestEvaluator(MetricTestResultFactory resultFactory)
  {
    _resultFactory = resultFactory ?? throw new System.ArgumentNullException(nameof(resultFactory));
  }

  /// <summary>
  /// Loads metrics, evaluates the symbol, and writes the JSON result.
  /// </summary>
  /// <param name="context">Resolved test command context.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>Exit code: 0 on success.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Metric evaluation needs access to engine creation, snapshot resolution, DTO projection, and console output; reducing dependencies would duplicate this orchestration.")]
  public async Task<int> EvaluateAsync(TestCommandContext context, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(context);

    var engine = await MetricsReaderCommandHelper.CreateEngineAsync(context.TestSettings, cancellationToken).ConfigureAwait(false);
    var snapshot = engine.TryGetSymbol(context.TestSettings.Symbol.Trim(), context.TestSettings.ResolvedMetric);
    var result = _resultFactory.Create(snapshot, context.TestSettings.IncludeSuppressed);
    JsonConsoleWriter.Write(result);
    return 0;
  }
}

