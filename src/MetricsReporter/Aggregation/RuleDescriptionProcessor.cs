namespace MetricsReporter.Aggregation;
using System;
using System.Collections.Generic;
using MetricsReporter.Model;
using MetricsReporter.Processing;
/// <summary>
/// Processes rule descriptions from SARIF documents by merging and filtering them.
/// </summary>
internal static class RuleDescriptionProcessor
{
  /// <summary>
  /// Merges rule descriptions from all SARIF documents and filters them to only include used rules.
  /// </summary>
  /// <param name="sarifDocuments">The SARIF documents to merge rule descriptions from.</param>
  /// <param name="usedRuleIds">Optional set of rule IDs that are actually used in breakdown. If provided, only these rules will be included.</param>
  /// <returns>A dictionary of merged and filtered rule descriptions keyed by rule ID.</returns>
  public static Dictionary<string, RuleDescription> Process(
      IList<ParsedMetricsDocument> sarifDocuments,
      HashSet<string>? usedRuleIds = null)
  {
    var allRuleDescriptions = Merge(sarifDocuments);
    if (usedRuleIds is not null)
    {
      return Filter(allRuleDescriptions, usedRuleIds);
    }
    return allRuleDescriptions;
  }
  /// <summary>
  /// Merges rule descriptions from all SARIF documents, detecting and warning about conflicts.
  /// </summary>
  /// <param name="sarifDocuments">The SARIF documents to merge rule descriptions from.</param>
  /// <returns>A dictionary of merged rule descriptions keyed by rule ID.</returns>
  private static Dictionary<string, RuleDescription> Merge(IList<ParsedMetricsDocument> sarifDocuments)
  {
    var merged = new Dictionary<string, RuleDescription>();
    foreach (var document in sarifDocuments)
    {
      foreach (var (ruleId, description) in document.RuleDescriptions)
      {
        if (merged.TryGetValue(ruleId, out var existing))
        {
          // Check for differences and warn if found
          if (!AreEqual(existing, description))
          {
            Console.Error.WriteLine(
                $"WARNING: Rule {ruleId} has different descriptions across SARIF files. " +
                $"Using first encountered description. " +
                $"Existing: Short='{existing.ShortDescription}', " +
                $"Incoming: Short='{description.ShortDescription}'");
          }
        }
        else
        {
          merged[ruleId] = description;
        }
      }
    }
    return merged;
  }
  /// <summary>
  /// Filters rule descriptions to only include rules that are actually used in breakdown.
  /// </summary>
  /// <param name="allRuleDescriptions">All rule descriptions from SARIF files.</param>
  /// <param name="usedRuleIds">Set of rule IDs that are actually used in breakdown.</param>
  /// <returns>A filtered dictionary containing only used rule descriptions.</returns>
  private static Dictionary<string, RuleDescription> Filter(
      Dictionary<string, RuleDescription> allRuleDescriptions,
      HashSet<string> usedRuleIds)
  {
    var filtered = new Dictionary<string, RuleDescription>();
    foreach (var (ruleId, description) in allRuleDescriptions)
    {
      if (usedRuleIds.Contains(ruleId))
      {
        filtered[ruleId] = description;
      }
    }
    return filtered;
  }
  /// <summary>
  /// Compares two rule descriptions for equality.
  /// </summary>
  /// <param name="first">The first rule description.</param>
  /// <param name="second">The second rule description.</param>
  /// <returns><see langword="true"/> if the descriptions are equal; otherwise, <see langword="false"/>.</returns>
  private static bool AreEqual(RuleDescription first, RuleDescription second)
  {
    return string.Equals(first.ShortDescription, second.ShortDescription, StringComparison.Ordinal)
        && string.Equals(first.FullDescription ?? string.Empty, second.FullDescription ?? string.Empty, StringComparison.Ordinal)
        && string.Equals(first.HelpUri ?? string.Empty, second.HelpUri ?? string.Empty, StringComparison.Ordinal)
        && string.Equals(first.Category ?? string.Empty, second.Category ?? string.Empty, StringComparison.Ordinal);
  }
}






