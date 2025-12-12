namespace MetricsReporter.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Aggregation;
using MetricsReporter.Configuration;
using MetricsReporter.Model;
using MetricsReporter.Rendering;
using MetricsReporter.Serialization;
using MetricsReporter.Services.DTO;
using MetricsReporter.Logging;
using Microsoft.Extensions.Logging;

/// <summary>
/// Coordinates the aggregation workflow and report generation.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Application service orchestrates metrics aggregation workflow coordinating multiple services through interfaces; further decomposition would fragment the coordination logic and degrade maintainability.")]
public sealed class MetricsReporterApplication
{
  private readonly IMetricsReportPipeline _reportPipeline;
  private readonly IBaselineLifecycleService _baselineLifecycle;
  private readonly ISuppressedSymbolsService _suppressedSymbolsService;

  /// <summary>
  /// Initializes a new instance of the <see cref="MetricsReporterApplication"/> class.
  /// </summary>
  public MetricsReporterApplication()
    : this(new MetricsReportPipeline(), new BaselineLifecycleService(), new SuppressedSymbolsService())
  {
  }

  internal MetricsReporterApplication(
      IMetricsReportPipeline reportPipeline,
      IBaselineLifecycleService baselineLifecycle,
      ISuppressedSymbolsService suppressedSymbolsService)
  {
    _reportPipeline = reportPipeline;
    _baselineLifecycle = baselineLifecycle;
    _suppressedSymbolsService = suppressedSymbolsService;
  }

