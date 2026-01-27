namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;
using MetricsReporter.Model;
using MetricsReporter.Processing;

/// <summary>
/// Reconciles coverage for nested types defined with the plus-sign deconstruction pattern.
/// </summary>
internal sealed class PlainNestedTypeCoverageReconciler
{
  /// <summary>
  /// Transfers coverage from plus-type representations to their dot-type equivalents.
  /// </summary>
  /// <param name="types">The shared type lookup dictionary.</param>
  /// <param name="members">The shared member lookup dictionary.</param>
  /// <param name="removeIteratorType">Delegate invoked to drop reconciled types.</param>
  public static void Reconcile(
      IDictionary<string, TypeEntry> types,
      IDictionary<string, MemberMetricsNode> members,
      Action<string, TypeEntry> removeIteratorType)
  {
    if (types.Count == 0)
    {
      return;
    }

    var candidateTypeKeys = CollectPlainNestedTypeKeys(types);
    foreach (var plusTypeKey in candidateTypeKeys)
    {
      if (!TryBuildReconciliationContext(plusTypeKey, types, out var context))
      {
        continue;
      }

      if (context.ShouldTransferTypeCoverage)
      {
        TransferTypeOpenCoverCoverage(context.PlusTypeEntry.Node, context.DotTypeEntry.Node);
      }

      TransferMethodCoverageFromPlusType(
          context.PlusTypeEntry,
          context.DotTypeEntry,
          plusTypeKey,
          context.DotTypeFqn,
          members);

      removeIteratorType(plusTypeKey, context.PlusTypeEntry);
    }
  }

  private static bool TryBuildReconciliationContext(
      string plusTypeKey,
      IDictionary<string, TypeEntry> types,
      out ReconciliationContext context)
  {
    context = default;

    if (!types.TryGetValue(plusTypeKey, out var plusTypeEntry))
    {
      return false;
    }

    if (!TryParsePlainNestedPlusType(plusTypeKey, out _, out _, out var dotTypeFqn))
    {
      return false;
    }

    if (!types.TryGetValue(dotTypeFqn, out var dotTypeEntry))
    {
      return false;
    }

    var plusTypeHasCoverage = HasNonZeroOpenCoverCoverage(plusTypeEntry.Node.Metrics);
    var dotTypeHasCoverage = HasNonZeroOpenCoverCoverage(dotTypeEntry.Node.Metrics);

    if ((plusTypeHasCoverage && dotTypeHasCoverage) ||
        HasMethodCoverageConflict(plusTypeEntry.Node, dotTypeEntry.Node))
    {
      return false;
    }

    context = new ReconciliationContext(
        plusTypeEntry,
        dotTypeEntry,
        dotTypeFqn,
        plusTypeHasCoverage && !dotTypeHasCoverage);

    return true;
  }

  private static List<string> CollectPlainNestedTypeKeys(IDictionary<string, TypeEntry> types)
  {
    var result = new List<string>();

    foreach (var key in types.Keys)
    {
      if (TryParsePlainNestedPlusType(key, out _, out _, out _))
      {
        result.Add(key);
      }
    }

    return result;
  }

  private static bool TryParsePlainNestedPlusType(
      string typeFqn,
      out string namespaceFqn,
      out string[] segments,
      out string dotTypeFqn)
  {
    namespaceFqn = ResolveNamespaceName(typeFqn);
    dotTypeFqn = string.Empty;
    segments = Array.Empty<string>();

    if (string.IsNullOrWhiteSpace(typeFqn))
    {
      return false;
    }

    var nsPrefix = string.IsNullOrWhiteSpace(namespaceFqn) || namespaceFqn == "<global>"
        ? string.Empty
        : namespaceFqn + ".";

    if (!typeFqn.StartsWith(nsPrefix, StringComparison.Ordinal) || typeFqn.Length <= nsPrefix.Length)
    {
      return false;
    }

    var typePart = typeFqn[nsPrefix.Length..];
    if (!typePart.Contains('+', StringComparison.Ordinal))
    {
      return false;
    }

    segments = typePart.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (segments.Length < 2)
    {
      return false;
    }

    foreach (var segment in segments)
    {
      if (segment.Contains('<', StringComparison.Ordinal) ||
          segment.Contains('>', StringComparison.Ordinal) ||
          segment.Contains("__", StringComparison.Ordinal))
      {
        return false;
      }
    }

    var leafName = segments[^1];
    var parentSegments = segments[..^1];
    var dotNamespace = namespaceFqn == "<global>"
        ? string.Join('.', parentSegments)
        : string.IsNullOrWhiteSpace(namespaceFqn)
            ? string.Join('.', parentSegments)
            : namespaceFqn + "." + string.Join('.', parentSegments);

    dotTypeFqn = string.IsNullOrWhiteSpace(dotNamespace) ? leafName : dotNamespace + "." + leafName;
    return true;
  }

