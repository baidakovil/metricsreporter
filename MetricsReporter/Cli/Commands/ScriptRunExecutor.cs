using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Logging;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Runs configured scripts using the shared script execution service.
/// </summary>
internal sealed class ScriptRunExecutor
{
  private readonly ScriptExecutionService _scriptExecutor;

  public ScriptRunExecutor(ScriptExecutionService scriptExecutor)
  {
    _scriptExecutor = scriptExecutor ?? throw new System.ArgumentNullException(nameof(scriptExecutor));
  }

  /// <summary>
  /// Executes scripts and returns an exit code when they fail.
  /// </summary>
  /// <param name="request">Parameters describing scripts to run.</param>
  /// <param name="logger">Logger used by the script execution context.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>Exit code when scripts fail; otherwise <see langword="null"/>.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Execution step constructs script context and invokes script executor; required dependencies are minimal and encapsulated here.")]
  public async Task<int?> ExecuteAsync(GenerateScriptRunRequest request, ILogger logger, CancellationToken cancellationToken)
  {
    var result = await _scriptExecutor.RunAsync(
      request.Scripts,
      new ScriptExecutionContext(request.WorkingDirectory, request.Timeout, request.LogTruncationLimit, logger),
      cancellationToken).ConfigureAwait(false);

    if (!result.IsSuccess)
    {
      return (int)result.ExitCode;
    }

    return null;
  }
}

