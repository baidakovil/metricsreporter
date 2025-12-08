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
/// Builds the readsarif command context by resolving configuration, scripts, and SARIF settings.
/// </summary>
internal sealed class ReadSarifCommandContextBuilder
{
  private readonly ReadSarifConfigurationProvider _configurationProvider;
  private readonly ReadSarifPathResolver _pathResolver;
  private readonly ReadSarifScriptResolver _scriptResolver;
  private readonly SarifSettingsAssembler _settingsAssembler;

  public ReadSarifCommandContextBuilder(MetricsReporterConfigLoader configLoader)
  {
    ArgumentNullException.ThrowIfNull(configLoader);

    _configurationProvider = new ReadSarifConfigurationProvider(configLoader);
    _pathResolver = new ReadSarifPathResolver();
    _scriptResolver = new ReadSarifScriptResolver();
    _settingsAssembler = new SarifSettingsAssembler();
  }

  /// <summary>
  /// Creates a fully validated readsarif command context or returns an error outcome.
  /// </summary>
  /// <param name="settings">Readsarif CLI settings.</param>
  /// <returns>Outcome containing context or exit code.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Build aggregates configuration loading, SARIF metric resolution, script resolution, and validation into a single orchestrated entry point; further fragmentation would obscure the command flow.")]
  public BuildSarifContextResult Build(ReadSarifSettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);

