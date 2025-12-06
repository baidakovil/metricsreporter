namespace MetricsReporter.MetricsReader.Services;
using System.Collections.Generic;
/// <summary>
/// Sorts SARIF violation groups according to specified criteria.
/// </summary>
internal interface ISarifGroupSorter
{
  /// <summary>
  /// Sorts violation groups by count (descending) and rule ID (ascending, case-insensitive).
  /// </summary>
  /// <param name="groups">The groups to sort.</param>
  /// <returns>A sorted list of groups.</returns>
  List<SarifViolationGroup> SortByCountAndRuleId(IEnumerable<SarifViolationGroup> groups);
}






