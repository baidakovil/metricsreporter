namespace MetricsReporter.Rendering;

using MetricsReporter.Rendering.Scripts;

/// <summary>
/// Generates JavaScript code for the HTML metrics report.
/// </summary>
internal static class HtmlScriptGenerator
{
  /// <summary>
  /// Generates the complete JavaScript code for the metrics report.
  /// </summary>
  /// <returns>The JavaScript code as a string.</returns>
  public static string Generate()
      => ScriptComposer.Compose(JavascriptModules.RefactoredFragments);
}

