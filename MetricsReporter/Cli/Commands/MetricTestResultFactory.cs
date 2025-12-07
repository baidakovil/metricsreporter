using MetricsReporter.MetricsReader.Output;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.Model;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Creates DTOs describing metric test outcomes for CLI output.
/// </summary>
internal sealed class MetricTestResultFactory
{
  private readonly string _missingSnapshotMessage;

  /// <summary>
  /// Initializes a new instance of the <see cref="MetricTestResultFactory"/> class.
  /// </summary>
  /// <param name="missingSnapshotMessage">Message used when a symbol snapshot is absent.</param>
  public MetricTestResultFactory(string missingSnapshotMessage = "Symbol not present in the current metrics report.")
  {
    _missingSnapshotMessage = missingSnapshotMessage;
  }

  /// <summary>
  /// Builds a result DTO from a metric snapshot.
  /// </summary>
  /// <param name="snapshot">Symbol metric snapshot.</param>
  /// <param name="includeSuppressed">Indicates whether suppressed metrics should affect status.</param>
  /// <returns>Formatted result DTO.</returns>
  public MetricTestResultDto Create(SymbolMetricSnapshot? snapshot, bool includeSuppressed)
  {
    return new MetricTestResultDto
    {
      IsOk = EvaluateStatus(snapshot, includeSuppressed),
      Details = snapshot is null ? null : SymbolMetricDto.FromSnapshot(snapshot),
      Message = snapshot is null ? _missingSnapshotMessage : null
    };
  }

  private static bool EvaluateStatus(SymbolMetricSnapshot? snapshot, bool includeSuppressed)
  {
    if (snapshot is null)
    {
      return true;
    }

    if (!includeSuppressed && snapshot.IsSuppressed)
    {
      return true;
    }

    return snapshot.Status switch
    {
      ThresholdStatus.Warning or ThresholdStatus.Error => false,
      _ => true
    };
  }
}