  private static bool HasMethodCoverageConflict(TypeMetricsNode plusType, TypeMetricsNode dotType)
  {
    if (plusType.Members.Count == 0 || dotType.Members.Count == 0)
    {
      return false;
    }

    var dotMethods = BuildMethodMapByName(dotType);
    foreach (var plusMember in plusType.Members)
    {
      var name = ExtractMethodKey(plusMember);
      if (string.IsNullOrWhiteSpace(name))
      {
        continue;
      }

      if (!dotMethods.TryGetValue(name, out var dotMember))
      {
        continue;
      }

      var plusHasCoverage = HasNonZeroOpenCoverCoverage(plusMember.Metrics);
      var dotHasCoverage = HasNonZeroOpenCoverCoverage(dotMember.Metrics);

      if (plusHasCoverage && dotHasCoverage)
      {
        return true;
      }
    }

    return false;
  }

  private static Dictionary<string, MemberMetricsNode> BuildMethodMapByName(TypeMetricsNode typeNode)
  {
    var result = new Dictionary<string, MemberMetricsNode>(StringComparer.Ordinal);
    foreach (var member in typeNode.Members)
    {
      var name = ExtractMethodKey(member);
      if (string.IsNullOrWhiteSpace(name))
      {
        continue;
      }

      if (!result.ContainsKey(name))
      {
        result[name] = member;
      }
    }

    return result;
  }

  private static string? ExtractMethodKey(MemberMetricsNode member)
  {
    if (!string.IsNullOrWhiteSpace(member.FullyQualifiedName))
    {
      return SymbolNormalizer.ExtractMethodName(member.FullyQualifiedName);
    }

    return SymbolNormalizer.ExtractMethodName(member.Name);
  }

  private static bool HasNonZeroOpenCoverCoverage(IDictionary<MetricIdentifier, MetricValue> metrics)
  {
    if (metrics.TryGetValue(MetricIdentifier.OpenCoverSequenceCoverage, out var seq) &&
        seq.Value.HasValue && seq.Value.Value != 0)
    {
      return true;
    }

    if (metrics.TryGetValue(MetricIdentifier.OpenCoverBranchCoverage, out var br) &&
        br.Value.HasValue && br.Value.Value != 0)
    {
      return true;
    }

    return false;
  }

  private static void TransferTypeOpenCoverCoverage(TypeMetricsNode sourceType, TypeMetricsNode targetType)
  {
    CopyOpenCoverMetricIfPresent(sourceType.Metrics, targetType.Metrics, MetricIdentifier.OpenCoverSequenceCoverage);
    CopyOpenCoverMetricIfPresent(sourceType.Metrics, targetType.Metrics, MetricIdentifier.OpenCoverBranchCoverage);
    CopyOpenCoverMetricIfPresent(sourceType.Metrics, targetType.Metrics, MetricIdentifier.OpenCoverCyclomaticComplexity);
    CopyOpenCoverMetricIfPresent(sourceType.Metrics, targetType.Metrics, MetricIdentifier.OpenCoverNPathComplexity);
  }

