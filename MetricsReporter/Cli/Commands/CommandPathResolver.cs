using System;
using System.IO;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Resolves command paths by applying fallback precedence and absolute normalization.
/// </summary>
internal static class CommandPathResolver
{
  /// <summary>
  /// Returns the first non-empty value in the provided list.
  /// </summary>
  /// <param name="values">Candidate values ordered by precedence.</param>
  /// <returns>The first non-empty value or <see langword="null"/>.</returns>
  public static string? FirstNonEmpty(params string?[] values)
  {
    foreach (var value in values)
    {
      if (!string.IsNullOrWhiteSpace(value))
      {
        return value;
      }
    }

    return null;
  }

  /// <summary>
  /// Converts a path to an absolute path using the provided working directory when necessary.
  /// </summary>
  /// <param name="path">Path to convert.</param>
  /// <param name="workingDirectory">Working directory used when the path is relative.</param>
  /// <returns>Absolute path or <see langword="null"/> when the input is empty.</returns>
  public static string? MakeAbsolute(string? path, string workingDirectory)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      return null;
    }

    return Path.IsPathRooted(path)
      ? Path.GetFullPath(path)
      : Path.GetFullPath(Path.Combine(workingDirectory, path));
  }
}

