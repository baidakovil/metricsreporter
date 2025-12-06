namespace MetricsReporter.Services.DTO;

/// <summary>
/// Parameters for replacing baseline with new report.
/// </summary>
/// <param name="ReportPath">Path to the metrics report JSON file that will become the baseline.</param>
/// <param name="BaselinePath">Path to the baseline JSON file that will be created or replaced.</param>
/// <param name="StoragePath">Directory path where the old baseline will be archived with a timestamp.</param>
internal sealed record BaselineReplacementParameters(
    string ReportPath,
    string BaselinePath,
    string? StoragePath);


