using System;
using System.Collections.Generic;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.Services;
using MetricsReporter.Services.Scripts;
using MetricsReporter.Model;
using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds the execution context for the generate command, combining configuration, inputs, and validation.
/// </summary>
internal sealed class GenerateCommandContextBuilder
{
  private readonly GenerateConfigurationProvider _configurationProvider;

  public GenerateCommandContextBuilder(MetricsReporterConfigLoader configLoader)
  {
    ArgumentNullException.ThrowIfNull(configLoader);

    _configurationProvider = new GenerateConfigurationProvider(configLoader);
  }

  /// <summary>
  /// Creates a fully validated generate command context or returns an error outcome.
  /// </summary>
  /// <param name="settings">Generate CLI settings.</param>
  /// <returns>Outcome containing context or exit code.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Build orchestrates configuration, path resolution, script resolution, and options assembly in a single entry point; further splitting into smaller methods would scatter validation flow and reduce readability for the command pipeline.")]
  public BuildGenerateContextResult Build(GenerateSettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);

    var configuration = _configurationProvider.Load(settings);
    if (!configuration.Succeeded)
    {
      return BuildGenerateContextResult.CreateFailure(configuration.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var inputs = GenerateInputResolver.Resolve(settings, configuration);
    if (!inputs.Succeeded || inputs.Inputs is null || inputs.LogPath is null)
    {
      return BuildGenerateContextResult.CreateFailure(inputs.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var resolvedInputs = inputs.Inputs;
    var logPath = inputs.LogPath;

    var validation = GenerateInputResolver.Validate(resolvedInputs);
    if (!validation.Succeeded)
    {
      AnsiConsole.MarkupLine($"[red]{validation.Error}[/]");
      return BuildGenerateContextResult.CreateFailure((int)MetricsReporterExitCode.ValidationError);
    }

    var scripts = GenerateScriptResolver.Resolve(settings, configuration);
    if (!scripts.Succeeded)
    {
      return BuildGenerateContextResult.CreateFailure(scripts.ExitCode ?? (int)MetricsReporterExitCode.ValidationError);
    }

    var options = BuildOptions(resolvedInputs, logPath, configuration.MetricAliases);
    return BuildGenerateContextResult.CreateSuccess(
      new GenerateCommandContext(
        configuration.GeneralOptions,
        scripts.Scripts!,
        resolvedInputs,
        options,
        logPath));
  }

  private static MetricsReporterOptions BuildOptions(
    ResolvedGenerateInputs inputs,
    string logPath,
    IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> metricAliases)
  {
    return new MetricsReporterOptions
    {
      SolutionName = inputs.SolutionName ?? "Solution",
      MetricsDirectory = inputs.MetricsDir ?? string.Empty,
      AltCoverPaths = inputs.AltCover,
      RoslynPaths = inputs.Roslyn,
      SarifPaths = inputs.Sarif,
      BaselinePath = inputs.Baseline,
      BaselineReference = inputs.BaselineReference,
      ThresholdsJson = inputs.ThresholdsInline,
      ThresholdsPath = inputs.ThresholdsFile,
      InputJsonPath = inputs.InputJson,
      OutputJsonPath = inputs.OutputJson ?? string.Empty,
      OutputHtmlPath = inputs.OutputHtml ?? string.Empty,
      LogFilePath = logPath,
      ExcludedMemberNamesPatterns = inputs.ExcludedMembers,
      ExcludedAssemblyNames = inputs.ExcludedAssemblies,
      ExcludedTypeNamePatterns = inputs.ExcludedTypes,
      ExcludeMethods = inputs.ExcludeMethods,
      ExcludeProperties = inputs.ExcludeProperties,
      ExcludeFields = inputs.ExcludeFields,
      ExcludeEvents = inputs.ExcludeEvents,
      ReplaceMetricsBaseline = inputs.ReplaceBaseline,
      MetricsReportStoragePath = inputs.BaselineStoragePath,
      CoverageHtmlDir = inputs.CoverageHtmlDir,
      AnalyzeSuppressedSymbols = inputs.AnalyzeSuppressedSymbols,
      SuppressedSymbolsPath = inputs.SuppressedSymbols,
      SolutionDirectory = inputs.SolutionDirectory,
      SourceCodeFolders = inputs.SourceCodeFolders,
      MetricAliases = metricAliases
    };
  }

}

/// <summary>
/// Represents the fully resolved inputs for executing the generate command.
/// </summary>
/// <param name="GeneralOptions">Resolved general options shared across commands.</param>
/// <param name="Scripts">Scripts to run for the generate command.</param>
/// <param name="Inputs">Resolved generate inputs including sources and outputs.</param>
/// <param name="Options">Options passed to the MetricsReporter application.</param>
/// <param name="LogPath">Log path chosen for generation.</param>
internal sealed record GenerateCommandContext(
  ResolvedGeneralOptions GeneralOptions,
  ResolvedScripts Scripts,
  ResolvedGenerateInputs Inputs,
  MetricsReporterOptions Options,
  string LogPath);

/// <summary>
/// Outcome of building the generate command context.
/// </summary>
/// <param name="Succeeded">Indicates whether context creation succeeded.</param>
/// <param name="ExitCode">Exit code to return when creation fails.</param>
/// <param name="Context">Resolved context when successful.</param>
internal sealed record BuildGenerateContextResult(bool Succeeded, int? ExitCode, GenerateCommandContext? Context)
{
  public static BuildGenerateContextResult CreateSuccess(GenerateCommandContext context) =>
    new(true, null, context);

  public static BuildGenerateContextResult CreateFailure(int exitCode) =>
    new(false, exitCode, null);
}

