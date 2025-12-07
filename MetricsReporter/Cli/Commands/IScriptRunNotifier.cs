namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Notifies users about script execution decisions.
/// </summary>
internal interface IScriptRunNotifier
{
  /// <summary>
  /// Informs the user that scripts were disabled and will be skipped.
  /// </summary>
  /// <param name="operationName">Name of the operation being skipped.</param>
  void NotifyScriptsDisabled(string operationName);

  /// <summary>
  /// Informs the user that no scripts are configured for the current operation.
  /// </summary>
  /// <param name="operationName">Name of the operation being skipped.</param>
  void NotifyNoScripts(string operationName);
}
