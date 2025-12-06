namespace MetricsReporter.MetricsReader.Settings;

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Base settings shared by all metrics-reader commands.
/// </summary>
internal abstract class MetricsReaderSettingsBase : CommandSettings
{
  private const string DefaultReportPath = "build/Metrics/Report/MetricsReport.g.json";

  [CommandOption("--report <PATH>")]
  [Description("Path to MetricsReport.g.json. Defaults to build/Metrics/Report/MetricsReport.g.json.")]
  public string ReportPath { get; init; } = DefaultReportPath;

  [CommandOption("--thresholds-file <PATH>")]
  [Description("Optional thresholds file (MetricsReporterThresholds.json) used to override report metadata.")]
  public string? ThresholdsFile { get; init; }

  [CommandOption("--include-suppressed")]
  [Description("Include metrics that have been suppressed via SuppressMessage attributes.")]
  public bool IncludeSuppressed { get; init; }

  [CommandOption("--no-update")]
  [Description("Skips rebuilding metrics before reading the report. By default metrics are regenerated.")]
  public bool NoUpdate { get; init; }

  /// <inheritdoc />
  public override ValidationResult Validate()
  {
    if (string.IsNullOrWhiteSpace(ReportPath))
    {
      return ValidationResult.Error("--report must point to MetricsReport.g.json.");
    }

    return ValidationResult.Success();
  }
}


