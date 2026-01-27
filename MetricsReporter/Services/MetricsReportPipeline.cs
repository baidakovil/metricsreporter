namespace MetricsReporter.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using MetricsReporter.Processing.Parsers;
using MetricsReporter.Rendering;
using MetricsReporter.Serialization;
using MetricsReporter.Services.DTO;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles parsing metrics sources, building aggregation input, and writing final reports.
/// </summary>
internal sealed class MetricsReportPipeline : IMetricsReportPipeline
{
  private readonly OpenCoverMetricsParser _openCoverParser;
  private readonly RoslynMetricsParser _roslynParser;
  private readonly SarifMetricsParser _sarifParser;

  public MetricsReportPipeline()
    : this(new OpenCoverMetricsParser(), new RoslynMetricsParser(), new SarifMetricsParser())
  {
  }

  internal MetricsReportPipeline(
      OpenCoverMetricsParser openCoverParser,
      RoslynMetricsParser roslynParser,
      SarifMetricsParser sarifParser)
  {
    _openCoverParser = openCoverParser;
    _roslynParser = roslynParser;
    _sarifParser = sarifParser;
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Microsoft.Maintainability",
      "CA1506:Avoid excessive class coupling",
      Justification = "Pipeline orchestrator coordinates document parsing, report generation, and report writing through DTOs and helper methods; further decomposition would fragment the orchestration logic.")]
  public async Task<MetricsReporterExitCode> ExecuteAsync(
      MetricsReporterOptions options,
      ThresholdLoadResult thresholdsResult,
      MetricsReport? baseline,
      List<SuppressedSymbolInfo> suppressedSymbols,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    var documentsResult = await ParseAllDocumentsAsync(options, logger, cancellationToken).ConfigureAwait(false);
    if (documentsResult.ExitCode != MetricsReporterExitCode.Success)
    {
      return documentsResult.ExitCode;
    }

    if (!OpenCoverDocumentValidator.TryValidateUniqueSymbols(documentsResult.OpenCoverDocuments, logger))
    {
      return MetricsReporterExitCode.ParsingError;
    }

    var context = new PipelineExecutionContext(options, thresholdsResult, baseline, suppressedSymbols);
    var report = GenerateReport(context, documentsResult, logger);
    if (report is null)
    {
      return MetricsReporterExitCode.ValidationError;
    }

    return await WriteReportsAsync(report, options, logger, cancellationToken).ConfigureAwait(false);
  }

  private static MetricsReport? GenerateReport(
      PipelineExecutionContext context,
      ParsedDocumentsResult documentsResult,
      ILogger logger)
  {
    var aggregationInput = BuildAggregationInput(context, documentsResult);
    return BuildReportWithLogging(aggregationInput, context.Options, logger);
  }

  private async Task<ParsedDocumentsResult> ParseAllDocumentsAsync(
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    var openCoverDocuments = await ParseOpenCoverDocumentsAsync(options, logger, cancellationToken).ConfigureAwait(false);
    if (openCoverDocuments is null)
    {
      return new ParsedDocumentsResult(MetricsReporterExitCode.ParsingError, [], [], []);
    }

    var roslynDocuments = await ParseRoslynDocumentsAsync(options, logger, cancellationToken).ConfigureAwait(false);
    if (roslynDocuments is null)
    {
      return new ParsedDocumentsResult(MetricsReporterExitCode.ParsingError, openCoverDocuments, [], []);
    }

    var sarifDocuments = await ParseSarifDocumentsAsync(options, logger, cancellationToken).ConfigureAwait(false);
    if (sarifDocuments is null)
    {
      return new ParsedDocumentsResult(MetricsReporterExitCode.ParsingError, openCoverDocuments, roslynDocuments, []);
    }

    logger.LogInformation(
      "Metrics parsed successfully: {OpenCoverCount} OpenCover, {RoslynCount} Roslyn, {SarifCount} Sarif",
      openCoverDocuments.Count,
      roslynDocuments.Count,
      sarifDocuments.Count);

    return new ParsedDocumentsResult(MetricsReporterExitCode.Success, openCoverDocuments, roslynDocuments, sarifDocuments);
  }

  private async Task<IList<ParsedMetricsDocument>?> ParseOpenCoverDocumentsAsync(
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    var documents = new List<ParsedMetricsDocument>();
    foreach (var path in options.OpenCoverPaths)
    {
      if (string.IsNullOrWhiteSpace(path))
      {
        continue;
      }

      var document = await ParseSafeAsync(_openCoverParser, path, logger, cancellationToken).ConfigureAwait(false);
      if (document is null)
      {
        return null;
      }

      documents.Add(document);
    }

    return documents;
  }

  private async Task<IList<ParsedMetricsDocument>?> ParseRoslynDocumentsAsync(
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    var documents = new List<ParsedMetricsDocument>();
    foreach (var path in options.RoslynPaths)
    {
      var document = await ParseSafeAsync(_roslynParser, path, logger, cancellationToken).ConfigureAwait(false);
      if (document is null)
      {
        return null;
      }

      documents.Add(document);
    }

    return documents;
  }

  private async Task<IList<ParsedMetricsDocument>?> ParseSarifDocumentsAsync(
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    var documents = new List<ParsedMetricsDocument>();
    foreach (var path in options.SarifPaths)
    {
      var document = await ParseSafeAsync(_sarifParser, path, logger, cancellationToken).ConfigureAwait(false);
      if (document is null)
      {
        return null;
      }

      documents.Add(document);
    }

    return documents;
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Microsoft.Maintainability",
      "CA1506:Avoid excessive class coupling",
      Justification = "Method constructs aggregation input object from DTOs containing multiple data sources; further decomposition would create artificial factory methods that degrade code readability without architectural benefit.")]
  private static MetricsAggregationInput BuildAggregationInput(
      PipelineExecutionContext context,
      ParsedDocumentsResult documentsResult)
  {
    return new MetricsAggregationInput
    {
      SolutionName = context.Options.SolutionName,
      OpenCoverDocuments = documentsResult.OpenCoverDocuments,
      RoslynDocuments = documentsResult.RoslynDocuments,
      SarifDocuments = documentsResult.SarifDocuments,
      Baseline = context.Baseline,
      Thresholds = context.ThresholdsResult.Configuration.AsDictionary(),
      Paths = new ReportPaths
      {
        MetricsDirectory = context.Options.MetricsDirectory,
        Baseline = context.Options.BaselinePath,
        Report = context.Options.OutputJsonPath,
        Html = context.Options.OutputHtmlPath,
        Thresholds = !string.IsNullOrWhiteSpace(context.Options.ThresholdsPath)
                ? context.Options.ThresholdsPath
                : !string.IsNullOrWhiteSpace(context.Options.ThresholdsJson) ? "(inline thresholds)" : null
      },
      BaselineReference = context.Options.BaselineReference,
      SuppressedSymbols = context.SuppressedSymbols,
      MetricAliases = context.Options.MetricAliases
    };
  }

  private static MetricsReport? BuildReportWithLogging(
      MetricsAggregationInput aggregationInput,
      MetricsReporterOptions options,
      ILogger logger)
  {
    var memberFilter = MemberFilter.FromString(options.ExcludedMemberNamesPatterns);
    var memberKindFilter = MemberKindFilter.Create(
        options.ExcludeMethods,
        options.ExcludeProperties,
        options.ExcludeFields,
        options.ExcludeEvents);
    var assemblyFilter = AssemblyFilter.FromString(options.ExcludedAssemblyNames);
    var typeFilter = TypeFilter.FromString(options.ExcludedTypeNamePatterns);
    var aggregationService = new MetricsAggregationService(memberFilter, memberKindFilter, assemblyFilter, typeFilter);

    try
    {
      return aggregationService.BuildReport(aggregationInput);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to build metrics report for solution {Solution}", options.SolutionName);
      return null;
    }
  }

  private static async Task<MetricsReporterExitCode> WriteReportsAsync(
      MetricsReport report,
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    try
    {
      await ReportWriter.WriteJsonAsync(report, options.OutputJsonPath, cancellationToken).ConfigureAwait(false);
      if (!string.IsNullOrWhiteSpace(options.OutputHtmlPath))
      {
        var html = HtmlReportGenerator.Generate(report, options.CoverageHtmlDir);
        await ReportWriter.WriteHtmlAsync(html, options.OutputHtmlPath, cancellationToken).ConfigureAwait(false);
      }

      return MetricsReporterExitCode.Success;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      logger.LogError(
        ex,
        "Failed to write output files to {OutputJson} and {OutputHtml}",
        options.OutputJsonPath,
        options.OutputHtmlPath);
      return MetricsReporterExitCode.IoError;
    }
  }

  private static async Task<ParsedMetricsDocument?> ParseSafeAsync(
      IMetricsSourceParser parser,
      string path,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    try
    {
      logger.LogDebug("Parsing metrics file {Path}", path);
      return await parser.ParseAsync(path, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to parse metrics file {Path}", path);
      return null;
    }
  }
}


