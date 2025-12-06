namespace MetricsReporter.MetricsReader.Services;

/// <summary>
/// Factory for creating MetricsUpdater instances.
/// </summary>
internal sealed class MetricsUpdaterFactory : IMetricsUpdaterFactory
{
  /// <inheritdoc/>
  public IMetricsUpdater Create(string solutionPath)
    => new MetricsUpdater(solutionPath);
}


