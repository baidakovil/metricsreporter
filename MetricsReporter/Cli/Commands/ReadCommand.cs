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
/// Reads metric violations for a namespace and metric.
/// </summary>
internal sealed class ReadCommand : AsyncCommand<ReadSettings>
{
  private readonly ReadCommandContextBuilder _contextBuilder;
  private readonly ScriptAggregationRunner _scriptRunner;
  private readonly IReadCommandExecutorFactory _executorFactory;
  private readonly ReadCommandOrchestrator _orchestrator;
  internal static readonly char[] MetricScriptSeparators = ['=', ':'];

  /// <summary>
  /// Initializes a new instance of the <see cref="ReadCommand"/> class.
  /// </summary>
  public ReadCommand(MetricsReporterConfigLoader configLoader, ScriptExecutionService scriptExecutor)
  {
    ArgumentNullException.ThrowIfNull(configLoader);
    ArgumentNullException.ThrowIfNull(scriptExecutor);

    _contextBuilder = new ReadCommandContextBuilder(configLoader);
    _scriptRunner = new ScriptAggregationRunner(scriptExecutor);
    _executorFactory = new ReadCommandExecutorFactory();
    _orchestrator = new ReadCommandOrchestrator(_contextBuilder, _scriptRunner, _executorFactory);
  }

  /// <inheritdoc />
  public override async Task<int> ExecuteAsync(CommandContext context, ReadSettings settings)
  {
    _ = context;
    var cancellationToken = CancellationToken.None;
    return await _orchestrator.ExecuteAsync(settings, cancellationToken).ConfigureAwait(false);
  }
}

