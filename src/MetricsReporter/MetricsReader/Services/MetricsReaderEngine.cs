namespace MetricsReporter.MetricsReader.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Provides high-level queries over MetricsReport.g.json for CLI commands.
/// </summary>
internal sealed class MetricsReaderEngine
{
  private readonly IMetricsNodeEnumerator _nodeEnumerator;
  private readonly ISymbolSnapshotBuilder _snapshotBuilder;
  private readonly ISarifViolationAggregator _violationAggregator;
  private readonly ISarifViolationOrderer _violationOrderer;
  private readonly MetricsReport _report;

  /// <summary>
  /// Initializes a new instance of the <see cref="MetricsReaderEngine"/> class.
  /// </summary>
  /// <param name="nodeEnumerator">The node enumerator to use.</param>
  /// <param name="snapshotBuilder">The snapshot builder to use.</param>
  /// <param name="violationAggregator">The violation aggregator to use.</param>
  /// <param name="violationOrderer">The violation orderer to use.</param>
  /// <param name="report">The metrics report to query.</param>
  public MetricsReaderEngine(
    IMetricsNodeEnumerator nodeEnumerator,
    ISymbolSnapshotBuilder snapshotBuilder,
    ISarifViolationAggregator violationAggregator,
    ISarifViolationOrderer violationOrderer,
    MetricsReport report)
  {
    _nodeEnumerator = nodeEnumerator ?? throw new ArgumentNullException(nameof(nodeEnumerator));
    _snapshotBuilder = snapshotBuilder ?? throw new ArgumentNullException(nameof(snapshotBuilder));
    _violationAggregator = violationAggregator ?? throw new ArgumentNullException(nameof(violationAggregator));
    _violationOrderer = violationOrderer ?? throw new ArgumentNullException(nameof(violationOrderer));
    _report = report ?? throw new ArgumentNullException(nameof(report));
  }

  /// <summary>
  /// Gets problematic symbols that exceed thresholds.
  /// </summary>
  /// <param name="filter">The filter to apply.</param>
  /// <returns>An enumeration of problematic symbol snapshots.</returns>
  public IEnumerable<SymbolMetricSnapshot> GetProblematicSymbols(SymbolFilter filter)
    => EnumerateSymbols(filter)
      .Where(snapshot => snapshot.Status == ThresholdStatus.Warning || snapshot.Status == ThresholdStatus.Error)
      .Where(snapshot => filter.IncludeSuppressed || !snapshot.IsSuppressed);

  /// <summary>
  /// Tries to get a symbol snapshot by fully qualified name.
  /// </summary>
  /// <param name="fullyQualifiedName">The fully qualified name of the symbol.</param>
  /// <param name="metric">The metric identifier.</param>
  /// <returns>A snapshot if found; otherwise, <see langword="null"/>.</returns>
  public SymbolMetricSnapshot? TryGetSymbol(string fullyQualifiedName, MetricIdentifier metric)
  {
    foreach (var type in _nodeEnumerator.EnumerateTypeNodes())
    {
      if (string.Equals(type.FullyQualifiedName, fullyQualifiedName, StringComparison.Ordinal))
      {
        return _snapshotBuilder.BuildSnapshot(type, metric);
      }

      foreach (var member in type.Members)
      {
        if (string.Equals(member.FullyQualifiedName, fullyQualifiedName, StringComparison.Ordinal))
        {
          return _snapshotBuilder.BuildSnapshot(member, metric);
        }
      }
    }

    return null;
  }

  /// <summary>
  /// Gets SARIF violation groups aggregated by rule ID.
  /// </summary>
  /// <param name="filter">The filter to apply.</param>
  /// <returns>An aggregation result containing ordered violation groups.</returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Method orchestrates violation aggregation by coordinating specialized services (aggregator, orderer, node enumerator) and accessing report metadata; further decomposition would fragment the coordination logic without meaningful architectural benefit.")]
  public SarifViolationAggregationResult GetSarifViolationGroups(SymbolFilter filter)
  {
    var nodes = _nodeEnumerator.EnumerateNodes(filter);
    var ruleDescriptions = ExtractRuleDescriptions();

    var groups = _violationAggregator.AggregateViolations(nodes, filter, ruleDescriptions);
    var ordered = _violationOrderer.OrderGroups(groups.Values);

    return new SarifViolationAggregationResult(ordered);
  }

