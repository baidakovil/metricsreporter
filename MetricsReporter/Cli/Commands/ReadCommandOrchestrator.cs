using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds read command context and drives the pipeline that runs scripts then executes the reader.
/// </summary>
internal sealed class ReadCommandOrchestrator : IReadCommandOrchestrator
{
  private readonly ReadCommandContextBuilder _contextBuilder;
  private readonly ReadCommandPipeline _pipeline;

  /// <summary>
  /// Initializes a new instance of the <see cref="ReadCommandOrchestrator"/> class.
  /// </summary>
  /// <param name="contextBuilder">Builder that resolves configuration and read settings.</param>
  /// <param name="scriptRunner">Runner that executes read scripts and aggregation.</param>
  /// <param name="executorFactory">Factory that creates the metrics reader executor.</param>
  public ReadCommandOrchestrator(
    ReadCommandContextBuilder contextBuilder,
    ScriptAggregationRunner scriptRunner,
    IReadCommandExecutorFactory executorFactory)
  {
    _contextBuilder = contextBuilder ?? throw new System.ArgumentNullException(nameof(contextBuilder));
    var runner = scriptRunner ?? throw new System.ArgumentNullException(nameof(scriptRunner));
    var factory = executorFactory ?? throw new System.ArgumentNullException(nameof(executorFactory));
    _pipeline = new ReadCommandPipeline(runner, factory, new ReadScriptContextFactory());
  }

  /// <inheritdoc />
  public async Task<int> ExecuteAsync(ReadSettings settings, CancellationToken cancellationToken)
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

