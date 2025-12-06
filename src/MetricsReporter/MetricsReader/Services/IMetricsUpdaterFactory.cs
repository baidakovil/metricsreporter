namespace MetricsReporter.MetricsReader.Services;

/// <summary>
/// Factory for creating IMetricsUpdater instances.
/// </summary>
internal interface IMetricsUpdaterFactory
{
  /// <summary>
  /// Creates an IMetricsUpdater instance for the given solution path.
  /// </summary>
  /// <param name="solutionPath">The path to the solution file.</param>
  /// <returns>An IMetricsUpdater instance.</returns>
  IMetricsUpdater Create(string solutionPath);
}


