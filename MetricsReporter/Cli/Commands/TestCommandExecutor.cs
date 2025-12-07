using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Cli.Settings;
using MetricsReporter.MetricsReader.Output;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Orchestrates the test command by building context, running scripts, and evaluating metrics.
/// </summary>
internal sealed class TestCommandExecutor : ITestCommandExecutor
{
  private readonly TestCommandContextBuilder _contextBuilder;
  private readonly ScriptAggregationRunner _scriptRunner;
  private readonly MetricTestEvaluator _metricEvaluator;
  private readonly TestScriptContextFactory _scriptContextFactory;

  /// <summary>
  /// Initializes a new instance of the <see cref="TestCommandExecutor"/> class.
  /// </summary>
  /// <param name="contextBuilder">Context builder resolving configuration and inputs.</param>
  /// <param name="scriptRunner">Runner that executes pre/post scripts and aggregation.</param>
  /// <param name="resultFactory">Factory that creates structured metric test results.</param>
  public TestCommandExecutor(
    TestCommandContextBuilder contextBuilder,
    ScriptAggregationRunner scriptRunner,
    MetricTestResultFactory resultFactory)
  {
    _contextBuilder = contextBuilder ?? throw new System.ArgumentNullException(nameof(contextBuilder));
    _scriptRunner = scriptRunner ?? throw new System.ArgumentNullException(nameof(scriptRunner));
    _metricEvaluator = new MetricTestEvaluator(resultFactory ?? throw new System.ArgumentNullException(nameof(resultFactory)));
    _scriptContextFactory = new TestScriptContextFactory();
  }

  /// <inheritdoc />
  /// <inheritdoc />
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Executor coordinates context building, script execution, engine creation, and result projection; the coupling is inherent to the orchestration step.")]
  public async Task<int> ExecuteAsync(TestSettings settings, CancellationToken cancellationToken)
  {
    var buildResult = _contextBuilder.Build(settings);
    if (!buildResult.Succeeded)
    {
      return buildResult.ExitCode ?? (int)MetricsReporterExitCode.ValidationError;
    }

    var commandContext = buildResult.Context!;
    var scriptContext = _scriptContextFactory.Create(commandContext);

    var exitCode = await _scriptRunner.RunAsync(scriptContext, cancellationToken).ConfigureAwait(false);
    if (exitCode.HasValue)
    {
      return exitCode.Value;
    }

    return await _metricEvaluator.EvaluateAsync(commandContext, cancellationToken).ConfigureAwait(false);
  }
}

