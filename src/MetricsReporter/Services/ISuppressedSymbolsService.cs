namespace MetricsReporter.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Logging;
using MetricsReporter.Model;

/// <summary>
/// Provides suppressed symbol metadata by either running Roslyn analysis or loading cached artefacts.
/// </summary>
internal interface ISuppressedSymbolsService
{
  /// <summary>
  /// Resolves suppressed symbols according to the configured options.
  /// </summary>
  /// <param name="options">Metrics reporter options.</param>
  /// <param name="logger">Logger for progress and error messages.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <returns>A list of suppressed symbols. Returns an empty list when no data is available.</returns>
  Task<List<SuppressedSymbolInfo>> ResolveAsync(
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken);
}


