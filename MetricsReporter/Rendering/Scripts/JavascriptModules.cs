namespace MetricsReporter.Rendering.Scripts;

using System.Collections.Generic;

/// <summary>
/// Serves as a container for modular JavaScript snippets used by <see cref="HtmlScriptGenerator"/>.
/// </summary>
internal static partial class JavascriptModules
{
  private static IReadOnlyCollection<ScriptFragment>? _refactoredFragments;

  /// <summary>
  /// Gets the modernized JavaScript fragments that compose the metrics report script.
  /// </summary>
  internal static IReadOnlyCollection<ScriptFragment> RefactoredFragments
    => _refactoredFragments ??= BuildRefactoredFragments();

  private static ScriptFragment[] BuildRefactoredFragments()
    => new[]
    {
      new ScriptFragment("Utilities", Utilities),
      new ScriptFragment("Tooltips", Tooltips),
      new ScriptFragment("StateManagement", StateManagement),
      new ScriptFragment("Filtering", Filtering),
      new ScriptFragment("Sorting", Sorting),
      new ScriptFragment("Actions", Actions),
      new ScriptFragment("Hotkeys", Hotkeys),
      new ScriptFragment("Bootstrap", Bootstrap)
    };
}


