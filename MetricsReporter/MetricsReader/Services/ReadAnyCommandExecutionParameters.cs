namespace MetricsReporter.MetricsReader.Services;

using MetricsReporter.MetricsReader.Settings;

/// <summary>
/// Parameters for executing the ReadAny command.
/// </summary>
internal sealed record ReadAnyCommandExecutionParameters(
  string Namespace,
  string Metric,
  MetricsReaderSymbolKind SymbolKind,
  bool IncludeSuppressed,
  bool ShowAll);


