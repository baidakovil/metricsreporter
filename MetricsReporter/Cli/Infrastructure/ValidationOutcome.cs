namespace MetricsReporter.Cli.Infrastructure;

/// <summary>
/// Represents the result of validating CLI inputs before execution.
/// </summary>
internal sealed record ValidationOutcome(bool Succeeded, string? Error)
{
  /// <summary>
  /// Creates a successful validation outcome.
  /// </summary>
  public static ValidationOutcome Success() => new(true, null);

  /// <summary>
  /// Creates a failed validation outcome with the specified message.
  /// </summary>
  /// <param name="message">Validation error message.</param>
  public static ValidationOutcome Fail(string message) => new(false, message);
}

