namespace MetricsReporter.MetricsReader.Output;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the response of the metrics-reader test command.
/// </summary>
internal sealed class MetricTestResultDto
{
  public bool IsOk { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
  public SymbolMetricDto? Details { get; init; }

  public string? Message { get; init; }
}


