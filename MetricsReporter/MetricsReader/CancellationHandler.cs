namespace MetricsReporter.MetricsReader;

using System;
using System.Threading;

/// <summary>
/// Handles cancellation token setup and cleanup for console applications.
/// </summary>
internal sealed class CancellationHandler : IDisposable
{
  private readonly CancellationTokenSource _cancellationTokenSource;
  private readonly ConsoleCancelEventHandler _handler;

  /// <summary>
  /// Initializes a new instance of the <see cref="CancellationHandler"/> class.
  /// </summary>
  public CancellationHandler()
  {
    _cancellationTokenSource = new CancellationTokenSource();
    _handler = (_, eventArgs) =>
    {
      eventArgs.Cancel = true;
      _cancellationTokenSource.Cancel();
    };

    Console.CancelKeyPress += _handler;
    MetricsReaderCancellation.Initialize(_cancellationTokenSource.Token);
  }

  /// <summary>
  /// Gets the cancellation token.
  /// </summary>
  public CancellationToken Token => _cancellationTokenSource.Token;

  /// <inheritdoc/>
  public void Dispose()
  {
    Console.CancelKeyPress -= _handler;
    _cancellationTokenSource.Dispose();
  }
}


