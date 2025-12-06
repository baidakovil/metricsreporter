namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Aggregates SARIF violation groups from multiple metrics.
/// </summary>
internal interface ISarifGroupAggregator
{
  /// <summary>
  /// Aggregates SARIF violation groups for the specified metrics.
  /// </summary>
  /// <param name="engine">The metrics reader engine to use.</param>
  /// <param name="namespace">The namespace filter.</param>
  /// <param name="metrics">The metrics to aggregate.</param>
  /// <param name="symbolKind">The symbol kind filter.</param>
  /// <param name="includeSuppressed">Whether to include suppressed symbols.</param>
  /// <returns>A list of aggregated violation groups.</returns>
  List<SarifViolationGroup> AggregateGroups(
    MetricsReaderEngine engine,
    string @namespace,
    IEnumerable<MetricIdentifier> metrics,
    MetricsReaderSymbolKind symbolKind,
    bool includeSuppressed);
}


