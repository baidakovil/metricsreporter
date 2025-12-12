using System;
using Microsoft.Extensions.Logging;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Holds a logger and its disposable scope for script execution.
/// </summary>
internal sealed class ScriptLoggerScope : IDisposable
{
  public ScriptLoggerScope(ILogger logger, ILoggerFactory factory)
  {
    Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _factory = factory ?? throw new ArgumentNullException(nameof(factory));
  }

  /// <summary>
  /// Logger configured for script execution.
  /// </summary>
  public ILogger Logger { get; }

  private readonly ILoggerFactory _factory;

  public void Dispose()
  {
    _factory.Dispose();
  }
}

