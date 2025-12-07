using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Settings;

namespace MetricsReporter.Cli.Commands;

internal interface ITestCommandExecutor
{
  /// <summary>
  /// Builds execution context, runs scripts and evaluates the requested metric for the provided test settings.
  /// </summary>
  /// <param name="settings">CLI settings describing the symbol and metric to test.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>A task that completes with the process exit code.</returns>
  Task<int> ExecuteAsync(TestSettings settings, CancellationToken cancellationToken);
}

