using System.Collections.Generic;
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.MetricsReader.Services;

namespace MetricsReporter.Cli.Settings;

/// <summary>
/// Settings for the readsarif command.
/// </summary>
internal sealed class ReadSarifSettings : CliSettingsBase
{
  [CommandOption("--report <PATH>")]
  [Description("Path to MetricsReport.g.json (can be provided via config).")]
  public string? Report { get; init; }

  [CommandOption("--metric <NAME>")]
  [Description("SARIF metric identifier (SarifCaRuleViolations, SarifIdeRuleViolations) or 'Any'. Defaults to Any.")]
  public string? Metric { get; init; }

  [CommandOption("--namespace <NAME>")]
  [Description("Namespace prefix to filter symbols (e.g. Sample.Loader.Infrastructure).")]
  public string? Namespace { get; init; }

  [CommandOption("--symbol-kind <Any|Type|Member>")]
  [Description("Symbol level to inspect. Defaults to Any.")]
  public MetricsReaderSymbolKind SymbolKind { get; init; } = MetricsReaderSymbolKind.Any;

  [CommandOption("--ruleid <ID>")]
  [Description("Optional SARIF rule identifier filter (e.g. CA1506, IDE0051).")]
  public string? RuleId { get; init; }

  [CommandOption("--group-by <metric|method|type|namespace|ruleId>")]
  [Description("Controls grouping of SARIF violations (metric, namespace, type, method, ruleId). Defaults to ruleId.")]
  public MetricsReaderGroupByOption? GroupBy { get; init; }

  [CommandOption("--all")]
  [Description("Emit all matching groups instead of only the most severe one.")]
  public bool ShowAll { get; init; }

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

  /// <inheritdoc/>
  public override ValidationResult Validate()
  {
    var baseResult = base.Validate();
    if (!baseResult.Successful)
    {
      return baseResult;
    }

    if (string.IsNullOrWhiteSpace(Namespace))
    {
      return ValidationResult.Error("--namespace is required.");
    }

    if (!string.IsNullOrWhiteSpace(Metric) && !MetricIdentifierResolver.TryResolve(Metric!, out _))
    {
      return ValidationResult.Error($"Unknown metric identifier '{Metric}'.");
    }

    return ValidationResult.Success();
  }
}

