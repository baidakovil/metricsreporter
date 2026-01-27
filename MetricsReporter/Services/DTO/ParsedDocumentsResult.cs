namespace MetricsReporter.Services.DTO;

using System.Collections.Generic;
using MetricsReporter;
using MetricsReporter.Processing;

/// <summary>
/// Result of parsing all metrics documents from different sources.
/// </summary>
/// <param name="ExitCode">Exit code indicating parsing status.</param>
/// <param name="OpenCoverDocuments">Parsed OpenCover documents.</param>
/// <param name="RoslynDocuments">Parsed Roslyn documents.</param>
/// <param name="SarifDocuments">Parsed SARIF documents.</param>
internal sealed record ParsedDocumentsResult(
    MetricsReporterExitCode ExitCode,
    IList<ParsedMetricsDocument> OpenCoverDocuments,
    IList<ParsedMetricsDocument> RoslynDocuments,
    IList<ParsedMetricsDocument> SarifDocuments);


