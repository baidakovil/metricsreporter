namespace MetricsReporter.Processing;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Unified contract for all raw metrics parsers.
/// </summary>
public interface IMetricsSourceParser
{
  /// <summary>
  /// Parses a metrics file asynchronously.
  /// </summary>
  /// <param name="path">Path to the source file.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Parsed document snapshot.</returns>
  Task<ParsedMetricsDocument> ParseAsync(string path, CancellationToken cancellationToken);
}


