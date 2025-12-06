using System;

namespace MetricsReporter.Logging;

/// <summary>
/// Wraps an <see cref="ILogger"/> and filters informational messages based on verbosity.
/// </summary>
public sealed class VerbosityAwareLogger : ILogger
{
  private readonly ILogger _inner;
  private readonly bool _logInformation;

  /// <summary>
  /// Initializes a new instance of the <see cref="VerbosityAwareLogger"/> class.
  /// </summary>
  /// <param name="inner">The underlying logger.</param>
  /// <param name="verbosity">Verbosity value (quiet|minimal|normal|detailed).</param>
  public VerbosityAwareLogger(ILogger inner, string verbosity)
  {
    _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    _logInformation = !string.Equals(verbosity, "quiet", StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(verbosity, "minimal", StringComparison.OrdinalIgnoreCase);
  }

  /// <inheritdoc />
  public void LogInformation(string message)
  {
    if (_logInformation)
    {
      _inner.LogInformation(message);
    }
  }

  /// <inheritdoc />
  public void LogError(string message, Exception? exception = null)
    => _inner.LogError(message, exception);
}

