namespace MetricsReporter.MetricsReader.Services;
using System;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// Sorts SARIF violation groups according to specified criteria.
/// </summary>
internal sealed class SarifGroupSorter : ISarifGroupSorter
{
  /// <inheritdoc/>
  public List<SarifViolationGroup> SortByCountAndRuleId(IEnumerable<SarifViolationGroup> groups)
  {
    ArgumentNullException.ThrowIfNull(groups);
    return groups
      .OrderByDescending(group => group.Count)
      .ThenBy(group => group.RuleId, StringComparer.OrdinalIgnoreCase)
      .ToList();
  }
}






