namespace MetricsReporter.Rendering.Scripts
{
  /// <summary>
  /// Represents a reusable JavaScript fragment that can be composed into the final inline script.
  /// </summary>
  /// <param name="Name">Human-friendly name used for diagnostics and tests.</param>
  /// <param name="Content">JavaScript code to inject into the HTML document.</param>
  internal sealed record ScriptFragment(string Name, string Content);
}



