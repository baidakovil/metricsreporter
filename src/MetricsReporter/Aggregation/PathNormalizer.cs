namespace MetricsReporter.Aggregation;

using System;

/// <summary>
/// Provides shared path normalization utilities for the aggregation workspace.
/// </summary>
internal static class PathNormalizer
{
  /// <summary>
  /// Normalizes file paths to use backslashes, trimmed whitespace, and upper-case drive letters.
  /// </summary>
  public static string Normalize(string path)
  {
    ArgumentNullException.ThrowIfNull(path);
    return path.Replace('/', '\\').Trim().ToUpperInvariant();
  }
}