  /// <summary>
  /// Executes the aggregation process.
  /// </summary>
  /// <param name="options">The options for the metrics reporter.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <returns>The exit code indicating success or failure.</returns>
  public async Task<MetricsReporterExitCode> RunAsync(MetricsReporterOptions options, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(options);

    var minimumLevel = LoggerFactoryBuilder.FromVerbosity(options.Verbosity);
    using var loggerFactory = LoggerFactoryBuilder.Create(options.LogFilePath, minimumLevel);
    return await RunAsync(options, loggerFactory, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Executes the aggregation process with a preconfigured logger factory.
  /// </summary>
  public async Task<MetricsReporterExitCode> RunAsync(
    MetricsReporterOptions options,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(loggerFactory);

    var logger = loggerFactory.CreateLogger<MetricsReporterApplication>();
    using var scope = logger.BeginScope(new Dictionary<string, object?>
    {
      ["solution"] = options.SolutionName,
      ["metricsDirectory"] = options.MetricsDirectory,
      ["outputJson"] = options.OutputJsonPath,
      ["outputHtml"] = options.OutputHtmlPath,
      ["baseline"] = options.BaselinePath
    });

    logger.LogInformation("Metrics Reporter started.");

    ConfigurationWarningEvaluator.LogWarnings(options, options.CommandName, logger);

    LogCommandLineArguments(logger);

    // If input JSON is specified, load it and generate HTML only
    if (!string.IsNullOrWhiteSpace(options.InputJsonPath))
    {
      return await GenerateHtmlFromJsonAsync(options, logger, cancellationToken).ConfigureAwait(false);
    }

    var validationResult = ValidateOptionsWithLogging(options, logger);
    if (validationResult != MetricsReporterExitCode.Success)
    {
      return validationResult;
    }

    var thresholdsResult = LoadThresholdsWithLogging(options, logger);
    if (thresholdsResult.ExitCode != MetricsReporterExitCode.Success)
    {
      return thresholdsResult.ExitCode;
    }

    var baselineLogger = loggerFactory.CreateLogger<BaselineLifecycleService>();
    var suppressedLogger = loggerFactory.CreateLogger<SuppressedSymbolsService>();
    var pipelineLogger = loggerFactory.CreateLogger<MetricsReportPipeline>();

    var baselineContext = await InitializeBaselineContextAsync(options, baselineLogger, cancellationToken).ConfigureAwait(false);

    var reportGenerationContext = new ReportGenerationContext(options, thresholdsResult, baselineContext);
    var executionResult = await ExecuteReportGenerationAsync(
        reportGenerationContext,
        pipelineLogger,
        suppressedLogger,
        baselineLogger,
        cancellationToken).ConfigureAwait(false);
    if (executionResult != MetricsReporterExitCode.Success)
    {
      return executionResult;
    }

    logger.LogInformation("Metrics Reporter completed successfully.");
    return MetricsReporterExitCode.Success;
  }

  private async Task<BaselineRunContext> InitializeBaselineContextAsync(MetricsReporterOptions options, ILogger logger, CancellationToken cancellationToken)
  {
    var baselineContext = _baselineLifecycle.CaptureContext(options);
    _baselineLifecycle.LogContext(baselineContext, options, logger);
    await _baselineLifecycle.InitializeBaselineAsync(baselineContext, options, logger, cancellationToken).ConfigureAwait(false);
    return baselineContext;
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Microsoft.Maintainability",
      "CA1506:Avoid excessive class coupling",
      Justification = "Report generation orchestrator coordinates baseline lifecycle, suppressed symbols resolution, and pipeline execution through interfaces and DTOs; further decomposition would create artificial wrapper methods that degrade code readability without architectural benefit.")]
  private async Task<MetricsReporterExitCode> ExecuteReportGenerationAsync(
      ReportGenerationContext context,
      ILogger pipelineLogger,
      ILogger suppressedLogger,
      ILogger baselineLogger,
      CancellationToken cancellationToken)
  {
    var baseline = await _baselineLifecycle.LoadBaselineAsync(context.Options.BaselinePath, cancellationToken).ConfigureAwait(false);
    var suppressedSymbols = await _suppressedSymbolsService.ResolveAsync(context.Options, suppressedLogger, cancellationToken).ConfigureAwait(false);

    var pipelineResult = await _reportPipeline.ExecuteAsync(
        context.Options,
        context.ThresholdsResult,
        baseline,
        suppressedSymbols,
        pipelineLogger,
        cancellationToken).ConfigureAwait(false);
    if (pipelineResult != MetricsReporterExitCode.Success)
    {
      return pipelineResult;
    }

    await _baselineLifecycle.ReplaceBaselineAsync(context.BaselineContext, context.Options, baselineLogger, cancellationToken).ConfigureAwait(false);
    return MetricsReporterExitCode.Success;
  }

  private static void LogCommandLineArguments(ILogger logger)
  {
    try
    {
      var cliArgs = Environment.GetCommandLineArgs();
      logger.LogInformation("CLI args: {Args}", string.Join(" | ", cliArgs));
    }
    catch (Exception)
    {
      // Swallow any environment-related exceptions; argument logging is best-effort only.
    }
  }


  /// <summary>
  /// Validates options and logs any errors.
  /// </summary>
  /// <param name="options">The options to validate.</param>
  /// <param name="logger">The logger to use for error messages.</param>
  /// <returns>The exit code indicating validation result.</returns>
  private static MetricsReporterExitCode ValidateOptionsWithLogging(MetricsReporterOptions options, ILogger logger)
  {
    try
    {
      ValidateOptions(options);
      return MetricsReporterExitCode.Success;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Options validation failed: {Message}", ex.Message);
      return MetricsReporterExitCode.ValidationError;
    }
  }

  /// <summary>
  /// Loads thresholds and logs any errors.
  /// </summary>
  /// <param name="options">The options containing threshold configuration.</param>
  /// <param name="logger">The logger to use for error messages.</param>
  /// <returns>A result containing the exit code and loaded thresholds.</returns>
  private static ThresholdLoadResult LoadThresholdsWithLogging(
      MetricsReporterOptions options,
      ILogger logger)
  {
    try
    {
      var thresholds = ParseThresholds(options);
      return new ThresholdLoadResult(MetricsReporterExitCode.Success, ThresholdConfiguration.From(thresholds));
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to load thresholds: {Message}", ex.Message);
      return new ThresholdLoadResult(MetricsReporterExitCode.ValidationError, ThresholdConfiguration.Empty);
    }
  }

  private static void ValidateOptions(MetricsReporterOptions options)
  {
    // Skip validation if using input JSON mode
    if (!string.IsNullOrWhiteSpace(options.InputJsonPath))
    {
      return;
    }

    if (string.IsNullOrWhiteSpace(options.OutputJsonPath))
    {
      throw new ArgumentException("Output JSON path is required.", nameof(options));
    }

    if (string.IsNullOrWhiteSpace(options.MetricsDirectory))
    {
      throw new ArgumentException("Metrics directory is required.", nameof(options));
    }

    foreach (var path in EnumerateInputFiles(options))
    {
      if (!File.Exists(path))
      {
        throw new FileNotFoundException($"Input file not found: {path}", path);
      }
    }

    if (!string.IsNullOrWhiteSpace(options.OutputHtmlPath))
    {
      var htmlDir = Path.GetDirectoryName(options.OutputHtmlPath);
      if (!string.IsNullOrWhiteSpace(htmlDir) && !Directory.Exists(htmlDir))
      {
        Directory.CreateDirectory(htmlDir);
      }
    }
  }

  private static IEnumerable<string> EnumerateInputFiles(MetricsReporterOptions options)
  {
    foreach (var path in options.AltCoverPaths)
    {
      if (!string.IsNullOrWhiteSpace(path))
      {
        yield return path;
      }
    }

    foreach (var path in options.RoslynPaths)
    {
      yield return path;
    }

    foreach (var path in options.SarifPaths)
    {
      yield return path;
    }
  }

  private static Dictionary<MetricIdentifier, MetricThresholdDefinition> ParseThresholds(
      MetricsReporterOptions options)
  {
    string? payload;

    if (!string.IsNullOrWhiteSpace(options.ThresholdsPath))
    {
      var absolutePath = Path.GetFullPath(options.ThresholdsPath);
      if (!File.Exists(absolutePath))
      {
        throw new FileNotFoundException($"Thresholds file not found: {absolutePath}", absolutePath);
      }

      payload = File.ReadAllText(absolutePath);
    }
    else
    {
      payload = options.ThresholdsJson;
    }

    return ThresholdsParser.Parse(payload);
  }

  /// <summary>
  /// Loads an existing JSON report and generates HTML from it without parsing source files.
  /// </summary>
  private static async Task<MetricsReporterExitCode> GenerateHtmlFromJsonAsync(
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    var validationResult = ValidateHtmlGenerationOptions(options, logger);
    if (validationResult != MetricsReporterExitCode.Success)
    {
      return validationResult;
    }

    try
    {
      var report = await LoadReportForHtmlGenerationAsync(options, logger, cancellationToken).ConfigureAwait(false);
      if (report is null)
      {
        return MetricsReporterExitCode.ValidationError;
      }

      return await GenerateAndWriteHtmlAsync(report, options, logger, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      logger.LogError(ex, "Failed to write HTML output file to {OutputHtmlPath}", options.OutputHtmlPath);
      return MetricsReporterExitCode.IoError;
    }
  }

  /// <summary>
  /// Validates options required for HTML generation from JSON.
  /// </summary>
  /// <param name="options">The options to validate.</param>
  /// <param name="logger">The logger to use for error messages.</param>
  /// <returns>The exit code indicating validation result.</returns>
  private static MetricsReporterExitCode ValidateHtmlGenerationOptions(MetricsReporterOptions options, ILogger logger)
  {
    if (string.IsNullOrWhiteSpace(options.InputJsonPath))
    {
      logger.LogError("Input JSON path is required for HTML-only generation.");
      return MetricsReporterExitCode.ValidationError;
    }

    if (string.IsNullOrWhiteSpace(options.OutputHtmlPath))
    {
      logger.LogError("Output HTML path is required for HTML-only generation.");
      return MetricsReporterExitCode.ValidationError;
    }

    return MetricsReporterExitCode.Success;
  }

  /// <summary>
  /// Loads a metrics report from JSON for HTML generation.
  /// </summary>
  /// <param name="options">The options containing the JSON file path.</param>
  /// <param name="logger">The logger to use for progress messages.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <returns>The loaded metrics report, or <see langword="null"/> if loading failed.</returns>
  private static async Task<MetricsReport?> LoadReportForHtmlGenerationAsync(
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    var startedAt = DateTimeOffset.UtcNow;
    try
    {
      logger.LogInformation("Loading metrics report from {InputJsonPath}", options.InputJsonPath);
      var report = await JsonReportLoader.LoadAsync(options.InputJsonPath!, cancellationToken).ConfigureAwait(false);

      if (report is null)
      {
        logger.LogError("Failed to deserialize metrics report from JSON.");
        return null;
      }

      var duration = DateTimeOffset.UtcNow - startedAt;
      logger.LogInformation(
        "Loaded metrics report from {InputJsonPath} in {DurationMs:N0} ms",
        options.InputJsonPath,
        duration.TotalMilliseconds);
      return report;
    }
    catch (FileNotFoundException ex)
    {
      logger.LogError(ex, "Input JSON file not found at {InputJsonPath}", options.InputJsonPath);
      return null;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to load JSON report from {InputJsonPath}", options.InputJsonPath);
      return null;
    }
  }

  /// <summary>
  /// Generates HTML from a metrics report and writes it to disk.
  /// </summary>
  /// <param name="report">The metrics report to generate HTML from.</param>
  /// <param name="options">The options containing output path.</param>
  /// <param name="logger">The logger to use for progress messages.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <returns>The exit code indicating success or failure.</returns>
  private static async Task<MetricsReporterExitCode> GenerateAndWriteHtmlAsync(
      MetricsReport report,
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    var startedAt = DateTimeOffset.UtcNow;
    logger.LogInformation("Generating HTML report...");
    var html = HtmlReportGenerator.Generate(report);

    await ReportWriter.WriteHtmlAsync(html, options.OutputHtmlPath, cancellationToken).ConfigureAwait(false);
    var duration = DateTimeOffset.UtcNow - startedAt;
    logger.LogInformation(
      "HTML report generated successfully at {OutputHtmlPath} in {DurationMs:N0} ms",
      options.OutputHtmlPath,
      duration.TotalMilliseconds);
    return MetricsReporterExitCode.Success;
  }

}


