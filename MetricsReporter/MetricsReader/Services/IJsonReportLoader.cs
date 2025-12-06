namespace MetricsReporter.MetricsReader.Services;

using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Model;

/// <summary>
/// Loads metrics reports from JSON files.
/// </summary>
internal interface IJsonReportLoader
{
  /// <summary>
  /// Loads a metrics report from a JSON file.
  /// </summary>
  /// <param name="jsonPath">Path to the JSON file containing the metrics report.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <returns>The loaded metrics report, or <see langword="null"/> if deserialization failed.</returns>
  Task<MetricsReport?> LoadAsync(string jsonPath, CancellationToken cancellationToken);
}


