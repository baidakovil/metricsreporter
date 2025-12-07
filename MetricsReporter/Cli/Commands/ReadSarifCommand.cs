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
using MetricsReporter.Model;
using MetricsReporter.Services;
using MetricsReporter.Services.Scripts;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Aggregates SARIF-based metrics grouped by rule identifier.
/// </summary>
internal sealed class ReadSarifCommand : AsyncCommand<ReadSarifSettings>
{
  private readonly MetricsReporterConfigLoader _configLoader;
  private readonly ScriptExecutionService _scriptExecutor;
  private static readonly char[] MetricScriptSeparators = ['=', ':'];

  public ReadSarifCommand(MetricsReporterConfigLoader configLoader, ScriptExecutionService scriptExecutor)
  {
    _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
    _scriptExecutor = scriptExecutor ?? throw new ArgumentNullException(nameof(scriptExecutor));
  }

  /// <inheritdoc />
  public override async Task<int> ExecuteAsync(CommandContext context, ReadSarifSettings settings)
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
      settings.RunScripts,
      settings.AggregateAfterScripts,
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

    var parsedMetricScripts = ParseMetricScripts(settings.MetricScripts);
    var scripts = ConfigurationResolver.ResolveScripts(
      Array.Empty<string>(),
      settings.Scripts,
      parsedMetricScripts,
      Array.Empty<string>(),
      Array.Empty<(string Metric, string Path)>(),
      envConfig.Scripts,
      configResult.Configuration.Scripts);

    var sarifSettings = new SarifMetricSettings
    {
      ReportPath = reportPath,
      Namespace = settings.Namespace!,
      Metric = settings.Metric,
      SymbolKind = settings.SymbolKind,
      RuleId = settings.RuleId,
      GroupBy = settings.GroupBy,
      ShowAll = settings.ShowAll,
      ThresholdsFile = thresholdsFile,
      IncludeSuppressed = settings.IncludeSuppressed
    };

    var validation = sarifSettings.Validate();
    if (!validation.Successful)
    {
      AnsiConsole.MarkupLine($"[red]{validation.Message}[/]");
      return (int)MetricsReporterExitCode.ValidationError;
    }

    if (!sarifSettings.TryResolveSarifMetrics(out var metrics) || metrics is null)
    {
      AnsiConsole.MarkupLine($"[red]Unknown SARIF metric '{sarifSettings.EffectiveMetricName}'.[/]");
      return (int)MetricsReporterExitCode.ValidationError;
    }

    var scriptsToRun = SelectScriptsForMetrics(
      scripts,
      metrics.Select(m => m.ToString()));
    var hasScripts = scriptsToRun.Length > 0;

    var readLogPath = Path.Combine(Path.GetDirectoryName(reportPath) ?? general.WorkingDirectory, "MetricsReporter.read.log");
    using (var fileLogger = new FileLogger(readLogPath))
    {
      var logger = new VerbosityAwareLogger(fileLogger, general.Verbosity);
      if (general.RunScripts && hasScripts)
      {
        var scriptResult = await _scriptExecutor.RunAsync(
          scriptsToRun,
          new ScriptExecutionContext(general.WorkingDirectory, general.Timeout, general.LogTruncationLimit, logger),
          cancellationToken).ConfigureAwait(false);

        if (!scriptResult.IsSuccess)
        {
          return (int)scriptResult.ExitCode;
        }
      }
      else if (!general.RunScripts)
      {
        AnsiConsole.MarkupLine("[yellow]Scripts disabled (--run-scripts=false); skipping readsarif scripts and aggregation.[/]");
      }
      else if (!hasScripts && general.AggregateAfterScripts)
      {
        AnsiConsole.MarkupLine("[yellow]No readsarif scripts configured; skipping post-script aggregation.[/]");
      }

      var shouldAggregate = general.RunScripts && general.AggregateAfterScripts && hasScripts;
      if (shouldAggregate)
      {
        var aggregationInputs = AggregationOptionsResolver.Resolve(
          envConfig.Paths,
          configResult.Configuration.Paths,
          general.WorkingDirectory,
          reportPath);
        var aggregationValidation = AggregationOptionsResolver.Validate(aggregationInputs);
        if (!aggregationValidation.Succeeded)
        {
          AnsiConsole.MarkupLine($"[red]{aggregationValidation.Error}[/]");
          return (int)MetricsReporterExitCode.ValidationError;
        }

        var aggregationLogPath = AggregationOptionsResolver.BuildLogPath(aggregationInputs, general.WorkingDirectory);
        var aggregationOptions = AggregationOptionsResolver.BuildOptions(aggregationInputs, aggregationLogPath);
        var application = new MetricsReporterApplication();
        var aggregationExit = await application.RunAsync(aggregationOptions, cancellationToken).ConfigureAwait(false);
        if (aggregationExit != MetricsReporterExitCode.Success)
        {
          AnsiConsole.MarkupLine($"[red]Aggregation after scripts failed with exit code {(int)aggregationExit}.[/]");
          return (int)aggregationExit;
        }
      }
      else if (!general.AggregateAfterScripts && hasScripts)
      {
        AnsiConsole.MarkupLine("[yellow]Aggregation after scripts disabled (--aggregate-after-scripts=false).[/]");
      }
    }

    var executor = CreateExecutor();
    await executor.ExecuteAsync(sarifSettings, cancellationToken).ConfigureAwait(false);
    return 0;
  }

  private static ReadSarifCommandExecutor CreateExecutor()
  {
    var aggregator = new SarifGroupAggregator();
    var sorter = new SarifGroupSorter();
    var filter = new SarifGroupFilter();
    var resultHandler = new ReadSarifCommandResultHandler();
    return new ReadSarifCommandExecutor(MetricsReaderCommandHelper.CreateEngineAsync, aggregator, sorter, filter, resultHandler);
  }

  private static string[] SelectScriptsForMetrics(ResolvedScripts scripts, IEnumerable<string> metrics)
  {
    var metricSet = new HashSet<string>(metrics, StringComparer.OrdinalIgnoreCase);
    var metricScripts = scripts.ReadByMetric
      .Where(entry => entry.Path is not null && entry.Metrics.Any(metric => metricSet.Contains(metric)))
      .Select(entry => entry.Path!)
      .ToArray();

    return scripts.ReadAny.Concat(metricScripts).ToArray();
  }

  private static List<(string Metric, string Path)> ParseMetricScripts(IEnumerable<string> inputs)
  {
    var result = new List<(string Metric, string Path)>();
    foreach (var input in inputs)
    {
      if (string.IsNullOrWhiteSpace(input))
      {
        continue;
      }

      var parts = input.Split(MetricScriptSeparators, 2, StringSplitOptions.RemoveEmptyEntries);
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

