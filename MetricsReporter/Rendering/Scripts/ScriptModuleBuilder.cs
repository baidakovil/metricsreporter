namespace MetricsReporter.Rendering.Scripts
{
  /// <summary>
  /// Provides helper methods for building JavaScript module fragments.
  /// </summary>
  internal static class ScriptModuleBuilder
  {
    /// <summary>
    /// Wraps provided JavaScript code with an immediately-invoked function expression (IIFE).
    /// </summary>
    /// <param name="moduleName">Module name used for diagnostics.</param>
    /// <param name="body">Raw JavaScript body.</param>
    /// <returns>Script fragment.</returns>
    public static ScriptFragment CreateModule(string moduleName, string body)
    {
      if (string.IsNullOrWhiteSpace(body))
      {
        return new ScriptFragment(moduleName, $"(function {moduleName}(){{}})();");
      }

      var trimmed = body.Trim();
      var content = $@"(function {moduleName}(){{
{trimmed}
}})();";
      return new ScriptFragment(moduleName, content);
    }
  }
}


