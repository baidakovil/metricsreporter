namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Determines whether script execution should be skipped and emits user notifications.
/// </summary>
internal sealed class ScriptExecutionGuard : IScriptExecutionGuard
{
  private readonly IScriptRunNotifier _notifier;

  public ScriptExecutionGuard(IScriptRunNotifier notifier)
  {
    _notifier = notifier ?? throw new System.ArgumentNullException(nameof(notifier));
  }

  /// <inheritdoc />
  public bool ShouldSkip(GenerateScriptRunRequest request, string operationName)
  {
    ArgumentNullException.ThrowIfNull(request);
    var name = string.IsNullOrWhiteSpace(operationName) ? "generate" : operationName;

    if (!request.ShouldRunScripts)
    {
      _notifier.NotifyScriptsDisabled(name);
      return true;
    }

    if (!request.HasScripts)
    {
      _notifier.NotifyNoScripts(name);
      return true;
    }

    return false;
  }
}

