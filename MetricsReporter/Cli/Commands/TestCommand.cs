using System;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.MetricsReader.Output;
using MetricsReporter.Services.Scripts;
using Spectre.Console.Cli;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Tests whether a single symbol satisfies the specified metric.
/// </summary>
internal sealed class TestCommand : AsyncCommand<TestSettings>
{
  private readonly TestCommandContextBuilder _contextBuilder;
  private readonly ScriptAggregationRunner _scriptRunner;
  private readonly MetricTestResultFactory _resultFactory;
  private readonly TestCommandExecutor _executor;
  internal static readonly char[] MetricScriptSeparators = ['=', ':'];

  public TestCommand(MetricsReporterConfigLoader configLoader, ScriptExecutionService scriptExecutor)
  {
    ArgumentNullException.ThrowIfNull(configLoader);
    ArgumentNullException.ThrowIfNull(scriptExecutor);

    _contextBuilder = new TestCommandContextBuilder(configLoader);
    _scriptRunner = new ScriptAggregationRunner(scriptExecutor);
    _resultFactory = new MetricTestResultFactory();
    _executor = new TestCommandExecutor(_contextBuilder, _scriptRunner, _resultFactory);
  }

  /// <inheritdoc />
  public override async Task<int> ExecuteAsync(CommandContext context, TestSettings settings)
  {
    _ = context;
    var cancellationToken = CancellationToken.None;
    return await _executor.ExecuteAsync(settings, cancellationToken).ConfigureAwait(false);
  }
}

