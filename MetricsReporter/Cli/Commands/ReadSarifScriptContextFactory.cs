using System;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

internal sealed class ReadSarifScriptContextFactory
{
  private readonly string _operationName;
  private readonly string _logFileName;

  public ReadSarifScriptContextFactory(string operationName = "readsarif", string logFileName = "MetricsReporter.read.log")
  {
    _operationName = operationName;
    _logFileName = logFileName;
  }

  /// <summary>
  /// Creates a script aggregation context for the readsarif command.
  /// </summary>
  /// <param name="context">Resolved readsarif command context.</param>
  /// <returns>Prepared script aggregation context.</returns>
  public ScriptAggregationContext Create(ReadSarifCommandContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    return new ScriptAggregationContext(
      context.GeneralOptions,
      context.EnvironmentConfiguration,
      context.FileConfiguration,
      context.Scripts,
      context.Metrics,
      context.SarifSettings.ReportPath!,
      ScriptSelection.SelectReadScripts,
      _operationName,
      _logFileName);
  }
}

