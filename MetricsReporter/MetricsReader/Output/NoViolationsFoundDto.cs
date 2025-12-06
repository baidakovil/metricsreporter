namespace MetricsReporter.MetricsReader.Output;
/// <summary>
/// DTO representing a message when no violations are found.
/// </summary>
internal sealed record NoViolationsFoundDto(
  string Metric,
  string Namespace,
  string SymbolKind,
  string Message);






