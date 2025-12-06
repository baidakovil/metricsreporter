namespace MetricsReporter.MetricsReader.Services;

using System;
using System.Collections.Generic;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Aggregates SARIF violation groups from multiple metrics.
/// </summary>
internal sealed class SarifGroupAggregator : ISarifGroupAggregator
{
  /// <inheritdoc/>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Aggregation method creates SymbolFilter and delegates to engine; dependencies on model types are necessary for aggregation logic. Further decomposition would fragment the logic without benefit.")]
  public List<SarifViolationGroup> AggregateGroups(
    MetricsReaderEngine engine,
    string @namespace,
    IEnumerable<MetricIdentifier> metrics,
    MetricsReaderSymbolKind symbolKind,
    bool includeSuppressed)
  {
    ArgumentNullException.ThrowIfNull(engine);
    ArgumentNullException.ThrowIfNull(metrics);
    if (string.IsNullOrWhiteSpace(@namespace))
    {
      throw new ArgumentException("Namespace cannot be null or empty.", nameof(@namespace));
    }

    var aggregatedGroups = new List<SarifViolationGroup>();
    foreach (var metric in metrics)
    {
      var filter = new SymbolFilter(@namespace, metric, symbolKind, includeSuppressed);
      var aggregation = engine.GetSarifViolationGroups(filter);
      aggregatedGroups.AddRange(aggregation.Groups);
    }

    return aggregatedGroups;
  }
}


