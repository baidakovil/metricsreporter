using System;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Resolves scripts for the test command using configuration precedence.
/// </summary>
internal sealed class TestScriptResolver
{
  /// <summary>
  /// Resolves scripts to run around the test pipeline.
  /// </summary>
  /// <param name="settings">Test CLI settings.</param>
  /// <param name="configuration">Resolved configuration.</param>
  /// <returns>Script resolution result.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Script resolution coordinates CLI, environment, and configuration sources to honor precedence.")]
  public ScriptResolutionResult Resolve(TestSettings settings, ConfigurationLoadResult configuration)
  {
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(configuration);

    var parsedMetricScripts = MetricScriptParser.Parse(settings.MetricScripts, TestCommand.MetricScriptSeparators);
    var scripts = ConfigurationResolver.ResolveScripts(
      Array.Empty<string>(),
      Array.Empty<string>(),
      Array.Empty<(string Metric, string Path)>(),
      settings.Scripts,
      parsedMetricScripts,
      configuration.EnvironmentConfiguration.Scripts,
      configuration.FileConfiguration.Scripts);

    return ScriptResolutionResult.Success(scripts);
  }
}

