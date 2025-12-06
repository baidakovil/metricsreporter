namespace MetricsReporter.MetricsReader.Output;
/// <summary>
/// DTO representing an error when a metric does not expose SARIF rule breakdown data.
/// </summary>
internal sealed record SarifInvalidMetricDto(
  string Metric,
  string Message);






