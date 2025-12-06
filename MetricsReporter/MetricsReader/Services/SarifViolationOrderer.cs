namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Orders SARIF violation groups by count and rule ID.
/// </summary>
internal sealed class SarifViolationOrderer : ISarifViolationOrderer
{
  /// <inheritdoc/>
  public IReadOnlyList<SarifViolationGroup> OrderGroups(IEnumerable<SarifViolationGroupBuilder> groups)
  {
    return groups
      .Select(builder => builder.Build())
      .OrderByDescending(group => group.Count)
      .ThenBy(group => group.RuleId, System.StringComparer.OrdinalIgnoreCase)
      .ToList();
  }
}


