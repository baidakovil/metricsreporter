using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.Services.Scripts;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Generates metrics reports (JSON/HTML) from AltCover, Roslyn, and SARIF inputs.
/// </summary>
internal sealed class GenerateCommand : AsyncCommand<GenerateSettings>
{
  private readonly GenerateCommandContextBuilder _contextBuilder;
  private readonly GenerateScriptRunner _scriptRunner;
  private readonly GenerateApplicationRunner _applicationRunner;
  private readonly GenerateCommandOrchestrator _orchestrator;

  /// <summary>
  /// Initializes a new instance of the <see cref="GenerateCommand"/> class.
  /// </summary>
  /// <param name="configLoader">Configuration loader.</param>
  /// <param name="scriptExecutor">Script executor.</param>
  public GenerateCommand(MetricsReporterConfigLoader configLoader, ScriptExecutionService scriptExecutor)
  {
    ArgumentNullException.ThrowIfNull(configLoader);
    ArgumentNullException.ThrowIfNull(scriptExecutor);

    _contextBuilder = new GenerateCommandContextBuilder(configLoader);
    _scriptRunner = new GenerateScriptRunner(scriptExecutor);
    _applicationRunner = new GenerateApplicationRunner();
    _orchestrator = new GenerateCommandOrchestrator(_contextBuilder, _scriptRunner, _applicationRunner);
  }

  /// <inheritdoc />
  public override async Task<int> ExecuteAsync(CommandContext context, GenerateSettings settings)
  {
    _ = context;
    var cancellationToken = CancellationToken.None;
    return await _orchestrator.ExecuteAsync(settings, cancellationToken).ConfigureAwait(false);
  }
}

/// <summary>
/// Represents resolved inputs for the generate command across CLI, environment, and config sources.
/// </summary>
internal sealed record ResolvedGenerateInputs(
  string? SolutionName,
  string? MetricsDir,
  string[] AltCover,
  string[] Roslyn,
  string[] Sarif,
  string? Baseline,
  string? BaselineReference,
  string? OutputJson,
  string? OutputHtml,
  string? ThresholdsFile,
  string? ThresholdsInline,
  string? InputJson,
  string? ExcludedMembers,
  string? ExcludedAssemblies,
  string? ExcludedTypes,
  bool ReplaceBaseline,
  string? BaselineStoragePath,
  string? CoverageHtmlDir,
  bool AnalyzeSuppressedSymbols,
  string? SuppressedSymbols,
  string? SolutionDirectory,
  string[] SourceCodeFolders,
  bool MetricsDirProvided);
