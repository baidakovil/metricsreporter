namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Aggregates SARIF violations from metrics nodes into grouped results.
/// </summary>
internal interface ISarifViolationAggregator
{
  /// <summary>
  /// Aggregates SARIF violations from the provided nodes into groups by rule ID.
  /// </summary>
  /// <param name="nodes">The metrics nodes to process.</param>
  /// <param name="filter">The filter to apply when processing nodes.</param>
  /// <param name="ruleDescriptions">Optional rule descriptions keyed by rule ID.</param>
  /// <returns>A dictionary of rule ID to violation group builder.</returns>
  Dictionary<string, SarifViolationGroupBuilder> AggregateViolations(
    IEnumerable<MetricsNode> nodes,
    SymbolFilter filter,
    IReadOnlyDictionary<string, RuleDescription>? ruleDescriptions);
}


