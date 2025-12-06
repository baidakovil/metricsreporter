namespace MetricsReporter.MetricsReader.Services;

using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Describes the filtering criteria for symbol-level metrics queries.
/// </summary>
internal sealed record SymbolFilter(
  string Namespace,
  MetricIdentifier Metric,
  MetricsReaderSymbolKind SymbolKind,
  bool IncludeSuppressed);


