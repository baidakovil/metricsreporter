using System;
using System.IO;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Builds script execution requests for the generate command from resolved context.
/// </summary>
internal static class GenerateScriptRequestFactory
{
  /// <summary>
  /// Builds a request DTO for generate script execution.
  /// </summary>
  /// <param name="context">Resolved generate command context.</param>
  /// <returns>Request containing script execution parameters.</returns>
  public static GenerateScriptRunRequest Create(GenerateCommandContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    var logPath = context.LogPath;
    if (string.IsNullOrWhiteSpace(logPath))
    {
      var fallback = Path.Combine(context.GeneralOptions.WorkingDirectory, "MetricsReporter.log");
      logPath = fallback;
    }

    var hasScripts = context.Scripts.Generate.Count > 0;
    return new GenerateScriptRunRequest(
      context.GeneralOptions.RunScripts,
      hasScripts,
      logPath,
      context.GeneralOptions.Verbosity,
      context.Scripts.Generate,
      context.GeneralOptions.WorkingDirectory,
      context.GeneralOptions.Timeout,
      context.GeneralOptions.LogTruncationLimit);
  }
}

