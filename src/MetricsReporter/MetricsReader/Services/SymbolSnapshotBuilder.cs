namespace MetricsReporter.MetricsReader.Services;

using MetricsReporter.Model;

/// <summary>
/// Builds symbol metric snapshots from metrics nodes.
/// </summary>
internal sealed class SymbolSnapshotBuilder : ISymbolSnapshotBuilder
{
  private readonly IMetricsThresholdProvider _thresholdProvider;
  private readonly ISuppressedSymbolChecker _suppressedSymbolChecker;

  /// <summary>
  /// Initializes a new instance of the <see cref="SymbolSnapshotBuilder"/> class.
  /// </summary>
  /// <param name="thresholdProvider">The threshold provider to use.</param>
  /// <param name="suppressedSymbolChecker">The suppressed symbol checker to use.</param>
  public SymbolSnapshotBuilder(
    IMetricsThresholdProvider thresholdProvider,
    ISuppressedSymbolChecker suppressedSymbolChecker)
  {
    _thresholdProvider = thresholdProvider ?? throw new System.ArgumentNullException(nameof(thresholdProvider));
    _suppressedSymbolChecker = suppressedSymbolChecker ?? throw new System.ArgumentNullException(nameof(suppressedSymbolChecker));
  }

  /// <inheritdoc/>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Snapshot builder method creates snapshots from metrics nodes by accessing node properties, threshold provider, and suppressed symbol checker; dependencies on model types are necessary for snapshot construction.")]
  public SymbolMetricSnapshot? BuildSnapshot(MetricsNode node, MetricIdentifier metric)
  {
    if (!node.Metrics.TryGetValue(metric, out var metricValue) || metricValue is null || metricValue.Value is null)
    {
      return null;
    }

    var level = MapLevel(node.Kind);
    if (level is null)
    {
      return null;
    }

    var threshold = _thresholdProvider.GetThreshold(metric, level.Value);
    var isSuppressed = _suppressedSymbolChecker.IsSuppressed(node.FullyQualifiedName, metric);
    return new SymbolMetricSnapshot(
      node.FullyQualifiedName ?? string.Empty,
      node.Kind,
      node.Source?.Path,
      metric,
      metricValue,
      threshold,
      isSuppressed);
  }

  private static MetricSymbolLevel? MapLevel(CodeElementKind kind)
    => kind switch
    {
      CodeElementKind.Type => MetricSymbolLevel.Type,
      CodeElementKind.Member => MetricSymbolLevel.Member,
      _ => null
    };
}


