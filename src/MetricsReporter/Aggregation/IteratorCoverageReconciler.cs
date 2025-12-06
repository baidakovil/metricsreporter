namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;
using MetricsReporter.Model;
using MetricsReporter.Processing;

/// <summary>
/// Reconciles coverage data produced in compiler-generated iterator state machine types.
/// </summary>
internal sealed class IteratorCoverageReconciler
{
  /// <summary>
  /// Transfers coverage from iterator types back to their originating methods when safe.
  /// </summary>
  /// <param name="types">The shared type lookup.</param>
  /// <param name="removeIteratorType">Delegate used to drop reconciled iterator types.</param>
  public static void Reconcile(
      IDictionary<string, TypeEntry> types,
      Action<string, TypeEntry> removeIteratorType)
  {
    if (types.Count == 0)
    {
      return;
    }

    var iteratorTypeKeys = CollectIteratorTypeKeys(types);
    foreach (var iteratorTypeKey in iteratorTypeKeys)
    {
      if (!types.TryGetValue(iteratorTypeKey, out var iteratorTypeEntry))
      {
        continue;
      }

      if (!TryExtractIteratorInfo(iteratorTypeKey, out var outerTypeFqn, out var methodName))
      {
        continue;
      }

      if (!types.TryGetValue(outerTypeFqn, out var outerTypeEntry))
      {
        continue;
      }

      var targetMember = FindMethodOnType(outerTypeEntry.Node, methodName);
      if (targetMember is null)
      {
        continue;
      }

      var methodHasCoverage = HasNonZeroAltCoverCoverage(targetMember.Metrics);
      var iteratorHasCoverage = HasNonZeroAltCoverCoverage(iteratorTypeEntry.Node.Metrics);

      if (methodHasCoverage && iteratorHasCoverage)
      {
        continue;
      }

      if (!methodHasCoverage && !iteratorHasCoverage)
      {
        removeIteratorType(iteratorTypeKey, iteratorTypeEntry);
        continue;
      }

      if (!methodHasCoverage && iteratorHasCoverage)
      {
        TransferIteratorCoverage(iteratorTypeEntry.Node, targetMember);
        removeIteratorType(iteratorTypeKey, iteratorTypeEntry);
      }
    }
  }

  private static List<string> CollectIteratorTypeKeys(IDictionary<string, TypeEntry> types)
  {
    var result = new List<string>();
    foreach (var key in types.Keys)
    {
      if (TryExtractIteratorInfo(key, out _, out _))
      {
        result.Add(key);
      }
    }

    return result;
  }

  private static bool TryExtractIteratorInfo(string typeFqn, out string outerTypeFqn, out string methodName)
  {
    outerTypeFqn = string.Empty;
    methodName = string.Empty;

    if (string.IsNullOrWhiteSpace(typeFqn))
    {
      return false;
    }

    var plusIndex = typeFqn.LastIndexOf('+');
    if (plusIndex <= 0 || plusIndex >= typeFqn.Length - 1)
    {
      return false;
    }

    var nestedPart = typeFqn[(plusIndex + 1)..];
    if (!nestedPart.StartsWith('<') || nestedPart.IndexOf('>') is var closeIndex && closeIndex <= 1)
    {
      return false;
    }

    var endOfName = nestedPart.IndexOf('>');
    if (endOfName <= 1 || endOfName >= nestedPart.Length - 1)
    {
      return false;
    }

    var suffix = nestedPart[(endOfName + 1)..];
    if (!suffix.StartsWith("d__"))
    {
      return false;
    }

    var numberPart = suffix["d__".Length..];
    if (numberPart.Length == 0 || !int.TryParse(numberPart, out _))
    {
      return false;
    }

    outerTypeFqn = typeFqn[..plusIndex];
    methodName = nestedPart[1..endOfName];
    return !string.IsNullOrWhiteSpace(outerTypeFqn) && !string.IsNullOrWhiteSpace(methodName);
  }

  private static MemberMetricsNode? FindMethodOnType(TypeMetricsNode typeNode, string methodName)
  {
    foreach (var member in typeNode.Members)
    {
      if (string.IsNullOrWhiteSpace(member.FullyQualifiedName))
      {
        continue;
      }

      var extractedName = SymbolNormalizer.ExtractMethodName(member.FullyQualifiedName);
      if (string.Equals(extractedName, methodName, StringComparison.Ordinal))
      {
        return member;
      }
    }

    return null;
  }

  private static bool HasNonZeroAltCoverCoverage(IDictionary<MetricIdentifier, MetricValue> metrics)
  {
    if (metrics.TryGetValue(MetricIdentifier.AltCoverSequenceCoverage, out var seq) &&
        seq.Value.HasValue && seq.Value.Value != 0)
    {
      return true;
    }

    if (metrics.TryGetValue(MetricIdentifier.AltCoverBranchCoverage, out var br) &&
        br.Value.HasValue && br.Value.Value != 0)
    {
      return true;
    }

    return false;
  }

  private static void TransferIteratorCoverage(TypeMetricsNode iteratorType, MemberMetricsNode targetMember)
  {
    // WHY: We always want to transfer sequence coverage from the iterator state machine
    // back to the user method, because AltCover attributes most IL for async/iterator
    // methods to the compiler-generated <Method>d__N type. Without this step, user
    // methods would appear as never executed even when their logical body did run.
    CopyAltCoverMetricIfPresent(iteratorType.Metrics, targetMember.Metrics, MetricIdentifier.AltCoverSequenceCoverage);

    // WHY: Branch coverage coming solely from the compiler-generated iterator type
    // is often not meaningful for the user-authored method. For async/iterator patterns
    // AltCover reports branches that belong to the state machine scaffolding rather
    // than to explicit control-flow in the source method. We therefore only propagate
    // branch coverage when the target method already has an AltCoverBranchCoverage
    // metric of its own (i.e. AltCover reported real branch points for the method).
    // This keeps branch coverage for plain methods intact while avoiding misleading
    // "0% branch coverage" on async methods like ParseAsync that have no user-visible
    // branches but do have internal state-machine branches.
    var methodHasOwnBranchMetric = targetMember.Metrics.ContainsKey(MetricIdentifier.AltCoverBranchCoverage);
    if (methodHasOwnBranchMetric)
    {
      CopyAltCoverMetricIfPresent(iteratorType.Metrics, targetMember.Metrics, MetricIdentifier.AltCoverBranchCoverage);
    }

    CopyAltCoverMetricIfPresent(iteratorType.Metrics, targetMember.Metrics, MetricIdentifier.AltCoverCyclomaticComplexity);
    CopyAltCoverMetricIfPresent(iteratorType.Metrics, targetMember.Metrics, MetricIdentifier.AltCoverNPathComplexity);

    targetMember.IncludesIteratorStateMachineCoverage = true;
  }

  private static void CopyAltCoverMetricIfPresent(
      IDictionary<MetricIdentifier, MetricValue> sourceMetrics,
      IDictionary<MetricIdentifier, MetricValue> targetMetrics,
      MetricIdentifier identifier)
  {
    if (!sourceMetrics.TryGetValue(identifier, out var sourceValue) ||
        !sourceValue.Value.HasValue)
    {
      return;
    }

    if (targetMetrics.TryGetValue(identifier, out var existing) &&
        existing.Value.HasValue &&
        existing.Value.Value != 0)
    {
      return;
    }

    targetMetrics[identifier] = new MetricValue
    {
      Value = sourceValue.Value,
      Status = sourceValue.Status,
      Delta = sourceValue.Delta
    };
  }
}

