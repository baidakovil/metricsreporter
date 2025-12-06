using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.Logging;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Services.Scripts;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Reads metric violations for a namespace and metric.
/// </summary>
internal sealed class ReadCommand : AsyncCommand<ReadSettings>
{
  private readonly MetricsReporterConfigLoader _configLoader;
  private readonly ScriptExecutionService _scriptExecutor;

  /// <summary>
  /// Initializes a new instance of the <see cref="ReadCommand"/> class.
  /// </summary>
  public ReadCommand(MetricsReporterConfigLoader configLoader, ScriptExecutionService scriptExecutor)
  {
    _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
    _scriptExecutor = scriptExecutor ?? throw new ArgumentNullException(nameof(scriptExecutor));
  }

  /// <inheritdoc />
  public override async Task<int> ExecuteAsync(CommandContext context, ReadSettings settings)
  {
    _ = context;
    var cancellationToken = CancellationToken.None;
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

      return (int)MetricsReporterExitCode.ValidationError;
    }

    var general = ConfigurationResolver.ResolveGeneral(
      settings.Verbosity,
      settings.TimeoutSeconds,
      settings.WorkingDirectory,
      settings.LogTruncationLimit,
      envConfig,
      configResult.Configuration);

    var reportPath = FirstNonEmpty(settings.Report, envConfig.Paths.ReadReport, configResult.Configuration.Paths.ReadReport, envConfig.Paths.Report, configResult.Configuration.Paths.Report);
    reportPath = MakeAbsolute(reportPath, general.WorkingDirectory);
    if (string.IsNullOrWhiteSpace(reportPath))
    {
      AnsiConsole.MarkupLine("[red]--report is required (via CLI, env, or config).[/]");
      return (int)MetricsReporterExitCode.ValidationError;
    }

    var thresholdsFile = FirstNonEmpty(settings.ThresholdsFile, envConfig.Paths.Thresholds, configResult.Configuration.Paths.Thresholds);
    thresholdsFile = MakeAbsolute(thresholdsFile, general.WorkingDirectory);

    if (!MetricIdentifierResolver.TryResolve(settings.Metric!, out var resolvedMetric))
    {
      AnsiConsole.MarkupLine($"[red]Unknown metric identifier '{settings.Metric}'.[/]");
      return (int)MetricsReporterExitCode.ValidationError;
    }

    var parsedMetricScripts = ParseMetricScripts(settings.MetricScripts);
    var scripts = ConfigurationResolver.ResolveScripts(
      Array.Empty<string>(),
      settings.Scripts,
      parsedMetricScripts,
      envConfig.Scripts,
      configResult.Configuration.Scripts);

    var scriptsToRun = SelectScriptsForMetrics(
      scripts,
      new[] { resolvedMetric.ToString() });

    var readLogPath = Path.Combine(Path.GetDirectoryName(reportPath) ?? general.WorkingDirectory, "MetricsReporter.read.log");
    using (var fileLogger = new FileLogger(readLogPath))
    {
      var logger = new VerbosityAwareLogger(fileLogger, general.Verbosity);
      var scriptResult = await _scriptExecutor.RunAsync(
        scriptsToRun,
        new ScriptExecutionContext(general.WorkingDirectory, general.Timeout, general.LogTruncationLimit, logger),
        cancellationToken).ConfigureAwait(false);

      if (!scriptResult.IsSuccess)
      {
        return (int)scriptResult.ExitCode;
      }
    }

    var readerSettings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = settings.Namespace!,
      Metric = settings.Metric!,
      SymbolKind = settings.SymbolKind,
      ShowAll = settings.ShowAll,
      RuleId = settings.RuleId,
      GroupBy = settings.GroupBy,
      ThresholdsFile = thresholdsFile,
      IncludeSuppressed = settings.IncludeSuppressed
    };

    var validation = readerSettings.Validate();
    if (!validation.Successful)
    {
      AnsiConsole.MarkupLine($"[red]{validation.Message}[/]");
      return (int)MetricsReporterExitCode.ValidationError;
    }

    var executor = CreateExecutor();
    await executor.ExecuteAsync(readerSettings, cancellationToken).ConfigureAwait(false);
    return 0;
  }

  private static ReadAnyCommandExecutor CreateExecutor()
  {
    var queryService = new SymbolQueryService();
    var orderer = new SymbolSnapshotOrderer();
    var resultHandler = new ReadAnyCommandResultHandler();
    return new ReadAnyCommandExecutor(MetricsReaderCommandHelper.CreateEngineAsync, queryService, orderer, resultHandler);
  }

  private static IReadOnlyList<string> SelectScriptsForMetrics(ResolvedScripts scripts, IEnumerable<string> metrics)
  {
    var metricSet = new HashSet<string>(metrics, StringComparer.OrdinalIgnoreCase);
    var metricScripts = scripts.ReadByMetric
      .Where(entry => entry.Path is not null && entry.Metrics.Any(metric => metricSet.Contains(metric)))
      .Select(entry => entry.Path!)
      .ToArray();

    return scripts.ReadAny.Concat(metricScripts).ToArray();
  }

  private static IReadOnlyList<(string Metric, string Path)> ParseMetricScripts(IEnumerable<string> inputs)
  {
    var result = new List<(string Metric, string Path)>();
    foreach (var input in inputs)
    {
      if (string.IsNullOrWhiteSpace(input))
      {
        continue;
      }

      var parts = input.Split(new[] { '=', ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length != 2)
      {
        continue;
      }

      var metric = parts[0].Trim();
      var path = parts[1].Trim();
      if (metric.Length == 0 || path.Length == 0)
      {
        continue;
      }

      result.Add((metric, path));
    }

    return result;
  }

  private static string? MakeAbsolute(string? path, string workingDirectory)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      return null;
    }

    return Path.IsPathRooted(path)
      ? Path.GetFullPath(path)
      : Path.GetFullPath(Path.Combine(workingDirectory, path));
  }

  private static string? FirstNonEmpty(params string?[] values)
  {
    foreach (var value in values)
    {
      if (!string.IsNullOrWhiteSpace(value))
      {
        return value;
      }
    }

    return null;
  }
}

