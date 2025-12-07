using System;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.Services.Scripts;
using Spectre.Console.Cli;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Aggregates SARIF-based metrics grouped by rule identifier.
/// </summary>
internal sealed class ReadSarifCommand : AsyncCommand<ReadSarifSettings>
{
  private readonly ReadSarifCommandContextBuilder _contextBuilder;
  private readonly ScriptAggregationRunner _scriptRunner;
  private readonly IReadSarifExecutorFactory _executorFactory;
  private readonly ReadSarifCommandOrchestrator _orchestrator;
  internal static readonly char[] MetricScriptSeparators = ['=', ':'];

  public ReadSarifCommand(MetricsReporterConfigLoader configLoader, ScriptExecutionService scriptExecutor)
  {
    ArgumentNullException.ThrowIfNull(configLoader);
    ArgumentNullException.ThrowIfNull(scriptExecutor);

    _contextBuilder = new ReadSarifCommandContextBuilder(configLoader);
    _scriptRunner = new ScriptAggregationRunner(scriptExecutor);
    _executorFactory = new ReadSarifExecutorFactory();
    _orchestrator = new ReadSarifCommandOrchestrator(_contextBuilder, _scriptRunner, _executorFactory);
  }

  /// <inheritdoc />
  public override async Task<int> ExecuteAsync(CommandContext context, ReadSarifSettings settings)
  {
    _ = context;
    var cancellationToken = CancellationToken.None;
    return await _orchestrator.ExecuteAsync(settings, cancellationToken).ConfigureAwait(false);
  }
}

