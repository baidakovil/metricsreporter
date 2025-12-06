namespace MetricsReporter.Processing;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Processes source code folders and enumerates C# files with assembly name resolution.
/// </summary>
/// <remarks>
/// This class encapsulates the logic for normalizing source code folder paths,
/// enumerating C# files, and resolving assembly names from file paths.
/// This reduces coupling in the main analyzer class.
/// </remarks>
internal static class SourceCodeFolderProcessor
{
  /// <summary>
  /// Normalizes and sorts source code folder paths for longest-prefix matching.
  /// </summary>
  /// <param name="sourceCodeFolders">The source code folder paths to normalize.</param>
  /// <returns>
  /// An array of normalized folder paths, sorted by length (longest first).
  /// If the input is empty, returns an array with a single empty string.
  /// </returns>
  public static string[] NormalizeAndSortFolders(IReadOnlyCollection<string> sourceCodeFolders)
  {
    // Normalize source code folders: sort by length (longest first) for longest-prefix matching
    var normalizedFolders = sourceCodeFolders
      .Where(f => !string.IsNullOrWhiteSpace(f))
      .Select(f => NormalizePath(f))
      .OrderByDescending(f => f.Length)
      .ToArray();

    if (normalizedFolders.Length == 0)
    {
      // If no source code folders specified, fall back to scanning everything
      // (backward compatibility, though not recommended)
      normalizedFolders = new[] { string.Empty };
    }

    return normalizedFolders;
  }

  /// <summary>
  /// Enumerates all C# files under the specified source code folders.
  /// </summary>
  /// <param name="solutionDirectory">The root directory of the solution.</param>
  /// <param name="normalizedFolders">The normalized source code folder paths.</param>
  /// <returns>
  /// An enumerable collection of full paths to C# files.
  /// </returns>
  public static IEnumerable<string> EnumerateCSharpFiles(string solutionDirectory, string[] normalizedFolders)
  {
    if (!Directory.Exists(solutionDirectory))
    {
      return Array.Empty<string>();
    }

    var allFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var folder in normalizedFolders)
    {
      var folderPath = string.IsNullOrEmpty(folder)
        ? solutionDirectory
        : Path.Combine(solutionDirectory, folder);

      if (!Directory.Exists(folderPath))
      {
        continue;
      }

      var files = Directory.EnumerateFiles(folderPath, "*.cs", SearchOption.AllDirectories);
      foreach (var file in files)
      {
        allFiles.Add(file);
      }
    }

    return allFiles;
  }

  /// <summary>
  /// Attempts to resolve an assembly name from a file path using longest-prefix matching.
  /// </summary>
  /// <param name="solutionDirectory">The root directory of the solution.</param>
  /// <param name="filePath">The full path to the C# file.</param>
  /// <param name="normalizedFolders">The normalized source code folder paths (sorted by length, longest first).</param>
  /// <returns>
  /// The resolved assembly name, or <see langword="null"/> if resolution fails.
  /// </returns>
  public static string? TryResolveAssemblyName(
      string solutionDirectory,
      string filePath,
      string[] normalizedFolders)
  {
    var relative = Path.GetRelativePath(solutionDirectory, filePath);
    var normalizedRelative = NormalizePath(relative);
    var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    // Find the longest matching source code folder prefix
    string? matchedPrefix = null;
    foreach (var folder in normalizedFolders)
    {
      if (string.IsNullOrEmpty(folder))
      {
        continue;
      }

      var normalizedFolder = NormalizePath(folder);
      if (normalizedRelative.StartsWith(normalizedFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
          normalizedRelative.Equals(normalizedFolder, StringComparison.OrdinalIgnoreCase))
      {
        // Prefer longer matches (folders are already sorted by length descending)
        if (matchedPrefix is null || normalizedFolder.Length > matchedPrefix.Length)
        {
          matchedPrefix = normalizedFolder;
        }
      }
    }

    if (matchedPrefix is null)
    {
      // No source code folder matched - treat first segment as assembly name
      var segments = normalizedRelative.Split(separators, StringSplitOptions.RemoveEmptyEntries);
      return segments.Length > 0 ? segments[0] : null;
    }

    // Remove the matched prefix and take the next segment as assembly name
    var remaining = normalizedRelative.Substring(matchedPrefix.Length).Trim(separators);
    var remainingSegments = remaining.Split(separators, StringSplitOptions.RemoveEmptyEntries);
    return remainingSegments.Length > 0 ? remainingSegments[0] : null;
  }

  /// <summary>
  /// Normalizes path separators and removes leading/trailing separators.
  /// </summary>
  /// <param name="path">The path to normalize.</param>
  /// <returns>The normalized path.</returns>
  private static string NormalizePath(string path)
  {
    // Normalize path separators and remove leading/trailing separators
    return path.Replace('\\', Path.DirectorySeparatorChar)
               .Replace('/', Path.DirectorySeparatorChar)
               .Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
  }
}


