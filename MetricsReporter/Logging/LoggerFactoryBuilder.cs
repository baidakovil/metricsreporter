using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;

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
    if (ShouldSuppressConsoleLogging())
    {
      includeConsole = false;
    }

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

  private static bool ShouldSuppressConsoleLogging()
  {
    var processName = Process.GetCurrentProcess().ProcessName;
    if (processName.Contains("testhost", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    var friendlyName = AppDomain.CurrentDomain.FriendlyName;
    if (!string.IsNullOrWhiteSpace(friendlyName) && friendlyName.Contains("testhost", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    var args = Environment.GetCommandLineArgs();
    if (args.Any(a => a.Contains("testhost", StringComparison.OrdinalIgnoreCase)
                      || a.Contains("vstest", StringComparison.OrdinalIgnoreCase)
                      || a.Contains("nunit", StringComparison.OrdinalIgnoreCase)
                      || a.Contains("mstest", StringComparison.OrdinalIgnoreCase)))
    {
      return true;
    }

    var suppressEnv = Environment.GetEnvironmentVariable("METRICSREPORTER_SUPPRESS_CONSOLE_LOG");
    return !string.IsNullOrWhiteSpace(suppressEnv) && bool.TryParse(suppressEnv, out var suppress) && suppress;
  }
}

