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
/// Builds the test command context by resolving configuration, scripts, and test settings.
/// </summary>
internal sealed class TestCommandContextBuilder
{
  private readonly TestConfigurationProvider _configurationProvider;
  private readonly TestSettingsAssembler _settingsAssembler;

  public TestCommandContextBuilder(MetricsReporterConfigLoader configLoader)
  {
    ArgumentNullException.ThrowIfNull(configLoader);

    _configurationProvider = new TestConfigurationProvider(configLoader);
    _settingsAssembler = new TestSettingsAssembler();
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

    var configuration = _configurationProvider.Load(settings);
    if (!configuration.Succeeded)
    {
      return BuildTestContextResult.CreateFailure(configuration.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var paths = TestPathResolver.Resolve(settings, configuration);
    if (!paths.Succeeded || string.IsNullOrWhiteSpace(paths.ReportPath))
    {
      return BuildTestContextResult.CreateFailure(paths.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var scripts = TestScriptResolver.Resolve(settings, configuration);
    if (!scripts.Succeeded)
    {
      return BuildTestContextResult.CreateFailure(scripts.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var resolvedScripts = scripts.Scripts;
    if (resolvedScripts is null)
    {
      return BuildTestContextResult.CreateFailure(scripts.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var testSettings = _settingsAssembler.Build(settings, paths, configuration.MetricAliases);
    if (!testSettings.Succeeded || testSettings.Settings is null)
    {
      return BuildTestContextResult.CreateFailure(testSettings.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var metrics = testSettings.Metrics ?? Array.Empty<string>();

    return BuildTestContextResult.CreateSuccess(
      new TestCommandContext(
        configuration.GeneralOptions,
        configuration.EnvironmentConfiguration,
        configuration.FileConfiguration,
        configuration.MetricAliases,
        resolvedScripts,
        testSettings.Settings,
        metrics));
  }

}

/// <summary>
/// Represents the resolved context for executing the test command.
/// </summary>
/// <param name="GeneralOptions">Resolved general options shared across commands.</param>
/// <param name="EnvironmentConfiguration">Configuration from environment sources.</param>
/// <param name="FileConfiguration">Configuration from file sources.</param>
/// <param name="MetricAliases">Resolved metric alias mappings.</param>
/// <param name="Scripts">Scripts to run around the test command.</param>
/// <param name="TestSettings">Settings for the metric test.</param>
/// <param name="Metrics">Metrics requested by the user.</param>
internal sealed record TestCommandContext(
  ResolvedGeneralOptions GeneralOptions,
  MetricsReporterConfiguration EnvironmentConfiguration,
  MetricsReporterConfiguration FileConfiguration,
  IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> MetricAliases,
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

