using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Settings;

namespace MetricsReporter.Cli.Commands;

internal interface IReadSarifCommandOrchestrator
{
  /// <summary>
  /// Executes the readsarif command pipeline.
  /// </summary>
  /// <param name="settings">CLI settings describing the SARIF read operation.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>A task that completes with the process exit code.</returns>
  Task<int> ExecuteAsync(ReadSarifSettings settings, CancellationToken cancellationToken);
}

