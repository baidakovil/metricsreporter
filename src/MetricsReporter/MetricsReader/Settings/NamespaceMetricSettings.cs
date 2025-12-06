namespace MetricsReporter.MetricsReader.Settings;

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.Model;

/// <summary>
/// Settings shared by commands that operate on namespace + metric scope.
/// </summary>
internal sealed class NamespaceMetricSettings : MetricsReaderSettingsBase
{
  /// <summary>
  /// Gets or sets the namespace filter supplied by the user.
  /// </summary>
  [CommandOption("--namespace <NAME>")]
  [Description("Namespace prefix to filter symbols (e.g. Sample.Loader.Infrastructure).")]
  public string Namespace { get; init; } = string.Empty;

  /// <summary>
  /// Gets or sets the metric identifier or alias provided by the user.
  /// </summary>
  [CommandOption("--metric <NAME>")]
  [Description("Metric identifier or alias (Complexity, Coupling, Maintainability, etc.).")]
  public string Metric { get; init; } = string.Empty;

  [CommandOption("--symbol-kind <Any|Type|Member>")]
  [Description("Symbol level to inspect. Defaults to Any, which includes both types and members (types are listed first when using --all).")]
  public MetricsReaderSymbolKind SymbolKind { get; init; } = MetricsReaderSymbolKind.Any;

  /// <summary>
  /// Gets a value indicating whether all matching entries should be emitted instead of the single most severe one.
  /// </summary>
  [CommandOption("--all")]
  [Description("When specified, emits all matching entries instead of only the most severe one.")]
  public bool ShowAll { get; init; }

  /// <summary>
  /// Gets or sets an optional SARIF rule filter (e.g. CA1506) used by readsarif.
  /// </summary>
  [CommandOption("--ruleid <ID>")]
  [Description("Optional SARIF rule identifier filter (e.g. CA1506, IDE0051).")]
  public string? RuleId { get; init; }

  /// <summary>
  /// Gets or sets the optional grouping dimension.
  /// </summary>
  [CommandOption("--group-by <metric|method|type|namespace|ruleId>")]
  [Description("Controls grouping of violations (metric, namespace, type, method). ruleId is reserved for readsarif.")]
  public MetricsReaderGroupByOption? GroupBy { get; init; }

  /// <summary>
  /// Gets the effective grouping mode (defaults to None for readany).
  /// </summary>
  public MetricsReaderGroupByOption EffectiveGroupBy
    => GroupBy ?? MetricsReaderGroupByOption.None;

  /// <summary>
  /// Gets the resolved metric identifier after validation succeeds.
  /// </summary>
  public MetricIdentifier ResolvedMetric { get; private set; }

  /// <inheritdoc />
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

    if (string.IsNullOrWhiteSpace(Metric))
    {
      return ValidationResult.Error("--metric is required.");
    }

    if (!MetricIdentifierResolver.TryResolve(Metric, out var resolved))
    {
      return ValidationResult.Error($"Unknown metric identifier '{Metric}'.");
    }

    if (GroupBy == MetricsReaderGroupByOption.RuleId)
    {
      return ValidationResult.Error("--group-by ruleId is only supported by the readsarif command.");
    }

    ResolvedMetric = resolved;
    return ValidationResult.Success();
  }
}

