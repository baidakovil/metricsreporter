using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Logging;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Coordinates script execution guard checks with the script execution client.
/// </summary>
/// <remarks>
/// Encapsulates skip logic so callers remain focused on command orchestration rather than
/// guard/notification details.
/// </remarks>
internal sealed class GenerateScriptExecutionPipeline
{
  private readonly IScriptExecutionGuard _guard;
  private readonly IGenerateScriptExecutionClient _client;

  public GenerateScriptExecutionPipeline(IScriptExecutionGuard guard, IGenerateScriptExecutionClient client)
  {
    _guard = guard ?? throw new System.ArgumentNullException(nameof(guard));
    _client = client ?? throw new System.ArgumentNullException(nameof(client));
  }

  /// <summary>
  /// Executes scripts if enabled and present; otherwise returns <see langword="null"/>.
  /// </summary>
  /// <param name="request">Script execution parameters.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <param name="operationName">Friendly name of the operation for notifications.</param>
  /// <returns>Exit code when scripts fail; otherwise <see langword="null"/>.</returns>
  public async Task<int?> ExecuteAsync(GenerateScriptRunRequest request, CancellationToken cancellationToken, string operationName = "generate")
  {
    ArgumentNullException.ThrowIfNull(request);

    if (_guard.ShouldSkip(request, operationName))
    {
      return null;
    }

    return await _client.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
  }
}

