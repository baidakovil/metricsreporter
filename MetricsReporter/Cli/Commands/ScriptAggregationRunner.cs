using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Configuration;
using MetricsReporter.Services;
using MetricsReporter.Services.Scripts;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Executes command scripts and optional post-script aggregation in a single orchestrated flow.
/// </summary>
internal sealed class ScriptAggregationRunner
{
  private readonly ScriptExecutionService _scriptExecutor;
  private readonly IScriptRunNotifier _notifier;
  private readonly IGenerateScriptLoggerFactory _loggerFactory;
  private readonly IGenerateScriptExecutionClient _executionClient;
  private readonly IScriptExecutionGuard _guard;
  private readonly GenerateScriptExecutionPipeline _pipeline;

  public ScriptAggregationRunner(ScriptExecutionService scriptExecutor)
  {
    _scriptExecutor = scriptExecutor ?? throw new ArgumentNullException(nameof(scriptExecutor));
    _notifier = new ScriptRunNotifier();
    _loggerFactory = new GenerateScriptLoggerFactory();
    _executionClient = new GenerateScriptExecutionClient(_scriptExecutor, _loggerFactory);
    _guard = new ScriptExecutionGuard(_notifier);
    _pipeline = new GenerateScriptExecutionPipeline(_guard, _executionClient);
  }

  /// <summary>
  /// Runs scripts for the current command and performs aggregation when configured.
  /// </summary>
  /// <param name="context">Script aggregation context describing inputs and behaviors.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>Exit code when a step fails; otherwise <see langword="null"/>.</returns>
  public async Task<int?> RunAsync(ScriptAggregationContext context, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(context);

    var plan = CreateExecutionPlan(context);
    var scriptExit = await ExecuteScriptsAsync(context, plan, cancellationToken).ConfigureAwait(false);
    if (scriptExit.HasValue)
    {
      return scriptExit;
    }

    var aggregationExit = await ExecuteAggregationAsync(context, plan.HasScripts, cancellationToken).ConfigureAwait(false);
    if (aggregationExit.HasValue)
    {
      return aggregationExit;
    }

    return null;
  }

  private static ScriptExecutionPlan CreateExecutionPlan(ScriptAggregationContext context)
  {
    var scriptsToRun = context.ScriptSelector(context.Scripts, context.Metrics);
    var logPath = Path.Combine(Path.GetDirectoryName(context.ReportPath) ?? context.General.WorkingDirectory, context.LogFileName);
    return new ScriptExecutionPlan(scriptsToRun, scriptsToRun.Length > 0, logPath);
  }

  private async Task<int?> ExecuteScriptsAsync(
    ScriptAggregationContext context,
    ScriptExecutionPlan plan,
    CancellationToken cancellationToken)
  {
    var request = new GenerateScriptRunRequest(
      context.General.RunScripts,
      plan.HasScripts,
      plan.LogPath,
      context.General.Verbosity,
      plan.Scripts,
      context.General.WorkingDirectory,
      context.General.Timeout,
      context.General.LogTruncationLimit);

    return await _pipeline.ExecuteAsync(request, cancellationToken, context.OperationName).ConfigureAwait(false);
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Aggregation orchestration must coordinate path resolution, validation, logging, and application execution; additional splitting would complicate the command flow without reducing dependencies.")]
  private static async Task<int?> ExecuteAggregationAsync(ScriptAggregationContext context, bool hasScripts, CancellationToken cancellationToken)
  {
    var shouldAggregate = context.General.RunScripts && context.General.AggregateAfterScripts && hasScripts;
    if (shouldAggregate)
    {
      var aggregationInputs = AggregationOptionsResolver.Resolve(
        context.EnvironmentConfig.Paths,
        context.FileConfig.Paths,
        context.General.WorkingDirectory,
        context.ReportPath);
      var aggregationValidation = AggregationOptionsResolver.Validate(aggregationInputs);
      if (!aggregationValidation.Succeeded)
      {
        AnsiConsole.MarkupLine($"[red]{aggregationValidation.Error}[/]");
        return (int)MetricsReporterExitCode.ValidationError;
      }

      var aggregationLogPath = AggregationOptionsResolver.BuildLogPath(aggregationInputs, context.General.WorkingDirectory);
      var aggregationOptions = AggregationOptionsResolver.BuildOptions(aggregationInputs, aggregationLogPath, context.General.Verbosity);
      var application = new MetricsReporterApplication();
      var aggregationExit = await application.RunAsync(aggregationOptions, cancellationToken).ConfigureAwait(false);
      if (aggregationExit != MetricsReporterExitCode.Success)
      {
        AnsiConsole.MarkupLine($"[red]Aggregation after scripts failed with exit code {(int)aggregationExit}.[/]");
        return (int)aggregationExit;
      }
    }
    else if (!context.General.AggregateAfterScripts && hasScripts)
    {
      AnsiConsole.MarkupLine("[yellow]Aggregation after scripts disabled (--aggregate-after-scripts=false).[/]");
    }
    else if (!hasScripts && context.General.AggregateAfterScripts)
    {
      AnsiConsole.MarkupLine($"[yellow]No {context.OperationName} scripts configured; skipping post-script aggregation.[/]");
    }

    return null;
  }
}

/// <summary>
/// Describes inputs required to execute command scripts and optional aggregation.
/// </summary>
/// <param name="General">Resolved general options.</param>
/// <param name="EnvironmentConfig">Configuration read from environment.</param>
/// <param name="FileConfig">Configuration read from file.</param>
/// <param name="Scripts">Scripts available for execution.</param>
/// <param name="Metrics">Requested metrics.</param>
/// <param name="ReportPath">Report path used for aggregation.</param>
/// <param name="ScriptSelector">Delegate selecting scripts to run for the operation.</param>
/// <param name="OperationName">Friendly operation name for messaging.</param>
/// <param name="LogFileName">Log file name for script execution.</param>
internal sealed record ScriptAggregationContext(
  ResolvedGeneralOptions General,
  MetricsReporterConfiguration EnvironmentConfig,
  MetricsReporterConfiguration FileConfig,
  ResolvedScripts Scripts,
  IEnumerable<string> Metrics,
  string ReportPath,
  Func<ResolvedScripts, IEnumerable<string>, string[]> ScriptSelector,
  string OperationName,
  string LogFileName);

/// <summary>
/// Represents the scripts selected for execution and associated log path.
/// </summary>
/// <param name="Scripts">Scripts to execute.</param>
/// <param name="HasScripts">Indicates whether any scripts are present.</param>
/// <param name="LogPath">Log path for script execution.</param>
internal sealed record ScriptExecutionPlan(
  string[] Scripts,
  bool HasScripts,
  string LogPath);

