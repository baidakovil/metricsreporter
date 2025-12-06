namespace MetricsReporter.MetricsReader.Services;
using System.Collections.Generic;
/// <summary>
/// Filters SARIF violation groups based on criteria.
/// </summary>
internal interface ISarifGroupFilter
{
  /// <summary>
  /// Filters violation groups by rule ID.
  /// </summary>
  /// <param name="groups">The groups to filter.</param>
  /// <param name="ruleId">Optional rule ID filter (case-insensitive).</param>
  /// <returns>A filtered list of groups.</returns>
  List<SarifViolationGroup> Filter(List<SarifViolationGroup> groups, string? ruleId);
}

