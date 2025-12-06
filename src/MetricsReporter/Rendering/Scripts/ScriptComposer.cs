namespace MetricsReporter.Rendering.Scripts;

using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Provides helper methods to combine JavaScript fragments.
/// </summary>
internal static class ScriptComposer
{
  /// <summary>
  /// Creates a single inline script by concatenating all fragments with separators and banner comments.
  /// </summary>
  /// <param name="fragments">Fragments to include.</param>
  /// <returns>Combined JavaScript.</returns>
  public static string Compose(IReadOnlyCollection<ScriptFragment> fragments)
  {
    if (fragments == null || fragments.Count == 0)
    {
      return string.Empty;
    }

    var builder = new StringBuilder(capacity: fragments.Sum(fragment => fragment.Content.Length + 96));

    foreach (var fragment in fragments)
    {
      builder.AppendLine("//#region " + fragment.Name);
      builder.AppendLine(fragment.Content.Trim());
      builder.AppendLine("//#endregion");
      builder.AppendLine();
    }

    return builder.ToString();
  }
}


