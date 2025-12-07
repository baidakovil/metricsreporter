using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Coordinates generate scripts and the main application run to produce metrics outputs.
/// </summary>
/// <remarks>
/// This pipeline keeps the generate command flow linear: attempt scripts first and, regardless
/// of <c>--aggregate-after-scripts</c>, always run aggregation since generation requires it.
/// </remarks>
internal sealed class GenerateCommandPipeline
{
  private readonly GenerateScriptRunner _scriptRunner;
  private readonly GenerateApplicationRunner _applicationRunner;

  public GenerateCommandPipeline(GenerateScriptRunner scriptRunner, GenerateApplicationRunner applicationRunner)
  {
    _scriptRunner = scriptRunner ?? throw new System.ArgumentNullException(nameof(scriptRunner));
    _applicationRunner = applicationRunner ?? throw new System.ArgumentNullException(nameof(applicationRunner));
  }

  /// <summary>
  /// Executes generate scripts and, if they succeed, runs the application aggregation.
  /// </summary>
  /// <param name="context">Resolved generate command context.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>Exit code from scripts (if any) or from the application run.</returns>
  public async Task<int> ExecuteAsync(GenerateCommandContext context, CancellationToken cancellationToken)
  {
    var scriptExitCode = await _scriptRunner.RunAsync(context, cancellationToken).ConfigureAwait(false);
    if (scriptExitCode.HasValue)
    {
      return scriptExitCode.Value;
    }

    if (!context.GeneralOptions.AggregateAfterScripts)
    {
      AnsiConsole.MarkupLine("[yellow]--aggregate-after-scripts=false is ignored for generate; aggregation will still run.[/]");
    }

    var exitCode = await _applicationRunner.RunAsync(context.Options, cancellationToken).ConfigureAwait(false);
    return exitCode;
  }
}

