using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace MetricsReporter.Tests.TestHelpers;

internal sealed class TestLogger<T> : ILogger<T>
{
  private readonly List<LogEntry> _entries = new();

  public IReadOnlyList<LogEntry> Entries => _entries;

  public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

  public bool IsEnabled(LogLevel logLevel) => true;

  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
  {
    if (formatter is null)
    {
      return;
    }

    var message = formatter(state, exception);
    _entries.Add(new LogEntry(logLevel, message, exception));
  }

  internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

  private sealed class NullScope : IDisposable
  {
    public static readonly NullScope Instance = new();
    public void Dispose()
    {
    }
  }
}

