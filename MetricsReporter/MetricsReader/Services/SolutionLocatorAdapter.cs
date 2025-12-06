namespace MetricsReporter.MetricsReader.Services;

/// <summary>
/// Adapter that wraps the static SolutionLocator to implement ISolutionLocator interface.
/// </summary>
internal sealed class SolutionLocatorAdapter : ISolutionLocator
{
  /// <inheritdoc/>
  public string FindSolutionPath(string reportPath)
    => SolutionLocator.FindSolutionPath(reportPath);
}


