namespace MetricsReporter.MetricsReader.Commands;

using System.Threading;
using System.Threading.Tasks;
using Spectre.Console.Cli;
using MetricsReporter.MetricsReader.Output;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Implements the metrics-reader test command.
/// </summary>
internal sealed class TestMetricCommand : MetricsReaderCommandBase<TestMetricSettings>
{
  public override async Task<int> ExecuteAsync(CommandContext context, TestMetricSettings settings)
  {
    var result = await EvaluateAsync(settings).ConfigureAwait(false);
    JsonConsoleWriter.Write(result);
    return 0;
  }

  private static async Task<MetricTestResultDto> EvaluateAsync(TestMetricSettings settings)
  {
    var cancellationToken = MetricsReaderCancellation.Token;
    var engine = await CreateEngineAsync(settings, cancellationToken).ConfigureAwait(false);
    var snapshot = engine.TryGetSymbol(settings.Symbol.Trim(), settings.ResolvedMetric);

    return CreateResult(snapshot, settings.IncludeSuppressed);
  }

  private static MetricTestResultDto CreateResult(SymbolMetricSnapshot? snapshot, bool includeSuppressed)
  {
    return new MetricTestResultDto
    {
      IsOk = EvaluateStatus(snapshot, includeSuppressed),
      Details = snapshot is null ? null : SymbolMetricDto.FromSnapshot(snapshot),
      Message = snapshot is null ? "Symbol not present in the current metrics report." : null
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


