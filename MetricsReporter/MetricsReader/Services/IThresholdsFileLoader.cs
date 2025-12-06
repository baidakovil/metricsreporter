namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Model;

/// <summary>
/// Loads threshold overrides from files.
/// </summary>
internal interface IThresholdsFileLoader
{
  /// <summary>
  /// Loads threshold overrides from a file path.
  /// </summary>
  /// <param name="thresholdsPath">The path to the thresholds file. May be <see langword="null"/>.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <returns>The loaded threshold definitions, or <see langword="null"/> if the path is empty or the file doesn't exist.</returns>
  Task<IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?> LoadAsync(
    string? thresholdsPath,
    CancellationToken cancellationToken);
}


