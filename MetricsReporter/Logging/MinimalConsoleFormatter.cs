using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace MetricsReporter.Logging;

/// <summary>
/// Options for MinimalConsoleFormatter that include verbosity control.
/// </summary>
internal sealed class MinimalConsoleFormatterOptions : SimpleConsoleFormatterOptions
{
  /// <summary>
  /// Gets or sets a value indicating whether to include timestamp in output.
  /// When false (normal verbosity), timestamps are omitted from console output.
  /// </summary>
  public bool IncludeTimestamp { get; set; } = true;
}

/// <summary>
/// Minimal single-line console formatter without categories or scopes.
/// </summary>
internal sealed class MinimalConsoleFormatter : ConsoleFormatter
{
  private readonly IOptionsMonitor<MinimalConsoleFormatterOptions> _options;

  public MinimalConsoleFormatter(IOptionsMonitor<MinimalConsoleFormatterOptions> options) : base("minimal")
  {
    _options = options ?? throw new ArgumentNullException(nameof(options));
  }

  public override void Write<TState>(
    in LogEntry<TState> logEntry,
    IExternalScopeProvider? scopeProvider,
    TextWriter textWriter)
  {
    if (textWriter is null)
    {
      return;
    }

    var formatter = logEntry.Formatter ?? ((state, exception) => state?.ToString() ?? string.Empty);
    var message = formatter(logEntry.State, logEntry.Exception);
    if (string.IsNullOrEmpty(message) && logEntry.Exception is null)
    {
      return;
    }

    var formatterOptions = _options.CurrentValue;
    var timestamp = formatterOptions.UseUtcTimestamp == true
      ? DateTimeOffset.UtcNow
      : DateTimeOffset.Now;

    var levelText = ToShortLevel(logEntry.LogLevel);
    
    // Include timestamp unless explicitly disabled (normal verbosity)
    if (formatterOptions.IncludeTimestamp)
    {
      var timestampText = string.IsNullOrEmpty(formatterOptions.TimestampFormat)
        ? timestamp.ToString("HH:mm:ss ")
        : timestamp.ToString(formatterOptions.TimestampFormat);
      textWriter.Write(timestampText);
    }
    
    textWriter.Write(levelText);
    textWriter.Write(": ");
    textWriter.Write(message);

    if (logEntry.Exception is not null)
    {
      textWriter.Write(" :: ");
      textWriter.Write(logEntry.Exception.GetType().Name);
      textWriter.Write(": ");
      textWriter.Write(logEntry.Exception.Message);
    }

    textWriter.WriteLine();
  }

  private static string ToShortLevel(LogLevel level)
    => level switch
    {
      LogLevel.Trace => "trace",
      LogLevel.Debug => "debug",
      LogLevel.Information => "info",
      LogLevel.Warning => "warn",
      LogLevel.Error => "error",
      LogLevel.Critical => "crit",
      _ => "info"
    };
}

