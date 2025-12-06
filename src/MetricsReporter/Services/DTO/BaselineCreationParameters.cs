namespace MetricsReporter.Services.DTO;

/// <summary>
/// Parameters for creating baseline from previous report.
/// </summary>
/// <param name="PreviousReportPath">Path to the previous metrics report JSON file.</param>
/// <param name="BaselinePath">Path to the baseline JSON file that will be created.</param>
internal sealed record BaselineCreationParameters(
    string PreviousReportPath,
    string BaselinePath);


