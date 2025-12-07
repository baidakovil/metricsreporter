using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Settings;

namespace MetricsReporter.Cli.Commands;

internal interface IReadCommandOrchestrator
{
  /// <summary>
  /// Executes the read command: builds context, runs scripts, and prints results.
  /// </summary>
  /// <param name="settings">CLI settings describing the read operation.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>A task that completes with the process exit code.</returns>
  Task<int> ExecuteAsync(ReadSettings settings, CancellationToken cancellationToken);
}

