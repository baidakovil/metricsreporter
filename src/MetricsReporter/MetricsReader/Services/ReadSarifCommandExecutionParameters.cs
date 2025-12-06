namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Parameters for executing the ReadSarif command.
/// </summary>
internal sealed record ReadSarifCommandExecutionParameters(
  string Namespace,
  IReadOnlyList<MetricIdentifier> Metrics,
  MetricsReaderSymbolKind SymbolKind,
  bool IncludeSuppressed,
  string? RuleId,
  bool ShowAll);


