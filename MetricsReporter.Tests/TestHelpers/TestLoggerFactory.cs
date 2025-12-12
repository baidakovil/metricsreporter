using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace MetricsReporter.Tests.TestHelpers;

/// <summary>
/// A simple logger factory that creates TestLogger instances for testing.
/// </summary>
internal sealed class TestLoggerFactory : ILoggerFactory
{
  private readonly List<LogEntry> _allEntries = new();

  public IReadOnlyList<LogEntry> AllEntries => _allEntries;

  public void AddProvider(ILoggerProvider provider)
  {
    // No-op for test factory
  }

  public ILogger CreateLogger(string categoryName)
  {
    return new TestLogger(categoryName, _allEntries);
  }

  public void Dispose()
  {
    // No-op for test factory
  }

  internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception, string CategoryName);

  private sealed class TestLogger : ILogger
  {
    private readonly string _categoryName;
    private readonly List<LogEntry> _allEntries;

    public TestLogger(string categoryName, List<LogEntry> allEntries)
    {
      _categoryName = categoryName;
      _allEntries = allEntries;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
      if (formatter is null)
      {
        return;
      }

      var message = formatter(state, exception);
      _allEntries.Add(new LogEntry(logLevel, message, exception, _categoryName));
    }

    private sealed class NullScope : IDisposable
    {
      public static readonly NullScope Instance = new();
      public void Dispose()
      {
      }
    }
  }
}

