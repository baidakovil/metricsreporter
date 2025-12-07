using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Settings;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds readsarif command context and runs the pipeline that executes scripts and SARIF reader.
/// </summary>
internal sealed class ReadSarifCommandOrchestrator : IReadSarifCommandOrchestrator
{
  private readonly ReadSarifCommandContextBuilder _contextBuilder;
  private readonly ReadSarifCommandPipeline _pipeline;

  /// <summary>
  /// Initializes a new instance of the <see cref="ReadSarifCommandOrchestrator"/> class.
  /// </summary>
  /// <param name="contextBuilder">Builder that resolves SARIF command inputs.</param>
  /// <param name="scriptRunner">Runner that executes scripts and aggregation.</param>
  /// <param name="executorFactory">Factory that creates SARIF command executors.</param>
  public ReadSarifCommandOrchestrator(
    ReadSarifCommandContextBuilder contextBuilder,
    ScriptAggregationRunner scriptRunner,
    IReadSarifExecutorFactory executorFactory)
  {
    _contextBuilder = contextBuilder ?? throw new System.ArgumentNullException(nameof(contextBuilder));
    var runner = scriptRunner ?? throw new System.ArgumentNullException(nameof(scriptRunner));
    var factory = executorFactory ?? throw new System.ArgumentNullException(nameof(executorFactory));
    _pipeline = new ReadSarifCommandPipeline(runner, factory, new ReadSarifScriptContextFactory());
  }

  /// <inheritdoc />
  public async Task<int> ExecuteAsync(ReadSarifSettings settings, CancellationToken cancellationToken)
  {
    var buildResult = _contextBuilder.Build(settings);
    if (!buildResult.Succeeded)
    {
      return buildResult.ExitCode ?? (int)MetricsReporterExitCode.ValidationError;
    }

    var commandContext = buildResult.Context!;
    return await _pipeline.ExecuteAsync(commandContext, cancellationToken).ConfigureAwait(false);
  }
}

