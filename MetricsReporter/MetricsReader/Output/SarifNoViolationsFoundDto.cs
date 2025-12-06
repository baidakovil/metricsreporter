namespace MetricsReporter.MetricsReader.Output;
/// <summary>
/// DTO representing a message when no SARIF violations are found.
/// </summary>
internal sealed record SarifNoViolationsFoundDto(
  string Metric,
  string Namespace,
  string SymbolKind,
  string? RuleId,
  string Message);






