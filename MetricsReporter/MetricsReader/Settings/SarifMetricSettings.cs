namespace MetricsReporter.MetricsReader.Settings;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.Model;

/// <summary>
/// Settings specific to the readsarif command.
/// </summary>
internal sealed class SarifMetricSettings : MetricsReaderSettingsBase
{
  /// <summary>
  /// Gets or sets the metric identifier, alias, or <c>Any</c>.
  /// </summary>
  [CommandOption("--metric <NAME>")]
  [Description("SARIF metric identifier (SarifCaRuleViolations, SarifIdeRuleViolations) or 'Any'. Defaults to Any.")]
  public string? Metric { get; init; }

  /// <summary>
  /// Gets the namespace filter supplied by the user.
  /// </summary>
  [CommandOption("--namespace <NAME>")]
  [Description("Namespace prefix to filter symbols (e.g. Sample.Loader.Infrastructure).")]
  public string Namespace { get; init; } = string.Empty;

  /// <summary>
  /// Gets the targeted symbol level.
  /// </summary>
  [CommandOption("--symbol-kind <Any|Type|Member>")]
  [Description("Symbol level to inspect. Defaults to Any, which includes both types and members (types are listed first when using --all).")]
  public MetricsReaderSymbolKind SymbolKind { get; init; } = MetricsReaderSymbolKind.Any;

  /// <summary>
  /// Gets or sets an optional SARIF rule filter (e.g. CA1506) used by readsarif.
  /// </summary>
  [CommandOption("--ruleid <ID>")]
  [Description("Optional SARIF rule identifier filter (e.g. CA1506, IDE0051).")]
  public string? RuleId { get; init; }

  /// <summary>
  /// Gets or sets the grouping dimension (defaults to ruleId for readsarif).
  /// </summary>
  [CommandOption("--group-by <metric|method|type|namespace|ruleId>")]
  [Description("Controls grouping of SARIF violations (metric, namespace, type, method, ruleId). Defaults to ruleId.")]
  public MetricsReaderGroupByOption? GroupBy { get; init; }

  /// <summary>
  /// Gets a value indicating if the user explicitly provided a metric option.
  /// </summary>
  public bool HasExplicitMetric => !string.IsNullOrWhiteSpace(Metric);

  /// <summary>
  /// Gets the normalized metric text used for output and help.
  /// </summary>
  public string EffectiveMetricName
      => string.IsNullOrWhiteSpace(Metric)
        ? "Any"
        : Metric!.Trim();

  /// <summary>
  /// Gets the effective grouping mode for the command.
  /// </summary>
  public MetricsReaderGroupByOption EffectiveGroupBy
    => GroupBy ?? MetricsReaderGroupByOption.RuleId;

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

    return ValidationResult.Success();
  }

  [CommandOption("--all")]
  [Description("When specified, emits all matching groups instead of only the most severe one.")]
  public bool ShowAll { get; init; }

  public bool TryResolveSarifMetrics(out IReadOnlyList<MetricIdentifier>? metrics)
  {
    var metricName = EffectiveMetricName;

    if (string.Equals(metricName, "Any", StringComparison.OrdinalIgnoreCase))
    {
      metrics = new[]
      {
        MetricIdentifier.SarifCaRuleViolations,
        MetricIdentifier.SarifIdeRuleViolations
      };
      return true;
    }

    if (!MetricIdentifierResolver.TryResolve(metricName, out var resolved))
    {
      metrics = null;
      return false;
    }

    if (resolved != MetricIdentifier.SarifCaRuleViolations && resolved != MetricIdentifier.SarifIdeRuleViolations)
    {
      metrics = null;
      return false;
    }

    metrics = new[] { resolved };
    return true;
  }
}

