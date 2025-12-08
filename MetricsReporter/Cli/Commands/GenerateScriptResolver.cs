using System;
using System.Collections.Generic;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Resolves scripts for the generate command using configuration precedence.
/// </summary>
internal sealed class GenerateScriptResolver
{
  private static readonly IReadOnlyList<string> NoScripts = Array.Empty<string>();
  private static readonly IReadOnlyList<(string Metric, string Path)> NoMetricScripts = Array.Empty<(string Metric, string Path)>();

  /// <summary>
  /// Resolves scripts to run around the generate pipeline.
  /// </summary>
  /// <param name="settings">Generate CLI settings.</param>
  /// <param name="configuration">Resolved configuration.</param>
  /// <returns>Script resolution result.</returns>
  public ScriptResolutionResult Resolve(GenerateSettings settings, ConfigurationLoadResult configuration)
  {
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(configuration);

    var sources = CreateSources(settings, configuration);
    var scripts = ResolveScripts(sources);

    return ScriptResolutionResult.Success(scripts);
  }

  private static ScriptResolutionSources CreateSources(GenerateSettings settings, ConfigurationLoadResult configuration)
  {
    return new ScriptResolutionSources(
      settings.Scripts,
      NoScripts,
      NoMetricScripts,
      NoScripts,
      NoMetricScripts,
      configuration.EnvironmentConfiguration.Scripts,
      configuration.FileConfiguration.Scripts);
  }

  private static ResolvedScripts ResolveScripts(ScriptResolutionSources sources)
  {
    return ConfigurationResolver.ResolveScripts(
      sources.GenerateScripts,
      sources.ReadScripts,
      sources.ReadMetricScripts,
      sources.TestScripts,
      sources.TestMetricScripts,
      sources.EnvironmentScripts,
      sources.FileScripts);
  }

  private sealed record ScriptResolutionSources(
    IReadOnlyList<string> GenerateScripts,
    IReadOnlyList<string> ReadScripts,
    IReadOnlyList<(string Metric, string Path)> ReadMetricScripts,
    IReadOnlyList<string> TestScripts,
    IReadOnlyList<(string Metric, string Path)> TestMetricScripts,
    ScriptsConfiguration EnvironmentScripts,
    ScriptsConfiguration FileScripts);
}

