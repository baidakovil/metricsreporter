using System;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

internal sealed class TestScriptContextFactory
{
  private readonly string _operationName;
  private readonly string _logFileName;

  public TestScriptContextFactory(string operationName = "test", string logFileName = "MetricsReporter.read.log")
  {
    _operationName = operationName;
    _logFileName = logFileName;
  }

  /// <summary>
  /// Creates a script aggregation context for the test command using resolved inputs.
  /// </summary>
  /// <param name="context">Resolved test command context.</param>
  /// <returns>Prepared script aggregation context.</returns>
  public ScriptAggregationContext Create(TestCommandContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    return new ScriptAggregationContext(
      context.GeneralOptions,
      context.EnvironmentConfiguration,
      context.FileConfiguration,
      context.Scripts,
      context.Metrics,
      context.TestSettings.ReportPath!,
      ScriptSelection.SelectTestScripts,
      _operationName,
      _logFileName);
  }
}

