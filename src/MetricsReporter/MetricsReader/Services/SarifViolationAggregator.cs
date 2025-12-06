namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Aggregates SARIF violations from metrics nodes into grouped results.
/// </summary>
internal sealed class SarifViolationAggregator : ISarifViolationAggregator
{
  private readonly ISuppressedSymbolChecker _suppressedSymbolChecker;

  /// <summary>
  /// Initializes a new instance of the <see cref="SarifViolationAggregator"/> class.
  /// </summary>
  /// <param name="suppressedSymbolChecker">The suppressed symbol checker to use.</param>
  public SarifViolationAggregator(ISuppressedSymbolChecker suppressedSymbolChecker)
  {
    _suppressedSymbolChecker = suppressedSymbolChecker ?? throw new System.ArgumentNullException(nameof(suppressedSymbolChecker));
  }

  /// <inheritdoc/>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Aggregation method processes metrics nodes and breakdown entries to build violation groups; dependencies on model types (MetricsNode, SymbolFilter, RuleDescription) and builder are necessary for the aggregation logic.")]
  public Dictionary<string, SarifViolationGroupBuilder> AggregateViolations(
    IEnumerable<MetricsNode> nodes,
    SymbolFilter filter,
    IReadOnlyDictionary<string, RuleDescription>? ruleDescriptions)
  {
    var groups = new Dictionary<string, SarifViolationGroupBuilder>(System.StringComparer.OrdinalIgnoreCase);

    foreach (var node in nodes)
    {
      if (!node.Metrics.TryGetValue(filter.Metric, out var metricValue) || metricValue is null)
      {
        continue;
      }

      if (metricValue.Breakdown is null || metricValue.Breakdown.Count == 0)
      {
        continue;
      }

      ProcessBreakdownEntries(node, filter, metricValue.Breakdown, groups, ruleDescriptions);
    }

    return groups;
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Helper method processes breakdown entries from metrics nodes to build violation groups; dependencies on model types and builder are necessary for the processing logic.")]
  private void ProcessBreakdownEntries(
    MetricsNode node,
    SymbolFilter filter,
    IReadOnlyDictionary<string, SarifRuleBreakdownEntry> breakdown,
    Dictionary<string, SarifViolationGroupBuilder> groups,
    IReadOnlyDictionary<string, RuleDescription>? ruleDescriptions)
  {
    foreach (var pair in breakdown)
    {
      var entry = pair.Value;
      if (entry is null)
      {
        continue;
      }

      if (!filter.IncludeSuppressed
          && _suppressedSymbolChecker.IsSuppressed(node.FullyQualifiedName, filter.Metric, pair.Key))
      {
        continue;
      }

      var builder = GetOrCreateBuilder(groups, pair.Key, ruleDescriptions, filter.Metric);
      builder.Add(entry.Count, entry.Violations, node);
    }
  }

  private static SarifViolationGroupBuilder GetOrCreateBuilder(
    Dictionary<string, SarifViolationGroupBuilder> groups,
    string ruleId,
    IReadOnlyDictionary<string, RuleDescription>? ruleDescriptions,
    MetricIdentifier metric)
  {
    if (groups.TryGetValue(ruleId, out var builder))
    {
      return builder;
    }

    RuleDescription? description = null;
    ruleDescriptions?.TryGetValue(ruleId, out description);
    builder = new SarifViolationGroupBuilder(ruleId, description?.ShortDescription, metric);
    groups[ruleId] = builder;
    return builder;
  }
}

