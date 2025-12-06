namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;

/// <summary>
/// Orders SARIF violation groups for output.
/// </summary>
internal interface ISarifViolationOrderer
{
  /// <summary>
  /// Orders violation groups by count (descending) and then by rule ID (ascending).
  /// </summary>
  /// <param name="groups">The groups to order.</param>
  /// <returns>An ordered list of violation groups.</returns>
  IReadOnlyList<SarifViolationGroup> OrderGroups(IEnumerable<SarifViolationGroupBuilder> groups);
}


