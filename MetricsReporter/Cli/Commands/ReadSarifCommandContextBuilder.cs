using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Services.Scripts;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds the readsarif command context by resolving configuration, scripts, and SARIF settings.
/// </summary>
internal sealed class ReadSarifCommandContextBuilder
{
  private readonly MetricsReporterConfigLoader _configLoader;

  public ReadSarifCommandContextBuilder(MetricsReporterConfigLoader configLoader)
  {
    _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
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

    var configuration = LoadConfiguration(settings);
    if (!configuration.Succeeded)
    {
      return BuildSarifContextResult.CreateFailure(configuration.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var paths = ResolvePaths(settings, configuration);
    if (!paths.Succeeded)
    {
      return BuildSarifContextResult.CreateFailure(paths.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var scripts = ResolveScripts(settings, configuration);
    if (!scripts.Succeeded)
    {
      return BuildSarifContextResult.CreateFailure(scripts.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var sarifSettings = BuildSarifSettings(settings, paths);
    if (!sarifSettings.Succeeded)
    {
      return BuildSarifContextResult.CreateFailure(sarifSettings.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    return BuildSarifContextResult.CreateSuccess(
      new ReadSarifCommandContext(
        configuration.GeneralOptions,
        configuration.EnvironmentConfiguration,
        configuration.FileConfiguration,
        scripts.Scripts!,
        sarifSettings.Settings!,
        sarifSettings.Metrics));
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Configuration load stage merges CLI, environment, and file-based inputs for SARIF reads; coupling reflects mandatory dependencies.")]
  private ConfigurationLoadResult LoadConfiguration(ReadSarifSettings settings)
  {
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

    return ConfigurationLoadResult.Success(general, envConfig, configResult.Configuration);
  }

  private static PathResolutionResult ResolvePaths(ReadSarifSettings settings, ConfigurationLoadResult configuration)
  {
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

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Script resolution merges CLI, environment, and configuration sources for SARIF workflows; the dependencies are required for correctness.")]
  private static ScriptResolutionResult ResolveScripts(ReadSarifSettings settings, ConfigurationLoadResult configuration)
  {
    var parsedMetricScripts = MetricScriptParser.Parse(settings.MetricScripts, ReadSarifCommand.MetricScriptSeparators);
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

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Method consolidates SARIF-specific validation, metric resolution, and command settings construction; further splitting would duplicate validation messaging logic.")]
  private static SarifSettingsResult BuildSarifSettings(ReadSarifSettings settings, PathResolutionResult paths)
  {
    var sarifSettings = new SarifMetricSettings
    {
      ReportPath = paths.ReportPath!,
      Namespace = settings.Namespace!,
      Metric = settings.Metric,
      SymbolKind = settings.SymbolKind,
      RuleId = settings.RuleId,
      GroupBy = settings.GroupBy,
      ShowAll = settings.ShowAll,
      ThresholdsFile = paths.ThresholdsFile,
      IncludeSuppressed = settings.IncludeSuppressed
    };

    var validation = sarifSettings.Validate();
    if (!validation.Successful)
    {
      AnsiConsole.MarkupLine($"[red]{validation.Message}[/]");
      return SarifSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    if (!sarifSettings.TryResolveSarifMetrics(out var metrics) || metrics is null)
    {
      AnsiConsole.MarkupLine($"[red]Unknown SARIF metric '{sarifSettings.EffectiveMetricName}'.[/]");
      return SarifSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    return SarifSettingsResult.Success(sarifSettings, metrics.Select(metric => metric.ToString()));
  }
}

/// <summary>
/// Represents the resolved context for executing the readsarif command.
/// </summary>
/// <param name="GeneralOptions">Resolved general options shared across commands.</param>
/// <param name="EnvironmentConfiguration">Configuration from environment sources.</param>
/// <param name="FileConfiguration">Configuration from file sources.</param>
/// <param name="Scripts">Scripts to run before SARIF aggregation.</param>
/// <param name="SarifSettings">SARIF-specific settings for execution.</param>
/// <param name="Metrics">Resolved SARIF metrics.</param>
internal sealed record ReadSarifCommandContext(
  ResolvedGeneralOptions GeneralOptions,
  MetricsReporterConfiguration EnvironmentConfiguration,
  MetricsReporterConfiguration FileConfiguration,
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
internal sealed record ConfigurationLoadResult(
  bool Succeeded,
  int? ExitCode,
  ResolvedGeneralOptions GeneralOptions,
  MetricsReporterConfiguration EnvironmentConfiguration,
  MetricsReporterConfiguration FileConfiguration)
{
  public static ConfigurationLoadResult Success(
    ResolvedGeneralOptions generalOptions,
    MetricsReporterConfiguration environmentConfiguration,
    MetricsReporterConfiguration fileConfiguration) =>
    new(true, null, generalOptions, environmentConfiguration, fileConfiguration);

  public static ConfigurationLoadResult Failure(int exitCode) =>
    new(false, exitCode, null!, null!, null!);
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

