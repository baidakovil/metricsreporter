namespace MetricsReporter.Services.DTO;

using System.Collections.Generic;
using MetricsReporter.Configuration;
using MetricsReporter.Model;

/// <summary>
/// Execution context for metrics report pipeline.
/// </summary>
/// <param name="Options">Metrics reporter options.</param>
/// <param name="ThresholdsResult">Loaded threshold configuration result.</param>
/// <param name="Baseline">Baseline report for delta calculation.</param>
/// <param name="SuppressedSymbols">List of suppressed symbols.</param>
internal sealed record PipelineExecutionContext(
    MetricsReporterOptions Options,
    ThresholdLoadResult ThresholdsResult,
    MetricsReport? Baseline,
    List<SuppressedSymbolInfo> SuppressedSymbols);


