namespace MetricsReporter.MetricsReader.Services;
using System;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// Filters SARIF violation groups based on criteria.
/// </summary>
internal sealed class SarifGroupFilter : ISarifGroupFilter
{
  /// <inheritdoc/>
  public List<SarifViolationGroup> Filter(List<SarifViolationGroup> groups, string? ruleId)
  {
    ArgumentNullException.ThrowIfNull(groups);
    if (string.IsNullOrWhiteSpace(ruleId))
    {
      return groups;
    }

    return groups
      .Where(group => string.Equals(group.RuleId, ruleId, StringComparison.OrdinalIgnoreCase))
      .ToList();
  }
}

