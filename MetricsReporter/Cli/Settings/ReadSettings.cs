using System.Collections.Generic;
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;

namespace MetricsReporter.Cli.Settings;

/// <summary>
/// Settings for the read command (metric violations).
/// </summary>
internal sealed class ReadSettings : CliSettingsBase
{
  [CommandOption("--report <PATH>")]
  [Description("Path to MetricsReport.g.json (can be provided via config).")]
  public string? Report { get; init; }

  [CommandOption("--namespace <NAME>")]
  [Description("Namespace prefix to filter symbols (e.g. Sample.Loader.Infrastructure).")]
  public string? Namespace { get; init; }

  [CommandOption("--metric <NAME>")]
  [Description("Metric identifier or alias (Complexity, Coupling, Maintainability, etc.).")]
  public string? Metric { get; init; }

  [CommandOption("--symbol-kind <Any|Type|Member>")]
  [Description("Symbol level to inspect. Defaults to Any.")]
  public MetricsReaderSymbolKind SymbolKind { get; init; } = MetricsReaderSymbolKind.Any;

  [CommandOption("--all")]
  [Description("Emit all matching entries instead of only the most severe one.")]
  public bool ShowAll { get; init; }

  [CommandOption("--ruleid <ID>")]
  [Description("Optional SARIF rule identifier filter (e.g. CA1506, IDE0051).")]
  public string? RuleId { get; init; }

  [CommandOption("--group-by <metric|method|type|namespace>")]
  [Description("Controls grouping of violations (metric, namespace, type, method).")]
  public MetricsReaderGroupByOption? GroupBy { get; init; }

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

    if (!string.IsNullOrWhiteSpace(GroupBy?.ToString()) && GroupBy == MetricsReaderGroupByOption.RuleId)
    {
      return ValidationResult.Error("--group-by ruleId is only supported by readsarif.");
    }

    if (string.IsNullOrWhiteSpace(Metric))
    {
      return ValidationResult.Error("--metric is required.");
    }

    if (string.IsNullOrWhiteSpace(Namespace))
    {
      return ValidationResult.Error("--namespace is required.");
    }

    return ValidationResult.Success();
  }
}

