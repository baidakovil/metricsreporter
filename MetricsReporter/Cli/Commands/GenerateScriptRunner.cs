using System;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Services.Scripts;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Prepares generate script execution requests and invokes the script executor.
/// </summary>
internal sealed class GenerateScriptRunner
{
  private readonly GenerateScriptExecutor _executor;

  public GenerateScriptRunner(ScriptExecutionService scriptExecutor)
  {
    ArgumentNullException.ThrowIfNull(scriptExecutor);
    _executor = new GenerateScriptExecutor(scriptExecutor);
  }

  /// <summary>
  /// Builds a script request from the generate context and executes scripts.
  /// </summary>
  /// <param name="context">Resolved generate command context.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>Exit code when scripts fail; otherwise <see langword="null"/>.</returns>
  public async Task<int?> RunAsync(GenerateCommandContext context, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(context);

    var request = GenerateScriptRequestFactory.Create(context);
    return await RunAsync(request, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Executes generate scripts using the provided request.
  /// </summary>
  /// <param name="request">Script execution parameters.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>Exit code when scripts fail; otherwise <see langword="null"/>.</returns>
  private async Task<int?> RunAsync(GenerateScriptRunRequest request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);
    return await _executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
  }
}

