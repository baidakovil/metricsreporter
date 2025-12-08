using System;
using System.Collections.Generic;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds the execution context for the read command by resolving configuration, paths, scripts, and reader settings.
/// </summary>
internal sealed class ReadCommandContextBuilder
{
  private readonly ReadConfigurationProvider _configurationProvider;
  private readonly ReadSettingsAssembler _settingsAssembler;

  public ReadCommandContextBuilder(MetricsReporterConfigLoader configLoader)
  {
    ArgumentNullException.ThrowIfNull(configLoader);

    _configurationProvider = new ReadConfigurationProvider(configLoader);
    _settingsAssembler = new ReadSettingsAssembler();
  }

  /// <summary>
  /// Creates a fully validated read command context or returns an error outcome.
  /// </summary>
  /// <param name="settings">Read CLI settings.</param>
  /// <returns>Outcome containing context or exit code.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Build aggregates configuration loading, path resolution, script resolution, and settings validation for the read command; splitting further would obscure the linear CLI flow.")]
  public BuildReadContextResult Build(ReadSettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);

    var configuration = _configurationProvider.Load(settings);
    if (!configuration.Succeeded)
    {
      return BuildReadContextResult.CreateFailure(configuration.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var paths = ReadPathResolver.Resolve(settings, configuration);
    if (!paths.Succeeded || string.IsNullOrWhiteSpace(paths.ReportPath))
    {
      return BuildReadContextResult.CreateFailure(paths.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var scripts = ReadScriptResolver.Resolve(settings, configuration);
    if (!scripts.Succeeded)
    {
      return BuildReadContextResult.CreateFailure(scripts.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var resolvedScripts = scripts.Scripts;
    if (resolvedScripts is null)
    {
      return BuildReadContextResult.CreateFailure(scripts.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var readerSettings = _settingsAssembler.Build(settings, paths, configuration.MetricAliases);
    if (!readerSettings.Succeeded || readerSettings.ReaderSettings is null)
    {
      return BuildReadContextResult.CreateFailure(readerSettings.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var reportPath = paths.ReportPath ?? string.Empty;
    var metrics = readerSettings.Metrics ?? Array.Empty<string>();

    return BuildReadContextResult.CreateSuccess(
      new ReadCommandContext(
        configuration.GeneralOptions,
        configuration.EnvironmentConfiguration,
        configuration.FileConfiguration,
        configuration.MetricAliases,
        resolvedScripts,
        readerSettings.ReaderSettings,
        metrics,
        reportPath));
  }

}

/// <summary>
/// Represents the resolved context for executing the read command.
/// </summary>
/// <param name="GeneralOptions">Resolved general options shared across commands.</param>
/// <param name="EnvironmentConfiguration">Configuration from environment sources.</param>
/// <param name="FileConfiguration">Configuration from file sources.</param>
/// <param name="MetricAliases">Resolved metric alias mappings.</param>
/// <param name="Scripts">Scripts to run before reading metrics.</param>
/// <param name="ReaderSettings">Settings for the metrics reader.</param>
/// <param name="Metrics">Metrics requested by the user.</param>
/// <param name="ReportPath">Path to the metrics report.</param>
internal sealed record ReadCommandContext(
  ResolvedGeneralOptions GeneralOptions,
  MetricsReporterConfiguration EnvironmentConfiguration,
  MetricsReporterConfiguration FileConfiguration,
  IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> MetricAliases,
  ResolvedScripts Scripts,
  NamespaceMetricSettings ReaderSettings,
  IEnumerable<string> Metrics,
  string ReportPath);

/// <summary>
/// Outcome of building the read command context.
/// </summary>
/// <param name="Succeeded">Indicates whether context creation succeeded.</param>
/// <param name="ExitCode">Exit code to return when creation fails.</param>
/// <param name="Context">Resolved context when successful.</param>
internal sealed record BuildReadContextResult(bool Succeeded, int? ExitCode, ReadCommandContext? Context)
{
  public static BuildReadContextResult CreateSuccess(ReadCommandContext context) =>
    new(true, null, context);

  public static BuildReadContextResult CreateFailure(int exitCode) =>
    new(false, exitCode, null);
}

/// <summary>
/// Outcome of assembling reader settings for the read command.
/// </summary>
/// <param name="Succeeded">Indicates whether settings assembly succeeded.</param>
/// <param name="ExitCode">Exit code to return when creation fails.</param>
/// <param name="ReaderSettings">Resolved reader settings.</param>
/// <param name="Metrics">Metrics requested by the user.</param>
internal sealed record ReadSettingsResult(bool Succeeded, int? ExitCode, NamespaceMetricSettings? ReaderSettings, IEnumerable<string> Metrics)
{
  public static ReadSettingsResult Success(NamespaceMetricSettings readerSettings, IEnumerable<string> metrics) =>
    new(true, null, readerSettings, metrics);

  public static ReadSettingsResult Failure(int exitCode) =>
    new(false, exitCode, null, Array.Empty<string>());
}

