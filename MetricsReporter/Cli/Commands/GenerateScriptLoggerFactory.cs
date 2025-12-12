using MetricsReporter.Logging;
using MetricsReporter.Services.Scripts;
using Microsoft.Extensions.Logging;

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

    var minimumLevel = LoggerFactoryBuilder.FromVerbosity(request.Verbosity);
    var factory = LoggerFactoryBuilder.Create(request.LogPath, minimumLevel, verbosity: request.Verbosity);
    var logger = factory.CreateLogger<ScriptExecutionService>();
    return new ScriptLoggerScope(logger, factory);
  }
}

