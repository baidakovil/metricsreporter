namespace MetricsReporter.Processing.Parsers;

using MetricsReporter.Model;

/// <summary>
/// Represents the normalized source location for a SARIF violation.
/// </summary>
internal sealed record SarifLocation(SourceLocation Source, string? OriginalUri);


