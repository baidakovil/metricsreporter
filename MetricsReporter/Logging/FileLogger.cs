namespace MetricsReporter.Logging;

using System;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// Minimal file logger used by the metrics aggregator.
/// </summary>
public sealed class FileLogger : ILogger, IDisposable
{
  private readonly StreamWriter _writer;
  private readonly object _syncRoot = new();

  /// <summary>
  /// Initialises a new instance of <see cref="FileLogger"/>.
  /// </summary>
  /// <param name="logFilePath">Destination log file path.</param>
  public FileLogger(string logFilePath)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);

    var directory = Path.GetDirectoryName(logFilePath);
    if (!string.IsNullOrEmpty(directory))
    {
      Directory.CreateDirectory(directory);
    }

    _writer = new StreamWriter(new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
    {
      AutoFlush = true
    };
  }

  /// <summary>
  /// Writes an informational message.
  /// </summary>
  public void LogInformation(string message)
      => WriteLine("INFO", message);

  /// <summary>
  /// Writes an error message optionally accompanied by an exception.
  /// </summary>
  public void LogError(string message, Exception? exception = null)
  {
    var builder = new StringBuilder(message);
    if (exception is not null)
    {
      builder.Append(" :: ").Append(exception.GetType().Name).Append(": ").Append(exception.Message);
    }

    WriteLine("ERROR", builder.ToString());
  }

  /// <inheritdoc />
  public void Dispose()
  {
    _writer.Dispose();
  }

  private void WriteLine(string level, string message)
  {
    var timestamp = DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture);
    lock (_syncRoot)
    {
      _writer.WriteLine($"[{timestamp}] {level}: {message}");
    }
  }
}


