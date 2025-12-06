namespace MetricsReporter.MetricsReader.Services;

using MetricsReporter.Model;

/// <summary>
/// Represents a pre-loaded metrics report and helper services.
/// </summary>
internal sealed class MetricsReaderContext
{
  public MetricsReaderContext(
    MetricsReport report,
    MetricsThresholdProvider thresholdProvider,
    SuppressedSymbolIndex suppressedSymbolIndex,
    bool includeSuppressed)
  {
    Report = report;
    ThresholdProvider = thresholdProvider;
    SuppressedSymbolIndex = suppressedSymbolIndex;
    IncludeSuppressed = includeSuppressed;
  }

  public MetricsReport Report { get; }

  public MetricsThresholdProvider ThresholdProvider { get; }

  public SuppressedSymbolIndex SuppressedSymbolIndex { get; }

  public bool IncludeSuppressed { get; }
}


