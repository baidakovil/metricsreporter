namespace MetricsReporter.Tests.TestHelpers;

using System;
using System.Collections.Generic;
using MetricsReporter.Model;

internal static class SarifBreakdownTestHelper
{
  public static Dictionary<string, SarifRuleBreakdownEntry> Create(params (string RuleId, int Count)[] entries)
  {
    var result = new Dictionary<string, SarifRuleBreakdownEntry>(StringComparer.Ordinal);
    foreach (var entry in entries)
    {
      result[entry.RuleId] = new SarifRuleBreakdownEntry
      {
        Count = entry.Count,
        Violations = CreateViolations(entry.RuleId, entry.Count)
      };
    }

    return result;
  }

  public static Dictionary<string, SarifRuleBreakdownEntry> Single(string ruleId)
    => Create((ruleId, 1));

  public static Dictionary<string, SarifRuleBreakdownEntry> Empty()
    => new(StringComparer.Ordinal);

  private static List<SarifRuleViolationDetail> CreateViolations(string ruleId, int count)
  {
    if (count <= 0)
    {
      return new List<SarifRuleViolationDetail>();
    }

    var violations = new List<SarifRuleViolationDetail>(count);
    for (var i = 0; i < count; i++)
    {
      violations.Add(new SarifRuleViolationDetail
      {
        Message = $"Violation {i + 1} for {ruleId}",
        Uri = $"file:///test/{ruleId}/{i + 1}.cs",
        StartLine = 10 + i,
        EndLine = 10 + i
      });
    }

    return violations;
  }
}



