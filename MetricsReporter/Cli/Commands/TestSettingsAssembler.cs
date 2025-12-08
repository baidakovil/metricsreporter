using System;
using System.Collections.Generic;
using MetricsReporter.Cli.Settings;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds test settings and resolves metrics for the test command.
/// </summary>
internal sealed class TestSettingsAssembler
{
  /// <summary>
  /// Builds validated test settings and resolves metric identifiers.
  /// </summary>
  /// <param name="settings">Test CLI settings.</param>
  /// <param name="paths">Resolved report and thresholds paths.</param>
  /// <param name="metricAliases">Metric alias mappings.</param>
  /// <returns>Test settings result.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Assembler validates test inputs, resolves metrics, and constructs settings to keep command flow linear.")]
  public TestSettingsResult Build(
    TestSettings settings,
    PathResolutionResult paths,
    IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> metricAliases)
  {
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(metricAliases);

    if (string.IsNullOrWhiteSpace(paths.ReportPath))
    {
      return TestSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    MetricIdentifierResolver resolver;
    try
    {
      resolver = new MetricIdentifierResolver(metricAliases);
    }
    catch (ArgumentException ex)
    {
      AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
      return TestSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }
    if (!resolver.TryResolve(settings.Metric!, out var resolvedMetric))
    {
      AnsiConsole.MarkupLine($"[red]{resolver.BuildUnknownMetricMessage(settings.Metric)}[/]");
      return TestSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    var testSettings = new TestMetricSettings
    {
      ReportPath = paths.ReportPath,
      Symbol = settings.Symbol!,
      Metric = settings.Metric!,
      MetricResolver = resolver,
      ThresholdsFile = paths.ThresholdsFile,
      IncludeSuppressed = settings.IncludeSuppressed
    };

    var validation = testSettings.Validate();
    if (!validation.Successful)
    {
      AnsiConsole.MarkupLine($"[red]{validation.Message}[/]");
      return TestSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    return TestSettingsResult.Success(testSettings, new[] { resolvedMetric.ToString() });
  }
}

