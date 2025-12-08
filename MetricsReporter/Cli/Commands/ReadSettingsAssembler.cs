using System;
using System.Collections.Generic;
using MetricsReporter.Cli.Settings;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds reader settings and resolves metrics for the read command.
/// </summary>
internal sealed class ReadSettingsAssembler
{
  /// <summary>
  /// Builds validated reader settings and resolves metric identifiers.
  /// </summary>
  /// <param name="settings">Read CLI settings.</param>
  /// <param name="paths">Resolved report and thresholds paths.</param>
  /// <param name="metricAliases">Metric alias mappings.</param>
  /// <returns>Reader settings result.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Assembler validates metric identifiers, thresholds, grouping, and suppression flags together to keep the read flow linear.")]
  public ReadSettingsResult Build(
    ReadSettings settings,
    PathResolutionResult paths,
    IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> metricAliases)
  {
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(metricAliases);

    if (string.IsNullOrWhiteSpace(paths.ReportPath))
    {
      return ReadSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    MetricIdentifierResolver resolver;
    try
    {
      resolver = new MetricIdentifierResolver(metricAliases);
    }
    catch (ArgumentException ex)
    {
      AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
      return ReadSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }
    if (!resolver.TryResolve(settings.Metric!, out var resolvedMetric))
    {
      AnsiConsole.MarkupLine($"[red]{resolver.BuildUnknownMetricMessage(settings.Metric)}[/]");
      return ReadSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    var readerSettings = new NamespaceMetricSettings
    {
      ReportPath = paths.ReportPath,
      Namespace = settings.Namespace!,
      Metric = settings.Metric!,
      MetricResolver = resolver,
      SymbolKind = settings.SymbolKind,
      ShowAll = settings.ShowAll,
      RuleId = settings.RuleId,
      GroupBy = settings.GroupBy,
      ThresholdsFile = paths.ThresholdsFile,
      IncludeSuppressed = settings.IncludeSuppressed
    };

    var validation = readerSettings.Validate();
    if (!validation.Successful)
    {
      AnsiConsole.MarkupLine($"[red]{validation.Message}[/]");
      return ReadSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    return ReadSettingsResult.Success(readerSettings, new[] { resolvedMetric.ToString() });
  }
}

