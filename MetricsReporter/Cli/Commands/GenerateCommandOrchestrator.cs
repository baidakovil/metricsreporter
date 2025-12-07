using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Settings;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds generate command context and executes the pipeline that runs scripts then aggregation.
/// </summary>
internal sealed class GenerateCommandOrchestrator : IGenerateCommandOrchestrator
{
  private readonly GenerateCommandContextBuilder _contextBuilder;
  private readonly GenerateCommandPipeline _pipeline;

  public GenerateCommandOrchestrator(
    GenerateCommandContextBuilder contextBuilder,
    GenerateScriptRunner scriptRunner,
    GenerateApplicationRunner applicationRunner)
  {
    _contextBuilder = contextBuilder ?? throw new System.ArgumentNullException(nameof(contextBuilder));
    var runner = scriptRunner ?? throw new System.ArgumentNullException(nameof(scriptRunner));
    var appRunner = applicationRunner ?? throw new System.ArgumentNullException(nameof(applicationRunner));
    _pipeline = new GenerateCommandPipeline(runner, appRunner);
  }

  /// <inheritdoc />
  public async Task<int> ExecuteAsync(GenerateSettings settings, CancellationToken cancellationToken)
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

