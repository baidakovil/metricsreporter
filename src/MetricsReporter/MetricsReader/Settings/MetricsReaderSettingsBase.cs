namespace MetricsReporter.MetricsReader.Settings;

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Base settings shared by all metrics-reader commands.
/// </summary>
internal abstract class MetricsReaderSettingsBase : CommandSettings
{
  [CommandOption("--report <PATH>")]
  [Description("Path to MetricsReport.g.json. Can be provided via CLI, env, or config.")]
  public string? ReportPath { get; init; }

  [CommandOption("--thresholds-file <PATH>")]
  [Description("Optional thresholds file (MetricsReporterThresholds.json) used to override report metadata.")]
  public string? ThresholdsFile { get; init; }

  [CommandOption("--include-suppressed")]
  [Description("Include metrics that have been suppressed via SuppressMessage attributes.")]
  public bool IncludeSuppressed { get; init; }

  /// <inheritdoc />
  public override ValidationResult Validate()
  {
    return ValidationResult.Success();
  }
}


