using Microsoft.Extensions.Logging;

namespace MetricsReporter.Logging;

/// <summary>
/// Centralizes logger factory creation for CLI and services.
/// </summary>
internal static class LoggerFactoryBuilder
{
  private const string DefaultTimestampFormat = "HH:mm:ss ";

  /// <summary>
  /// Builds a logger factory configured with simple console output and an optional file sink.
  /// </summary>
  /// <param name="logFilePath">Optional log file path; when null or whitespace, no file sink is added.</param>
  /// <param name="minimumLevel">Minimum log level.</param>
  /// <param name="includeConsole">When true, emits logs to the console.</param>
  /// <returns>Configured logger factory.</returns>
  public static ILoggerFactory Create(string? logFilePath, LogLevel minimumLevel, bool includeConsole = true)
  {
    return LoggerFactory.Create(builder =>
    {
      builder.ClearProviders();
      builder.SetMinimumLevel(minimumLevel);

      if (includeConsole)
      {
        builder.AddSimpleConsole(options =>
        {
          options.SingleLine = true;
          options.TimestampFormat = DefaultTimestampFormat;
          options.UseUtcTimestamp = true;
          options.IncludeScopes = true;
        });
      }

      if (!string.IsNullOrWhiteSpace(logFilePath))
      {
        builder.AddProvider(new FileLoggerProvider(logFilePath));
      }
    });
  }

  /// <summary>
  /// Maps CLI verbosity strings to <see cref="LogLevel"/> values.
  /// </summary>
  /// <param name="verbosity">Verbosity value (quiet|minimal|normal|detailed).</param>
  /// <returns>Log level to apply.</returns>
  public static LogLevel FromVerbosity(string? verbosity)
  {
    if (string.IsNullOrWhiteSpace(verbosity))
    {
      return LogLevel.Information;
    }

    return verbosity.Trim().ToLowerInvariant() switch
    {
      "quiet" => LogLevel.Warning,
      "minimal" => LogLevel.Warning,
      "normal" => LogLevel.Information,
      "detailed" => LogLevel.Debug,
      _ => LogLevel.Information
    };
  }
}

