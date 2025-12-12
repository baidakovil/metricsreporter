using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MetricsReporter.Logging;

/// <summary>
/// Lightweight file logger provider used for per-command log capture.
/// </summary>
internal sealed class FileLoggerProvider : ILoggerProvider
{
  private readonly string _logFilePath;
  private readonly object _syncRoot = new();
  private readonly StreamWriter _writer;

  public FileLoggerProvider(string logFilePath)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);
    _logFilePath = logFilePath;

    var directory = Path.GetDirectoryName(_logFilePath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
      Directory.CreateDirectory(directory);
    }

    _writer = new StreamWriter(new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
    {
      AutoFlush = true,
      NewLine = Environment.NewLine
    };
  }

  public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _writer, _syncRoot);

  public void Dispose() => _writer.Dispose();

  private sealed class FileLogger : ILogger
  {
    private readonly string _categoryName;
    private readonly StreamWriter _writer;
    private readonly object _syncRoot;

    public FileLogger(string categoryName, StreamWriter writer, object syncRoot)
    {
      _categoryName = categoryName;
      _writer = writer;
      _syncRoot = syncRoot;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
      if (!IsEnabled(logLevel) || formatter is null)
      {
        return;
      }

      var timestamp = DateTimeOffset.UtcNow.ToString("u", CultureInfo.InvariantCulture);
      var builder = new StringBuilder()
        .Append('[').Append(timestamp).Append("] ")
        .Append(logLevel.ToString().ToUpperInvariant()).Append(" ")
        .Append(_categoryName).Append(": ")
        .Append(formatter(state, exception));

      if (exception is not null)
      {
        builder.Append(" :: ").Append(exception.GetType().Name).Append(": ").Append(exception.Message);
      }

      var line = builder.ToString();
      lock (_syncRoot)
      {
        _writer.WriteLine(line);
      }
    }
  }

  private sealed class NullScope : IDisposable
  {
    public static readonly NullScope Instance = new();
    public void Dispose()
    {
    }
  }
}

