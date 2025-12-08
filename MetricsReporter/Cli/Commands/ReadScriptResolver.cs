using System;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Resolves scripts for the read command using configuration precedence.
/// </summary>
internal sealed class ReadScriptResolver
{
  /// <summary>
  /// Resolves scripts to run for the read pipeline.
  /// </summary>
  /// <param name="settings">Read CLI settings.</param>
  /// <param name="configuration">Resolved configuration.</param>
  /// <returns>Script resolution result.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Script resolution coordinates CLI, environment, and configuration sources to honor precedence.")]
  public ScriptResolutionResult Resolve(ReadSettings settings, ConfigurationLoadResult configuration)
  {
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(configuration);

    var parsedMetricScripts = MetricScriptParser.Parse(settings.MetricScripts, ReadCommand.MetricScriptSeparators);
    var scripts = ConfigurationResolver.ResolveScripts(
      Array.Empty<string>(),
      settings.Scripts,
      parsedMetricScripts,
      Array.Empty<string>(),
      Array.Empty<(string Metric, string Path)>(),
      configuration.EnvironmentConfiguration.Scripts,
      configuration.FileConfiguration.Scripts);

    return ScriptResolutionResult.Success(scripts);
  }
}

