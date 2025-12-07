namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Determines whether script execution should proceed for a given request.
/// </summary>
internal interface IScriptExecutionGuard
{
  /// <summary>
  /// Evaluates skip conditions and optionally notifies the user.
  /// </summary>
  /// <param name="request">Script execution parameters.</param>
  /// <param name="operationName">Name of the operation for user messaging.</param>
  /// <returns><see langword="true"/> when execution should be skipped; otherwise <see langword="false"/>.</returns>
  bool ShouldSkip(GenerateScriptRunRequest request, string operationName);
}
