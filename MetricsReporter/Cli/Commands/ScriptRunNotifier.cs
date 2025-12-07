using Spectre.Console;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Emits user-facing notifications about script execution state.
/// </summary>
internal sealed class ScriptRunNotifier : IScriptRunNotifier
{
  /// <inheritdoc />
  public void NotifyScriptsDisabled(string operationName)
  {
    AnsiConsole.MarkupLine($"[yellow]Scripts disabled (--run-scripts=false); skipping {operationName} scripts and aggregation.[/]");
  }

  /// <inheritdoc />
  public void NotifyNoScripts(string operationName)
  {
    AnsiConsole.MarkupLine($"[yellow]No {operationName} scripts configured; skipping post-script aggregation.[/]");
  }
}