  private static void TransferMethodCoverageFromPlusType(
      TypeEntry plusTypeEntry,
      TypeEntry dotTypeEntry,
      string plusTypeFqn,
      string dotTypeFqn,
      IDictionary<string, MemberMetricsNode> members)
  {
    var dotMethodsByName = BuildMethodMapByName(dotTypeEntry.Node);
    foreach (var plusMember in plusTypeEntry.Node.Members)
    {
      var methodName = ExtractMethodKey(plusMember);
      if (string.IsNullOrWhiteSpace(methodName))
      {
        continue;
      }

      var plusHasCoverage = HasNonZeroOpenCoverCoverage(plusMember.Metrics);
      if (!plusHasCoverage)
      {
        continue;
      }

      dotMethodsByName.TryGetValue(methodName, out var dotMember);
      var dotHasCoverage = dotMember is not null && HasNonZeroOpenCoverCoverage(dotMember.Metrics);

      if (dotHasCoverage)
      {
        continue;
      }

      if (dotMember is null)
      {
        var memberFqn = BuildDotMemberFqn(plusMember.FullyQualifiedName, plusTypeFqn, dotTypeFqn, methodName);
        dotMember = new MemberMetricsNode
        {
          Name = ExtractMemberDisplayName(memberFqn, methodName),
          FullyQualifiedName = memberFqn,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        };
        dotTypeEntry.Node.Members.Add(dotMember);
        if (!string.IsNullOrWhiteSpace(memberFqn))
        {
          members[memberFqn] = dotMember;
        }
      }

      CopyOpenCoverMetricIfPresent(plusMember.Metrics, dotMember.Metrics, MetricIdentifier.OpenCoverSequenceCoverage);
      CopyOpenCoverMetricIfPresent(plusMember.Metrics, dotMember.Metrics, MetricIdentifier.OpenCoverBranchCoverage);
      CopyOpenCoverMetricIfPresent(plusMember.Metrics, dotMember.Metrics, MetricIdentifier.OpenCoverCyclomaticComplexity);
      CopyOpenCoverMetricIfPresent(plusMember.Metrics, dotMember.Metrics, MetricIdentifier.OpenCoverNPathComplexity);

      dotMember.IncludesIteratorStateMachineCoverage = true;
    }
  }

  private static string BuildDotMemberFqn(
      string? plusMemberFqn,
      string plusTypeFqn,
      string dotTypeFqn,
      string methodName)
  {
    if (!string.IsNullOrWhiteSpace(plusMemberFqn) &&
        plusMemberFqn.StartsWith(plusTypeFqn, StringComparison.Ordinal) &&
        plusMemberFqn.Length > plusTypeFqn.Length)
    {
      return dotTypeFqn + plusMemberFqn[plusTypeFqn.Length..];
    }

    return dotTypeFqn + "." + methodName + "(...)";
  }

  private static string ExtractMemberDisplayName(string memberFqn, string fallback)
  {
    if (string.IsNullOrWhiteSpace(memberFqn))
    {
      return fallback;
    }

    var paramStart = memberFqn.IndexOf('(');
    var searchEnd = paramStart >= 0 ? paramStart : memberFqn.Length;
    var lastDot = memberFqn.LastIndexOf('.', searchEnd - 1);

    if (lastDot < 0)
    {
      if (paramStart >= 0)
      {
        return memberFqn[..paramStart];
      }

      return fallback;
    }

    var methodNameStart = lastDot + 1;
    var methodNameEnd = paramStart >= 0 ? paramStart : memberFqn.Length;
    var methodName = memberFqn[methodNameStart..methodNameEnd].Trim();

    return string.IsNullOrWhiteSpace(methodName) ? fallback : methodName;
  }

  private static void CopyOpenCoverMetricIfPresent(
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

  private static string ResolveNamespaceName(string typeFqn)
  {
    var lastDot = typeFqn.LastIndexOf('.');
    return lastDot <= 0 ? "<global>" : typeFqn[..lastDot];
  }

  private readonly record struct ReconciliationContext(
      TypeEntry PlusTypeEntry,
      TypeEntry DotTypeEntry,
      string DotTypeFqn,
      bool ShouldTransferTypeCoverage);
}


