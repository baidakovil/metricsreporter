namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MetricsReporter.Model;

/// <summary>
/// Provides helper methods for cloning and merging SARIF rule breakdown dictionaries.
/// </summary>
internal static class SarifBreakdownHelper
{
  /// <summary>
  /// Creates a deep copy of the supplied breakdown dictionary.
  /// </summary>
  public static Dictionary<string, SarifRuleBreakdownEntry>? Clone(Dictionary<string, SarifRuleBreakdownEntry>? source)
  {
    if (source is null || source.Count == 0)
    {
      return null;
    }

    var clone = new Dictionary<string, SarifRuleBreakdownEntry>(source.Count, StringComparer.Ordinal);
    foreach (var pair in source)
    {
      clone[pair.Key] = CloneEntry(pair.Value);
    }

    return clone;
  }

  /// <summary>
  /// Merges two breakdown dictionaries by summing counts and concatenating violation details.
  /// </summary>
  public static Dictionary<string, SarifRuleBreakdownEntry>? Merge(
      Dictionary<string, SarifRuleBreakdownEntry>? existing,
      Dictionary<string, SarifRuleBreakdownEntry>? incoming)
  {
    if (incoming is null || incoming.Count == 0)
    {
      return Clone(existing);
    }

    if (existing is null || existing.Count == 0)
    {
      return Clone(incoming);
    }

    var merged = Clone(existing)!;
    foreach (var pair in incoming)
    {
      if (pair.Value is null)
      {
        continue;
      }

      if (!merged.TryGetValue(pair.Key, out var entry))
      {
        merged[pair.Key] = CloneEntry(pair.Value);
        continue;
      }

      entry.Count += pair.Value.Count;
      if (pair.Value.Violations.Count > 0)
      {
        entry.Violations.AddRange(CloneViolations(pair.Value.Violations));
      }
    }

    return merged;
  }

  [SuppressMessage(
      "Style",
      "IDE0028:Collection initialization can be simplified",
      Justification = "We want to keep the ternary explicit so the entry always builds with or without violation details.")]
  private static SarifRuleBreakdownEntry CloneEntry(SarifRuleBreakdownEntry? source)
  {
    if (source is null)
    {
      return new SarifRuleBreakdownEntry();
    }

    var entry = new SarifRuleBreakdownEntry
    {
      Count = source.Count
    };

    if (source.Violations.Count > 0)
    {
      entry.Violations.AddRange(CloneViolations(source.Violations));
    }

    return entry;
  }

  private static List<SarifRuleViolationDetail> CloneViolations(List<SarifRuleViolationDetail> source)
  {
    var result = new List<SarifRuleViolationDetail>(source.Count);
    foreach (var violation in source)
    {
      result.Add(new SarifRuleViolationDetail
      {
        Message = violation.Message,
        Uri = violation.Uri,
        StartLine = violation.StartLine,
        EndLine = violation.EndLine
      });
    }

    return result;
  }
}