    var configuration = _configurationProvider.Load(settings);
    if (!configuration.Succeeded)
    {
      return BuildSarifContextResult.CreateFailure(configuration.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var paths = _pathResolver.Resolve(settings, configuration);
    if (!paths.Succeeded || string.IsNullOrWhiteSpace(paths.ReportPath))
    {
      return BuildSarifContextResult.CreateFailure(paths.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var scripts = _scriptResolver.Resolve(settings, configuration);
    if (!scripts.Succeeded)
    {
      return BuildSarifContextResult.CreateFailure(scripts.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var resolvedScripts = scripts.Scripts;
    if (resolvedScripts is null)
    {
      return BuildSarifContextResult.CreateFailure(scripts.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var sarifSettings = _settingsAssembler.Build(settings, paths, configuration.MetricAliases);
    if (!sarifSettings.Succeeded || sarifSettings.Settings is null)
    {
      return BuildSarifContextResult.CreateFailure(sarifSettings.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var metrics = sarifSettings.Metrics ?? Array.Empty<string>();

    return BuildSarifContextResult.CreateSuccess(
      new ReadSarifCommandContext(
        configuration.GeneralOptions,
        configuration.EnvironmentConfiguration,
        configuration.FileConfiguration,
        configuration.MetricAliases,
        resolvedScripts,
        sarifSettings.Settings,
        metrics));
  }

}

/// <summary>
/// Represents the resolved context for executing the readsarif command.
/// </summary>
/// <param name="GeneralOptions">Resolved general options shared across commands.</param>
/// <param name="EnvironmentConfiguration">Configuration from environment sources.</param>
/// <param name="FileConfiguration">Configuration from file sources.</param>
/// <param name="MetricAliases">Resolved metric alias mappings.</param>
/// <param name="Scripts">Scripts to run before SARIF aggregation.</param>
/// <param name="SarifSettings">SARIF-specific settings for execution.</param>
/// <param name="Metrics">Resolved SARIF metrics.</param>
internal sealed record ReadSarifCommandContext(
  ResolvedGeneralOptions GeneralOptions,
  MetricsReporterConfiguration EnvironmentConfiguration,
  MetricsReporterConfiguration FileConfiguration,
  IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> MetricAliases,
  ResolvedScripts Scripts,
  SarifMetricSettings SarifSettings,
  IEnumerable<string> Metrics);

/// <summary>
/// Outcome of building the readsarif command context.
/// </summary>
/// <param name="Succeeded">Indicates whether context creation succeeded.</param>
/// <param name="ExitCode">Exit code to return when creation fails.</param>
/// <param name="Context">Resolved context when successful.</param>
internal sealed record BuildSarifContextResult(bool Succeeded, int? ExitCode, ReadSarifCommandContext? Context)
{
  public static BuildSarifContextResult CreateSuccess(ReadSarifCommandContext context) =>
    new(true, null, context);

  public static BuildSarifContextResult CreateFailure(int exitCode) =>
    new(false, exitCode, null);
}

/// <summary>
/// Represents the result of loading configuration for read/readsarif commands.
/// </summary>
/// <param name="Succeeded">Indicates load success.</param>
/// <param name="ExitCode">Exit code when loading fails.</param>
/// <param name="GeneralOptions">Resolved general options.</param>
/// <param name="EnvironmentConfiguration">Environment configuration.</param>
/// <param name="FileConfiguration">File configuration.</param>
/// <param name="MetricAliases">Resolved metric alias mappings.</param>
internal sealed record ConfigurationLoadResult(
  bool Succeeded,
  int? ExitCode,
  ResolvedGeneralOptions GeneralOptions,
  MetricsReporterConfiguration EnvironmentConfiguration,
  MetricsReporterConfiguration FileConfiguration,
  IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> MetricAliases)
{
  public static ConfigurationLoadResult Success(
    ResolvedGeneralOptions generalOptions,
    MetricsReporterConfiguration environmentConfiguration,
    MetricsReporterConfiguration fileConfiguration,
    IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> metricAliases) =>
    new(true, null, generalOptions, environmentConfiguration, fileConfiguration, metricAliases);

  public static ConfigurationLoadResult Failure(int exitCode) =>
    new(false, exitCode, null!, null!, null!, null!);
}

/// <summary>
/// Outcome of resolving report and thresholds paths.
/// </summary>
/// <param name="Succeeded">Indicates path resolution success.</param>
/// <param name="ExitCode">Exit code when resolution fails.</param>
/// <param name="ReportPath">Resolved report path.</param>
/// <param name="ThresholdsFile">Resolved thresholds file path.</param>
internal sealed record PathResolutionResult(bool Succeeded, int? ExitCode, string? ReportPath, string? ThresholdsFile)
{
  public static PathResolutionResult Success(string reportPath, string? thresholdsFile) =>
    new(true, null, reportPath, thresholdsFile);

  public static PathResolutionResult Failure(int exitCode) =>
    new(false, exitCode, null, null);
}

/// <summary>
/// Outcome of resolving scripts for a command.
/// </summary>
/// <param name="Succeeded">Indicates script resolution success.</param>
/// <param name="ExitCode">Exit code when resolution fails.</param>
/// <param name="Scripts">Resolved scripts set.</param>
internal sealed record ScriptResolutionResult(bool Succeeded, int? ExitCode, ResolvedScripts? Scripts)
{
  public static ScriptResolutionResult Success(ResolvedScripts scripts) =>
    new(true, null, scripts);

  public static ScriptResolutionResult Failure(int exitCode) =>
    new(false, exitCode, null);
}

/// <summary>
/// Outcome of assembling SARIF settings for the readsarif command.
/// </summary>
/// <param name="Succeeded">Indicates whether settings assembly succeeded.</param>
/// <param name="ExitCode">Exit code to return when creation fails.</param>
/// <param name="Settings">Resolved SARIF settings.</param>
/// <param name="Metrics">Resolved metrics for SARIF processing.</param>
internal sealed record SarifSettingsResult(bool Succeeded, int? ExitCode, SarifMetricSettings? Settings, IEnumerable<string> Metrics)
{
  public static SarifSettingsResult Success(SarifMetricSettings settings, IEnumerable<string> metrics) =>
    new(true, null, settings, metrics);

  public static SarifSettingsResult Failure(int exitCode) =>
    new(false, exitCode, null, Array.Empty<string>());
}

