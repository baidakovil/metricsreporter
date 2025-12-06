namespace MetricsReporter.MetricsReader.Services;

/// <summary>
/// Locates solution files in the file system.
/// </summary>
internal interface ISolutionLocator
{
  /// <summary>
  /// Finds the solution file associated with a given path by walking up the directory tree.
  /// </summary>
  /// <param name="reportPath">The path to start searching from.</param>
  /// <returns>The full path to the solution file.</returns>
  string FindSolutionPath(string reportPath);
}


