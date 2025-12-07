using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Logging;
using MetricsReporter.Services.Scripts;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Executes generate scripts through the reusable execution pipeline.
/// </summary>
internal sealed class GenerateScriptExecutor : IGenerateScriptExecutor
{
  private readonly GenerateScriptExecutionPipeline _pipeline;

  public GenerateScriptExecutor(ScriptExecutionService scriptExecutor)
  {
    ArgumentNullException.ThrowIfNull(scriptExecutor);
    var notifier = new ScriptRunNotifier();
    var loggerFactory = new GenerateScriptLoggerFactory();
    var guard = new ScriptExecutionGuard(notifier);
    var client = new GenerateScriptExecutionClient(scriptExecutor, loggerFactory);
    _pipeline = new GenerateScriptExecutionPipeline(guard, client);
  }

  /// <inheritdoc />
  public async Task<int?> ExecuteAsync(GenerateScriptRunRequest request, CancellationToken cancellationToken)
  {
    return await _pipeline.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
  }
}

