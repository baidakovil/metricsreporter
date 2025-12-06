namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Executes queries for problematic symbols.
/// </summary>
internal interface ISymbolQueryService
{
  /// <summary>
  /// Gets problematic symbols matching the specified criteria.
  /// </summary>
  /// <param name="engine">The metrics reader engine to use.</param>
  /// <param name="namespace">The namespace filter.</param>
  /// <param name="metric">The metric identifier.</param>
  /// <param name="symbolKind">The symbol kind filter.</param>
  /// <param name="includeSuppressed">Whether to include suppressed symbols.</param>
  /// <returns>An enumeration of problematic symbol snapshots.</returns>
  IEnumerable<SymbolMetricSnapshot> GetProblematicSymbols(
    MetricsReaderEngine engine,
    string @namespace,
    MetricIdentifier metric,
    MetricsReaderSymbolKind symbolKind,
    bool includeSuppressed);
}


