using System;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

internal sealed class ReadScriptContextFactory
{
  private readonly string _operationName;
  private readonly string _logFileName;

  public ReadScriptContextFactory(string operationName = "read", string logFileName = "MetricsReporter.read.log")
  {
    _operationName = operationName;
    _logFileName = logFileName;
  }

  /// <summary>
  /// Creates a script aggregation context for the read command.
  /// </summary>
  /// <param name="context">Resolved read command context.</param>
  /// <returns>Prepared script aggregation context.</returns>
  public ScriptAggregationContext Create(ReadCommandContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    return new ScriptAggregationContext(
      context.GeneralOptions,
      context.EnvironmentConfiguration,
      context.FileConfiguration,
      context.Scripts,
      context.Metrics,
      context.ReportPath,
      ScriptSelection.SelectReadScripts,
      _operationName,
      _logFileName);
  }
}

