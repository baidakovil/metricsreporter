using System;
using System.Collections.Generic;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.Model;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Loads configuration for the readsarif command by merging CLI, environment, and file sources.
/// </summary>
internal sealed class ReadSarifConfigurationProvider
{
  private readonly MetricsReporterConfigLoader _configLoader;

  /// <summary>
  /// Initializes a new instance of the <see cref="ReadSarifConfigurationProvider"/> class.
  /// </summary>
  /// <param name="configLoader">MetricsReporter configuration loader.</param>
  public ReadSarifConfigurationProvider(MetricsReporterConfigLoader configLoader)
  {
    _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
  }

  /// <summary>
  /// Loads configuration and resolves metric aliases for the readsarif command.
  /// </summary>
  /// <param name="settings">Readsarif CLI settings.</param>
  /// <returns>Configuration load result.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Configuration composition requires coordinating CLI, environment, file, and alias parsing responsibilities in one place.")]
  public ConfigurationLoadResult Load(ReadSarifSettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);

    var envConfig = EnvironmentConfigurationProvider.Read();
    var workingDirectoryHint = settings.WorkingDirectory
      ?? envConfig.General.WorkingDirectory
      ?? Environment.CurrentDirectory;

    var configResult = _configLoader.Load(settings.ConfigPath, workingDirectoryHint);
    if (!configResult.IsSuccess)
    {
      foreach (var error in configResult.Errors)
      {
        AnsiConsole.MarkupLine($"[red]{error}[/]");
      }

      return ConfigurationLoadResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    var general = ConfigurationResolver.ResolveGeneral(
      settings.Verbosity,
      settings.TimeoutSeconds,
      settings.WorkingDirectory,
      settings.LogTruncationLimit,
      settings.RunScripts,
      settings.AggregateAfterScripts,
      envConfig,
      configResult.Configuration);

    Dictionary<string, string[]>? cliAliases = null;
    try
    {
      cliAliases = MetricAliasParser.Parse(settings.MetricAliases);
    }
    catch (ArgumentException ex)
    {
      AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
      return ConfigurationLoadResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    var metricAliases = ConfigurationResolver.ResolveMetricAliases(
      cliAliases,
      envConfig,
      configResult.Configuration);

    return ConfigurationLoadResult.Success(general, envConfig, configResult.Configuration, metricAliases);
  }
}

