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

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds the test command context by resolving configuration, scripts, and test settings.
/// </summary>
internal sealed class TestCommandContextBuilder
{
  private readonly MetricsReporterConfigLoader _configLoader;

  public TestCommandContextBuilder(MetricsReporterConfigLoader configLoader)
  {
    _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
  }

  /// <summary>
  /// Creates a fully validated test command context or returns an error outcome.
  /// </summary>
  /// <param name="settings">Test CLI settings.</param>
  /// <returns>Outcome containing context or exit code.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Build centralizes configuration loading, path resolution, metric validation, and script resolution for test command execution; splitting further would reduce readability of the linear setup flow.")]
  public BuildTestContextResult Build(TestSettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);

    var configuration = LoadConfiguration(settings);
    if (!configuration.Succeeded)
    {
      return BuildTestContextResult.CreateFailure(configuration.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var paths = ResolvePaths(settings, configuration);
    if (!paths.Succeeded)
    {
      return BuildTestContextResult.CreateFailure(paths.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var scripts = ResolveScripts(settings, configuration);
    if (!scripts.Succeeded)
    {
      return BuildTestContextResult.CreateFailure(scripts.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var testSettings = BuildTestSettings(settings, paths);
    if (!testSettings.Succeeded)
    {
      return BuildTestContextResult.CreateFailure(testSettings.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    return BuildTestContextResult.CreateSuccess(
      new TestCommandContext(
        configuration.GeneralOptions,
        configuration.EnvironmentConfiguration,
        configuration.FileConfiguration,
        scripts.Scripts!,
        testSettings.Settings!,
        testSettings.Metrics));
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Configuration load stage composes CLI, environment, and file configuration for test command; coupling is inherent to this aggregation.")]
  private ConfigurationLoadResult LoadConfiguration(TestSettings settings)
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

  private static PathResolutionResult ResolvePaths(TestSettings settings, ConfigurationLoadResult configuration)
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
    Justification = "Script resolution merges CLI, environment, and configuration sources for test workflows; dependencies are required for correct precedence handling.")]
  private static ScriptResolutionResult ResolveScripts(TestSettings settings, ConfigurationLoadResult configuration)
  {
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

  private static TestSettingsResult BuildTestSettings(TestSettings settings, PathResolutionResult paths)
  {
    if (!MetricIdentifierResolver.TryResolve(settings.Metric!, out var resolvedMetric))
    {
      AnsiConsole.MarkupLine($"[red]Unknown metric identifier '{settings.Metric}'.[/]");
      return TestSettingsResult.Failure((int)MetricsReporterExitCode.ValidationError);
    }

    var testSettings = new TestMetricSettings
    {
      ReportPath = paths.ReportPath!,
      Symbol = settings.Symbol!,
      Metric = settings.Metric!,
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

/// <summary>
/// Represents the resolved context for executing the test command.
/// </summary>
/// <param name="GeneralOptions">Resolved general options shared across commands.</param>
/// <param name="EnvironmentConfiguration">Configuration from environment sources.</param>
/// <param name="FileConfiguration">Configuration from file sources.</param>
/// <param name="Scripts">Scripts to run around the test command.</param>
/// <param name="TestSettings">Settings for the metric test.</param>
/// <param name="Metrics">Metrics requested by the user.</param>
internal sealed record TestCommandContext(
  ResolvedGeneralOptions GeneralOptions,
  MetricsReporterConfiguration EnvironmentConfiguration,
  MetricsReporterConfiguration FileConfiguration,
  ResolvedScripts Scripts,
  TestMetricSettings TestSettings,
  IEnumerable<string> Metrics);

/// <summary>
/// Outcome of building the test command context.
/// </summary>
/// <param name="Succeeded">Indicates whether context creation succeeded.</param>
/// <param name="ExitCode">Exit code to return when creation fails.</param>
/// <param name="Context">Resolved context when successful.</param>
internal sealed record BuildTestContextResult(bool Succeeded, int? ExitCode, TestCommandContext? Context)
{
  public static BuildTestContextResult CreateSuccess(TestCommandContext context) =>
    new(true, null, context);

  public static BuildTestContextResult CreateFailure(int exitCode) =>
    new(false, exitCode, null);
}

/// <summary>
/// Outcome of assembling test settings for the test command.
/// </summary>
/// <param name="Succeeded">Indicates whether settings assembly succeeded.</param>
/// <param name="ExitCode">Exit code to return when creation fails.</param>
/// <param name="Settings">Resolved test metric settings.</param>
/// <param name="Metrics">Metrics requested by the user.</param>
internal sealed record TestSettingsResult(bool Succeeded, int? ExitCode, TestMetricSettings? Settings, IEnumerable<string> Metrics)
{
  public static TestSettingsResult Success(TestMetricSettings settings, IEnumerable<string> metrics) =>
    new(true, null, settings, metrics);

  public static TestSettingsResult Failure(int exitCode) =>
    new(false, exitCode, null, Array.Empty<string>());
}

