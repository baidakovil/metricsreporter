using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Logging;
using MetricsReporter.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Coordinates generate scripts and the main application run to produce metrics outputs.
/// </summary>
/// <remarks>
/// This pipeline keeps the generate command flow linear: attempt scripts first and, regardless
/// of <c>--aggregate-after-scripts</c>, always run aggregation since generation requires it.
/// </remarks>
internal sealed class GenerateCommandPipeline
{
  private readonly GenerateScriptRunner _scriptRunner;
  private readonly GenerateApplicationRunner _applicationRunner;

  public GenerateCommandPipeline(GenerateScriptRunner scriptRunner, GenerateApplicationRunner applicationRunner)
  {
    _scriptRunner = scriptRunner ?? throw new System.ArgumentNullException(nameof(scriptRunner));
    _applicationRunner = applicationRunner ?? throw new System.ArgumentNullException(nameof(applicationRunner));
  }

  /// <summary>
  /// Executes generate scripts and, if they succeed, runs the application aggregation.
  /// </summary>
  /// <param name="context">Resolved generate command context.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>Exit code from scripts (if any) or from the application run.</returns>
  public async Task<int> ExecuteAsync(GenerateCommandContext context, CancellationToken cancellationToken)
  {
    // Log start message immediately so user knows the command has started
    var minimumLevel = LoggerFactoryBuilder.FromVerbosity(context.GeneralOptions.Verbosity);
    using var loggerFactory = LoggerFactoryBuilder.Create(context.LogPath, minimumLevel, verbosity: context.GeneralOptions.Verbosity);
    var logger = loggerFactory.CreateLogger<GenerateCommandPipeline>();
    logger.LogInformation("Starting metrics report generation");
    
    var stopwatch = Stopwatch.StartNew();
    var scriptExitCode = await _scriptRunner.RunAsync(context, cancellationToken).ConfigureAwait(false);
    if (scriptExitCode.HasValue)
    {
      stopwatch.Stop();
      return scriptExitCode.Value;
    }

    if (!context.GeneralOptions.AggregateAfterScripts)
    {
      AnsiConsole.MarkupLine("[yellow]--aggregate-after-scripts=false is ignored for generate; aggregation will still run.[/]");
    }

    var exitCode = await _applicationRunner.RunAsync(context.Options, cancellationToken).ConfigureAwait(false);
    stopwatch.Stop();

    if (exitCode == (int)MetricsReporterExitCode.Success)
    {
      // If operation succeeded, files were written. JSON is always written, HTML only if path is specified.
      var generatedFiles = GetGeneratedFiles(context.Options);
      var filesText = string.Join(", ", generatedFiles);
      
      logger.LogInformation("Metrics generated: {Files} in {DurationSeconds:F0}s", filesText, stopwatch.Elapsed.TotalSeconds);
    }

    return exitCode;
  }

  /// <summary>
  /// Determines which files were generated based on operation success and configuration.
  /// </summary>
  /// <remarks>
  /// This method relies on the fact that if the operation succeeded, files were written.
  /// JSON is always written when operation succeeds, HTML only if the path is specified.
  /// This is more reliable than checking file modification times, which can be inaccurate
  /// due to filesystem precision, timezone issues, or external modifications.
  /// </remarks>
  private static string[] GetGeneratedFiles(MetricsReporterOptions options)
  {
    var files = new List<string>();
    
    // JSON is always written when operation succeeds (it's required)
    if (!string.IsNullOrWhiteSpace(options.OutputJsonPath))
    {
      files.Add("json");
    }
    
    // HTML is only written if path is specified
    if (!string.IsNullOrWhiteSpace(options.OutputHtmlPath))
    {
      files.Add("html");
    }
    
    return files.ToArray();
  }
}

