namespace MetricsReporter.MetricsReader.Services;

using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.MetricsReader.Settings;

/// <summary>
/// Executes the ReadSarif command logic.
/// </summary>
internal interface IReadSarifCommandExecutor
{
  /// <summary>
  /// Executes the ReadSarif command with the specified settings.
  /// </summary>
  /// <param name="settings">The command settings.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>A task representing the async operation.</returns>
  Task ExecuteAsync(SarifMetricSettings settings, CancellationToken cancellationToken);
}


