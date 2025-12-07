using MetricsReporter.Logging;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Creates logger scopes for generate script execution.
/// </summary>
internal sealed class GenerateScriptLoggerFactory : IGenerateScriptLoggerFactory
{
  /// <inheritdoc />
  public ScriptLoggerScope CreateScope(GenerateScriptRunRequest request)
  {
    ArgumentNullException.ThrowIfNull(request);

    var fileLogger = new FileLogger(request.LogPath);
    var logger = new VerbosityAwareLogger(fileLogger, request.Verbosity);
    return new ScriptLoggerScope(logger, fileLogger);
  }
}

