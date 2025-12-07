using System.Threading;
using System.Threading.Tasks;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Orchestrates read command execution by running optional scripts then invoking the metrics reader.
/// </summary>
/// <remarks>
/// The pipeline isolates the sequencing logic so the command entry point can remain thin while
/// still supporting script hooks and executor composition.
/// </remarks>
internal sealed class ReadCommandPipeline
{
  private readonly ScriptAggregationRunner _scriptRunner;
  private readonly IReadCommandExecutorFactory _executorFactory;
  private readonly ReadScriptContextFactory _scriptContextFactory;

  public ReadCommandPipeline(
    ScriptAggregationRunner scriptRunner,
    IReadCommandExecutorFactory executorFactory,
    ReadScriptContextFactory scriptContextFactory)
  {
    _scriptRunner = scriptRunner ?? throw new System.ArgumentNullException(nameof(scriptRunner));
    _executorFactory = executorFactory ?? throw new System.ArgumentNullException(nameof(executorFactory));
    _scriptContextFactory = scriptContextFactory ?? throw new System.ArgumentNullException(nameof(scriptContextFactory));
  }

  /// <summary>
  /// Executes read scripts and then runs the metrics reader if scripts succeed.
  /// </summary>
  /// <param name="commandContext">Resolved read command context.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>Exit code from scripts (if they fail) or <c>0</c> on success.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Pipeline coordinates script execution and metrics reader invocation; involved collaborators are required for command flow and further splitting would reduce clarity.")]
  public async Task<int> ExecuteAsync(ReadCommandContext commandContext, CancellationToken cancellationToken)
  {
    var scriptContext = _scriptContextFactory.Create(commandContext);
    var exitCode = await _scriptRunner.RunAsync(scriptContext, cancellationToken).ConfigureAwait(false);
    if (exitCode.HasValue)
    {
      return exitCode.Value;
    }

    var executor = _executorFactory.Create();
    await executor.ExecuteAsync(commandContext.ReaderSettings, cancellationToken).ConfigureAwait(false);
    return 0;
  }
}

