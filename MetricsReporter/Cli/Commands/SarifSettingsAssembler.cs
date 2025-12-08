using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.Cli.Settings;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds SARIF settings and resolves metrics for readsarif execution.
/// </summary>
internal sealed class SarifSettingsAssembler
{
  /// <summary>
  /// Builds SARIF settings and resolves metrics using aliases and validation.
  /// </summary>
  /// <param name="settings">Readsarif CLI settings.</param>
  /// <param name="paths">Resolved paths for report and thresholds.</param>
  /// <param name="metricAliases">Metric alias mappings.</param>
  /// <returns>SARIF settings result.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Assembler validates inputs, constructs SARIF settings, and resolves metrics to keep readsarif flow linear.")]
  public SarifSettingsResult Build(
    ReadSarifSettings settings,
    PathResolutionResult paths,
    IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> metricAliases)
  {
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(metricAliases);

    if (string.IsNullOrWhiteSpace(paths.ReportPath))
    {
      return SarifSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    MetricIdentifierResolver resolver;
    try
    {
      resolver = new MetricIdentifierResolver(metricAliases);
    }
    catch (ArgumentException ex)
    {
      AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
      return SarifSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    var sarifSettings = new SarifMetricSettings
    {
      ReportPath = paths.ReportPath,
      Namespace = settings.Namespace!,
      Metric = settings.Metric,
      SymbolKind = settings.SymbolKind,
      RuleId = settings.RuleId,
      GroupBy = settings.GroupBy,
      ShowAll = settings.ShowAll,
      ThresholdsFile = paths.ThresholdsFile,
      IncludeSuppressed = settings.IncludeSuppressed,
      MetricResolver = resolver
    };

    var validation = sarifSettings.Validate();
    if (!validation.Successful)
    {
      AnsiConsole.MarkupLine($"[red]{validation.Message}[/]");
      return SarifSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    if (!sarifSettings.TryResolveSarifMetrics(out var metrics) || metrics is null)
    {
      AnsiConsole.MarkupLine($"[red]{sarifSettings.MetricResolver.BuildUnknownMetricMessage(sarifSettings.EffectiveMetricName)}[/]");
      return SarifSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    return SarifSettingsResult.Success(sarifSettings, metrics.Select(metric => metric.ToString()));
  }
}