  private IReadOnlyDictionary<string, RuleDescription>? ExtractRuleDescriptions()
  {
    var ruleDescriptionsDict = _report.Metadata.RuleDescriptions;
    return ruleDescriptionsDict is null ? null : (IReadOnlyDictionary<string, RuleDescription>?)ruleDescriptionsDict;
  }

  private IEnumerable<SymbolMetricSnapshot> EnumerateSymbols(SymbolFilter filter)
  {
    return filter.SymbolKind switch
    {
      MetricsReaderSymbolKind.Type => EnumerateTypeSnapshots(filter),
      MetricsReaderSymbolKind.Member => EnumerateMemberSnapshots(filter),
      MetricsReaderSymbolKind.Any => EnumerateTypeSnapshots(filter).Concat(EnumerateMemberSnapshots(filter)),
      _ => Enumerable.Empty<SymbolMetricSnapshot>()
    };
  }

  private IEnumerable<SymbolMetricSnapshot> EnumerateTypeSnapshots(SymbolFilter filter)
    => _nodeEnumerator.EnumerateTypeNodes()
      .Where(type => NamespaceMatcher.Matches(type.FullyQualifiedName, filter.Namespace))
      .Select(node => _snapshotBuilder.BuildSnapshot(node, filter.Metric))
      .Where(snapshot => snapshot is not null)
      .Select(snapshot => snapshot!);

  private IEnumerable<SymbolMetricSnapshot> EnumerateMemberSnapshots(SymbolFilter filter)
    => _nodeEnumerator.EnumerateMemberNodes()
      .Where(member => NamespaceMatcher.Matches(member.FullyQualifiedName, filter.Namespace))
      .Select(node => _snapshotBuilder.BuildSnapshot(node, filter.Metric))
      .Where(snapshot => snapshot is not null)
      .Select(snapshot => snapshot!);
}

internal sealed record SymbolMetricSnapshot(
  string Symbol,
  CodeElementKind Kind,
  string? FilePath,
  MetricIdentifier Metric,
  MetricValue MetricValue,
  MetricThreshold? Threshold,
  bool IsSuppressed)
{
  public ThresholdStatus Status => MetricValue.Status;

  public decimal? Value => MetricValue.Value;

  public decimal? Delta => MetricValue.Delta;

  public string SymbolType => Kind.ToString();

  public string ThresholdKind => Status switch
  {
    ThresholdStatus.Error => "Error",
    ThresholdStatus.Warning => "Warning",
    _ => "None"
  };

  public decimal? ThresholdValue => Status switch
  {
    ThresholdStatus.Error => Threshold?.Error,
    ThresholdStatus.Warning => Threshold?.Warning,
    _ => null
  };

  public decimal? Magnitude
  {
    get
    {
      if (ThresholdValue is null || Value is null || Threshold is null)
      {
        return null;
      }

      var delta = Threshold.HigherIsBetter
        ? ThresholdValue.Value - Value.Value
        : Value.Value - ThresholdValue.Value;

      return Math.Abs(delta);
    }
  }
}

internal sealed record SarifViolationAggregationResult(
  IReadOnlyList<SarifViolationGroup> Groups);

internal sealed record SarifViolationGroup(
  string RuleId,
  string? ShortDescription,
  MetricIdentifier Metric,
  int Count,
  IReadOnlyList<SarifViolationRecord> Violations,
  IReadOnlyList<SarifSymbolContribution> SymbolContributions);

internal sealed record SarifViolationRecord(
  string Symbol,
  string? Message,
  string? Uri,
  int? StartLine,
  int? EndLine);

