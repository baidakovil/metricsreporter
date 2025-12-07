using System;
using MetricsReporter.Logging;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Holds a logger and its disposable scope for script execution.
/// </summary>
internal sealed class ScriptLoggerScope : IDisposable
{
  public ScriptLoggerScope(ILogger logger, IDisposable disposable)
  {
    Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _disposable = disposable ?? throw new ArgumentNullException(nameof(disposable));
  }

  /// <summary>
  /// Logger configured for script execution.
  /// </summary>
  public ILogger Logger { get; }

  private readonly IDisposable _disposable;

  public void Dispose()
  {
    _disposable.Dispose();
  }
}

