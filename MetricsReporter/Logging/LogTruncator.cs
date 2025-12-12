namespace MetricsReporter.Logging;

/// <summary>
/// Provides consistent truncation for large stdout/stderr buffers in logs.
/// </summary>
internal static class LogTruncator
{
  public const int DefaultLimit = 4000;

  public static string Truncate(string value, int limit = DefaultLimit)
  {
    if (string.IsNullOrEmpty(value))
    {
      return string.Empty;
    }

    if (limit <= 0 || value.Length <= limit)
    {
      return value;
    }

    return value[..limit] + "...";
  }
}

