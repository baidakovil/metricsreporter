using MetricsReporter.Logging;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Creates logger scopes for generate script execution.
/// </summary>
internal interface IGenerateScriptLoggerFactory
{
  /// <summary>
  /// Creates a new logger scope tailored to the provided request.
  /// </summary>
  /// <param name="request">Script execution parameters.</param>
  /// <returns>Disposable logger scope.</returns>
  ScriptLoggerScope CreateScope(GenerateScriptRunRequest request);
}
