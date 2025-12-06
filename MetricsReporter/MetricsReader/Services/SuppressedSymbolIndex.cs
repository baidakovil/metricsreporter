namespace MetricsReporter.MetricsReader.Services;

using System;
using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Provides efficient lookup for suppressed symbols by fully qualified name and metric.
/// </summary>
internal sealed class SuppressedSymbolIndex : ISuppressedSymbolChecker
{
  private readonly Dictionary<(string Symbol, MetricIdentifier Metric), SuppressedSymbolInfo> _metricLookup;
  private readonly Dictionary<(string Symbol, string RuleId), SuppressedSymbolInfo> _ruleLookup;

  private SuppressedSymbolIndex(
    Dictionary<(string Symbol, MetricIdentifier Metric), SuppressedSymbolInfo> metricLookup,
    Dictionary<(string Symbol, string RuleId), SuppressedSymbolInfo> ruleLookup)
  {
    _metricLookup = metricLookup;
    _ruleLookup = ruleLookup;
  }

  public static SuppressedSymbolIndex Create(IEnumerable<SuppressedSymbolInfo> entries)
  {
    var metricLookup = new Dictionary<(string Symbol, MetricIdentifier Metric), SuppressedSymbolInfo>();
    var ruleLookup = new Dictionary<(string Symbol, string RuleId), SuppressedSymbolInfo>();
    foreach (var entry in entries)
    {
      if (string.IsNullOrWhiteSpace(entry.FullyQualifiedName))
      {
        continue;
      }

      if (!Enum.TryParse(entry.Metric, ignoreCase: true, out MetricIdentifier metric))
      {
        continue;
      }

      metricLookup[(entry.FullyQualifiedName, metric)] = entry;

      if (!string.IsNullOrWhiteSpace(entry.RuleId))
      {
        var normalizedRule = entry.RuleId.ToUpperInvariant();
        ruleLookup[(entry.FullyQualifiedName, normalizedRule)] = entry;
      }
    }

    return new SuppressedSymbolIndex(metricLookup, ruleLookup);
  }

  public bool IsSuppressed(string? fullyQualifiedName, MetricIdentifier metric, string? ruleId = null)
  {
    if (string.IsNullOrWhiteSpace(fullyQualifiedName))
    {
      return false;
    }

    if (_metricLookup.ContainsKey((fullyQualifiedName, metric)))
    {
      return true;
    }

    if (!string.IsNullOrWhiteSpace(ruleId))
    {
      var normalizedRule = ruleId.ToUpperInvariant();
      if (_ruleLookup.ContainsKey((fullyQualifiedName, normalizedRule)))
      {
        return true;
      }
    }

    return false;
  }
}


