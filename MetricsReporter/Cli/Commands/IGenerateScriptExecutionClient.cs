using System.Threading;
using System.Threading.Tasks;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Executes generate scripts using a prepared request and cancellation token.
/// </summary>
internal interface IGenerateScriptExecutionClient
{
  /// <summary>
  /// Executes scripts and returns an exit code when execution fails.
  /// </summary>
  /// <param name="request">Script execution parameters.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>Exit code on failure; otherwise <see langword="null"/>.</returns>
  Task<int?> ExecuteAsync(GenerateScriptRunRequest request, CancellationToken cancellationToken);
}
