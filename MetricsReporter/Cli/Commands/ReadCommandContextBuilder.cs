using System;
using System.Collections.Generic;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Services.Scripts;
using Spectre.Console;
using MetricIdentifierResolver = MetricsReporter.MetricsReader.Services.MetricIdentifierResolver;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds the execution context for the read command by resolving configuration, paths, scripts, and reader settings.
/// </summary>
internal sealed class ReadCommandContextBuilder
{
  private readonly MetricsReporterConfigLoader _configLoader;

  public ReadCommandContextBuilder(MetricsReporterConfigLoader configLoader)
  {
    _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
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

    var configuration = LoadConfiguration(settings);
    if (!configuration.Succeeded)
    {
      return BuildReadContextResult.CreateFailure(configuration.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var paths = ResolvePaths(settings, configuration);
    if (!paths.Succeeded)
    {
      return BuildReadContextResult.CreateFailure(paths.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var scripts = ResolveScripts(settings, configuration);
    if (!scripts.Succeeded)
    {
      return BuildReadContextResult.CreateFailure(scripts.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var readerSettings = BuildReaderSettings(settings, paths);
    if (!readerSettings.Succeeded)
    {
      return BuildReadContextResult.CreateFailure(readerSettings.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    return BuildReadContextResult.CreateSuccess(
      new ReadCommandContext(
        configuration.GeneralOptions,
        configuration.EnvironmentConfiguration,
        configuration.FileConfiguration,
        scripts.Scripts!,
        readerSettings.ReaderSettings!,
        readerSettings.Metrics,
        paths.ReportPath!));
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Configuration load step composes CLI, environment, and file configuration sources for read command; coupling reflects necessary dependencies.")]
  private ConfigurationLoadResult LoadConfiguration(ReadSettings settings)
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

  private static PathResolutionResult ResolvePaths(ReadSettings settings, ConfigurationLoadResult configuration)
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
    Justification = "Script resolution must merge CLI, environment, and configuration-defined scripts; coupling reflects these required dependencies.")]
  private static ScriptResolutionResult ResolveScripts(ReadSettings settings, ConfigurationLoadResult configuration)
  {
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

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Reader settings assembly must validate metric identifier, thresholds, grouping, and suppression flags together; coupling reflects required dependencies for correctness.")]
  private static ReadSettingsResult BuildReaderSettings(ReadSettings settings, PathResolutionResult paths)
  {
    if (!MetricIdentifierResolver.TryResolve(settings.Metric!, out var resolvedMetric))
    {
      AnsiConsole.MarkupLine($"[red]Unknown metric identifier '{settings.Metric}'.[/]");
      return ReadSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    var readerSettings = new NamespaceMetricSettings
    {
      ReportPath = paths.ReportPath!,
      Namespace = settings.Namespace!,
      Metric = settings.Metric!,
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

/// <summary>
/// Represents the resolved context for executing the read command.
/// </summary>
/// <param name="GeneralOptions">Resolved general options shared across commands.</param>
/// <param name="EnvironmentConfiguration">Configuration from environment sources.</param>
/// <param name="FileConfiguration">Configuration from file sources.</param>
/// <param name="Scripts">Scripts to run before reading metrics.</param>
/// <param name="ReaderSettings">Settings for the metrics reader.</param>
/// <param name="Metrics">Metrics requested by the user.</param>
/// <param name="ReportPath">Path to the metrics report.</param>
internal sealed record ReadCommandContext(
  ResolvedGeneralOptions GeneralOptions,
  MetricsReporterConfiguration EnvironmentConfiguration,
  MetricsReporterConfiguration FileConfiguration,
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

