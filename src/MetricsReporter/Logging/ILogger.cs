namespace MetricsReporter.Logging;

/// <summary>
/// Provides logging capabilities for the metrics reporter.
/// </summary>
public interface ILogger
{
  /// <summary>
  /// Writes an informational message.
  /// </summary>
  /// <param name="message">The message to log.</param>
  void LogInformation(string message);

  /// <summary>
  /// Writes an error message optionally accompanied by an exception.
  /// </summary>
  /// <param name="message">The error message to log.</param>
  /// <param name="exception">Optional exception associated with the error.</param>
  void LogError(string message, Exception? exception = null);
}


