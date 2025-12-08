using System;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Model;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Resolves report and thresholds paths for the test command.
/// </summary>
internal static class TestPathResolver
{
  /// <summary>
  /// Resolves paths using CLI, environment, and configuration values.
  /// </summary>
  /// <param name="settings">Test CLI settings.</param>
  /// <param name="configuration">Previously resolved configuration.</param>
  /// <returns>Path resolution result.</returns>
  public static PathResolutionResult Resolve(TestSettings settings, ConfigurationLoadResult configuration)
  {
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(configuration);

    var reportPath = CommandPathResolver.FirstNonEmpty(
      settings.Report,
      configuration.EnvironmentConfiguration.Paths.ReadReport,
      configuration.FileConfiguration.Paths.ReadReport,
      configuration.EnvironmentConfiguration.Paths.Report,
      configuration.FileConfiguration.Paths.Report);
    reportPath = CommandPathResolver.MakeAbsolute(reportPath, configuration.GeneralOptions.WorkingDirectory);
    if (string.IsNullOrWhiteSpace(reportPath))
    {
      AnsiConsole.MarkupLine("[red]--report is required (via CLI, env, or config).[/]");
      return PathResolutionResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    var thresholdsFile = CommandPathResolver.FirstNonEmpty(
      settings.ThresholdsFile,
      configuration.EnvironmentConfiguration.Paths.Thresholds,
      configuration.FileConfiguration.Paths.Thresholds);
    thresholdsFile = CommandPathResolver.MakeAbsolute(thresholdsFile, configuration.GeneralOptions.WorkingDirectory);

    return PathResolutionResult.Success(reportPath, thresholdsFile);
  }
}

