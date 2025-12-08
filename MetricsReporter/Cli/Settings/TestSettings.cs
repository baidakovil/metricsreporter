using System.Collections.Generic;
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;

namespace MetricsReporter.Cli.Settings;

/// <summary>
/// Settings for the test command.
/// </summary>
internal sealed class TestSettings : CliSettingsBase
{
  [CommandOption("--report <PATH>")]
  [Description("Path to MetricsReport.g.json (can be provided via config).")]
  public string? Report { get; init; }

  [CommandOption("--symbol <FQN>")]
  [Description("Fully qualified symbol name (type or member).")]
  public string? Symbol { get; init; }

  [CommandOption("--metric <NAME>")]
  [Description("Metric identifier or alias to verify (e.g. Complexity, Coupling).")]
  public string? Metric { get; init; }

  [CommandOption("--thresholds-file <PATH>")]
  [Description("Optional thresholds file (MetricsReporterThresholds.json) used to override report metadata.")]
  public string? ThresholdsFile { get; init; }

  [CommandOption("--include-suppressed")]
  [Description("Include metrics that have been suppressed via SuppressMessage attributes.")]
  public bool IncludeSuppressed { get; init; }

  [CommandOption("--script <PATH>")]
  [Description("PowerShell script executed before reading. Repeat for multiple scripts.")]
  public List<string> Scripts { get; init; } = [];

  [CommandOption("--metric-script <METRIC=PATH>")]
  [Description("PowerShell script executed when the specified metric is requested. Repeatable.")]
  public List<string> MetricScripts { get; init; } = [];

  /// <inheritdoc />
  public override ValidationResult Validate()
  {
    var baseResult = base.Validate();
    if (!baseResult.Successful)
    {
      return baseResult;
    }

    if (string.IsNullOrWhiteSpace(Symbol))
    {
      return ValidationResult.Error("--symbol is required.");
    }

    if (string.IsNullOrWhiteSpace(Metric))
    {
      return ValidationResult.Error("--metric is required.");
    }

    return ValidationResult.Success();
  }
}

