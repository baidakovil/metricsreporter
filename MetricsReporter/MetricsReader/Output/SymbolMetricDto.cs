namespace MetricsReporter.MetricsReader.Output;

using MetricsReporter.MetricsReader.Services;

/// <summary>
/// JSON-friendly representation of a symbol metric row.
/// </summary>
internal sealed class SymbolMetricDto
{
  public string SymbolFqn { get; init; } = string.Empty;

  public string SymbolType { get; init; } = string.Empty;

  public string Metric { get; init; } = string.Empty;

  public decimal? Value { get; init; }

  public decimal? Threshold { get; init; }

  public string ThresholdKind { get; init; } = string.Empty;

  public decimal? Delta { get; init; }

  public string? FilePath { get; init; }

  public string Status { get; init; } = string.Empty;

  public bool IsSuppressed { get; init; }

  public static SymbolMetricDto FromSnapshot(SymbolMetricSnapshot snapshot)
    => new()
    {
      SymbolFqn = snapshot.Symbol,
      SymbolType = snapshot.SymbolType,
      Metric = snapshot.Metric.ToString(),
      Value = snapshot.Value,
      Threshold = snapshot.ThresholdValue,
      ThresholdKind = snapshot.ThresholdKind,
      Delta = snapshot.Delta,
      FilePath = snapshot.FilePath,
      Status = snapshot.Status.ToString(),
      IsSuppressed = snapshot.IsSuppressed
    };
}


