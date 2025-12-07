using System.Threading;
using System.Threading.Tasks;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Orchestrates readsarif execution by running optional scripts before invoking the SARIF reader.
/// </summary>
/// <remarks>
/// Keeps the command entry point lean while ensuring scripts and SARIF aggregation execute in the
/// correct order.
/// </remarks>
internal sealed class ReadSarifCommandPipeline
{
  private readonly ScriptAggregationRunner _scriptRunner;
  private readonly IReadSarifExecutorFactory _executorFactory;
  private readonly ReadSarifScriptContextFactory _scriptContextFactory;

  public ReadSarifCommandPipeline(
    ScriptAggregationRunner scriptRunner,
    IReadSarifExecutorFactory executorFactory,
    ReadSarifScriptContextFactory scriptContextFactory)
  {
    _scriptRunner = scriptRunner ?? throw new System.ArgumentNullException(nameof(scriptRunner));
    _executorFactory = executorFactory ?? throw new System.ArgumentNullException(nameof(executorFactory));
    _scriptContextFactory = scriptContextFactory ?? throw new System.ArgumentNullException(nameof(scriptContextFactory));
  }

  /// <summary>
  /// Executes readsarif scripts and then runs the SARIF reader if scripts succeed.
  /// </summary>
  /// <param name="commandContext">Resolved readsarif command context.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>Exit code from scripts (if they fail) or <c>0</c> on success.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Pipeline must coordinate script execution and SARIF executor; involved collaborators are required for the command flow.")]
  public async Task<int> ExecuteAsync(ReadSarifCommandContext commandContext, CancellationToken cancellationToken)
  {
    var scriptContext = _scriptContextFactory.Create(commandContext);
    var exitCode = await _scriptRunner.RunAsync(scriptContext, cancellationToken).ConfigureAwait(false);
    if (exitCode.HasValue)
    {
      return exitCode.Value;
    }

    var executor = _executorFactory.Create();
    await executor.ExecuteAsync(commandContext.SarifSettings, cancellationToken).ConfigureAwait(false);
    return 0;
  }
}

