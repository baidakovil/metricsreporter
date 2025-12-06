namespace MetricsReporter.MetricsReader.Output;

using MetricsReporter.MetricsReader.Services;

/// <summary>
/// DTO describing a single SARIF violation.
/// </summary>
internal sealed class SarifViolationDetailDto
{
  public string Symbol { get; init; } = string.Empty;

  public string? Message { get; init; }

  public string? Uri { get; init; }

  public int? StartLine { get; init; }

  public int? EndLine { get; init; }

  public static SarifViolationDetailDto FromModel(SarifViolationRecord record)
    => new()
    {
      Symbol = record.Symbol,
      Message = record.Message,
      Uri = record.Uri,
      StartLine = record.StartLine,
      EndLine = record.EndLine
    };
}

