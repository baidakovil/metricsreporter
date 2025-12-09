namespace MetricsReporter.MetricsReader.Services;

using System;
using System.IO;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Locates the solution file associated with a metrics report path.
/// </summary>
internal static class SolutionLocator
{
  public static string FindSolutionPath(string reportPath)
  {
    var directory = GetStartingDirectory(reportPath);
    while (directory is not null)
    {
      var solution = TryResolveSolution(directory.FullName);
      if (solution is not null)
      {
        return solution;
      }

      directory = directory.Parent;
    }

    throw new InvalidOperationException($"Failed to discover a solution file while walking up from '{reportPath}'.");
  }

  [SuppressMessage(
    "AltCoverBranchCoverage",
    "AltCoverBranchCoverage",
    Justification = "IO-walking helper; branch coverage retained but flagged as suppressed to avoid noise.")]
  [SuppressMessage(
    "AltCoverSequenceCoverage",
    "AltCoverSequenceCoverage",
    Justification = "IO-walking helper; sequence coverage is retained but flagged as suppressed to avoid noise.")]
  private static DirectoryInfo? GetStartingDirectory(string path)
  {
    var fullPath = Path.GetFullPath(path);

    // If it's an existing file, start from its directory
    if (File.Exists(fullPath))
    {
      return new DirectoryInfo(Path.GetDirectoryName(fullPath)!);
    }

    // If it's an existing directory, start there
    if (Directory.Exists(fullPath))
    {
      return new DirectoryInfo(fullPath);
    }

    // Otherwise, walk up until we find an existing directory
    var current = fullPath;
    while (current != null && !Directory.Exists(current))
    {
      current = Path.GetDirectoryName(current);
    }

    return current != null ? new DirectoryInfo(current) : null;
  }

  private static string? TryResolveSolution(string directory)
  {
    var solutions = Directory.GetFiles(directory, "*.sln");
    if (solutions.Length == 0)
    {
      return null;
    }

    var preferred = solutions.FirstOrDefault(s => string.Equals(Path.GetFileName(s), "rca-plugin.sln", StringComparison.OrdinalIgnoreCase));
    return preferred ?? solutions[0];
  }
}


