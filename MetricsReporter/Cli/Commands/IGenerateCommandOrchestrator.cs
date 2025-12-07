using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Settings;

namespace MetricsReporter.Cli.Commands;

internal interface IGenerateCommandOrchestrator
{
  /// <summary>
  /// Executes the generate command pipeline.
  /// </summary>
  /// <param name="settings">CLI settings describing generation inputs.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>A task that completes with the process exit code.</returns>
  Task<int> ExecuteAsync(GenerateSettings settings, CancellationToken cancellationToken);
}

