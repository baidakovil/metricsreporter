namespace MetricsReporter.MetricsReader.Services;

using MetricsReporter.MetricsReader.Settings;
/// <summary>
/// Parameters for handling ReadAny command results.
/// </summary>
internal sealed record ReadAnyCommandResultParameters(
  string Metric,
  string Namespace,
  string SymbolKind,
  bool ShowAll,
  bool IncludeSuppressed,
  MetricsReaderGroupByOption GroupBy);

