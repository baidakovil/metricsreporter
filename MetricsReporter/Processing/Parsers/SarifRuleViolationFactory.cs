namespace MetricsReporter.Processing.Parsers;

using System;
using System.Collections.Generic;
using MetricsReporter.Model;
using MetricsReporter.Processing;

/// <summary>
/// Builds parsed code elements for SARIF rule violations while keeping SarifMetricsParser lean.
/// </summary>
internal static class SarifRuleViolationFactory
{
  internal static ParsedCodeElement CreateCodeElement(
      string ruleId,
      MetricIdentifier identifier,
      SarifLocation location,
      string? messageText)
  {
    return new ParsedCodeElement(CodeElementKind.Member, ruleId, null)
    {
      Metrics = CreateMetricDictionary(identifier, CreateRuleBreakdown(ruleId, location, messageText)),
      Source = location.Source,
      MemberKind = MemberKind.Unknown,
      HasSarifViolations = true
    };
  }

  private static Dictionary<MetricIdentifier, MetricValue> CreateMetricDictionary(
      MetricIdentifier identifier,
      Dictionary<string, SarifRuleBreakdownEntry>? breakdown)
  {
    var metricValue = new MetricValue
    {
      Value = 1,
      Status = ThresholdStatus.NotApplicable,
      Breakdown = breakdown
    };

    return new Dictionary<MetricIdentifier, MetricValue>
    {
      [identifier] = metricValue
    };
  }

  private static Dictionary<string, SarifRuleBreakdownEntry>? CreateRuleBreakdown(
      string ruleId,
      SarifLocation location,
      string? messageText)
  {
    if (!RuleIdValidator.IsValidRuleId(ruleId))
    {
      return null;
    }

    var violation = new SarifRuleViolationDetail
    {
      Message = messageText,
      Uri = location.OriginalUri,
      StartLine = location.Source.StartLine,
      EndLine = location.Source.EndLine
    };

    var entry = new SarifRuleBreakdownEntry
    {
      Count = 1,
      Violations = [violation]
    };

    return new Dictionary<string, SarifRuleBreakdownEntry>(1, StringComparer.Ordinal)
    {
      [ruleId] = entry
    };
  }
}


