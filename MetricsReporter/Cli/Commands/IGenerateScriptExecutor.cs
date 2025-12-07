using System.Threading;
using System.Threading.Tasks;

namespace MetricsReporter.Cli.Commands;

internal interface IGenerateScriptExecutor
{
  /// <summary>
  /// Executes generate scripts when enabled.
  /// </summary>
  /// <param name="request">Parameters describing script execution.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>Exit code when scripts fail; otherwise <see langword="null"/>.</returns>
  Task<int?> ExecuteAsync(GenerateScriptRunRequest request, CancellationToken cancellationToken);
}

