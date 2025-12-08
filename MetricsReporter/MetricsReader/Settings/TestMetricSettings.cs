namespace MetricsReporter.MetricsReader.Settings;

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.Model;

/// <summary>
/// Settings for the metric verification command.
/// </summary>
internal sealed class TestMetricSettings : MetricsReaderSettingsBase
{
  /// <summary>
  /// Gets or sets the fully qualified symbol name.
  /// </summary>
  [CommandOption("--symbol <FQN>")]
  [Description("Fully qualified symbol name (type or member).")]
  public string Symbol { get; init; } = string.Empty;

  /// <summary>
  /// Gets or sets the metric alias or identifier.
  /// </summary>
  [CommandOption("--metric <NAME>")]
  [Description("Metric identifier or alias to verify (e.g. Complexity, Coupling).")]
  public string Metric { get; init; } = string.Empty;

  /// <summary>
  /// Gets the resolved metric identifier after validation succeeds.
  /// </summary>
  public MetricIdentifier ResolvedMetric { get; private set; }

  /// <summary>
  /// Gets or sets the resolver used for metric/alias resolution.
  /// </summary>
  public MetricIdentifierResolver MetricResolver { get; init; } = MetricIdentifierResolver.Empty;

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

    if (!MetricResolver.TryResolve(Metric, out var resolved))
    {
      return ValidationResult.Error(MetricResolver.BuildUnknownMetricMessage(Metric));
    }

    ResolvedMetric = resolved;
    return ValidationResult.Success();
  }
}


