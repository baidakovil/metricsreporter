using System;
using System.Collections.Generic;
using FluentAssertions;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using NUnit.Framework;

namespace MetricsReporter.Tests.Aggregation;

[TestFixture]
[Category("Unit")]
internal sealed class PlainNestedTypeCoverageReconcilerTests
{
  [Test]
  public void Reconcile_PlusTypeWithCoverage_TransfersTypeAndMembersAndRemovesPlusType()
  {
    var plusTypeKey = "My.Namespace.Outer+Inner";
    var dotTypeKey = "My.Namespace.Outer.Inner";
    var plusMemberFqn = $"{plusTypeKey}.Method(System.String)";

    var plusType = CreateTypeEntry(
        plusTypeKey,
        CreateMetrics((MetricIdentifier.AltCoverSequenceCoverage, 10m)),
        CreateMember("Method(...)", plusMemberFqn, CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 2m))));
    var dotType = CreateTypeEntry(dotTypeKey);

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [plusTypeKey] = plusType,
      [dotTypeKey] = dotType
    };
    var members = new Dictionary<string, MemberMetricsNode>(StringComparer.Ordinal);
    var removedTypes = new List<string>();

    PlainNestedTypeCoverageReconciler.Reconcile(types, members, (key, entry) =>
    {
      removedTypes.Add(key);
      types.Remove(key);
    });

    removedTypes.Should().ContainSingle().Which.Should().Be(plusTypeKey);
    types.Should().NotContainKey(plusTypeKey);
    types.Should().ContainKey(dotTypeKey);

    var reconciledType = types[dotTypeKey].Node;
    reconciledType.Metrics.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
    reconciledType.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(10m);

    reconciledType.Members.Should().ContainSingle();
    var reconciledMember = reconciledType.Members[0];
    reconciledMember.Metrics[MetricIdentifier.AltCoverBranchCoverage].Value.Should().Be(2m);
    reconciledMember.IncludesIteratorStateMachineCoverage.Should().BeTrue();
    reconciledMember.FullyQualifiedName.Should().Be("My.Namespace.Outer.Inner.Method(System.String)");
    members.Should().ContainKey("My.Namespace.Outer.Inner.Method(System.String)");
  }

  [Test]
  public void Reconcile_WhenDotTypeAlreadyHasCoverage_SkipsTransferAndRemoval()
  {
    var plusTypeKey = "My.Namespace.Outer+Inner";
    var dotTypeKey = "My.Namespace.Outer.Inner";

    var plusType = CreateTypeEntry(
        plusTypeKey,
        CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 1m)));
    var dotType = CreateTypeEntry(
        dotTypeKey,
        CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 5m)));

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [plusTypeKey] = plusType,
      [dotTypeKey] = dotType
    };
    var removedTypes = new List<string>();

    PlainNestedTypeCoverageReconciler.Reconcile(types, new Dictionary<string, MemberMetricsNode>(), (key, entry) =>
    {
      removedTypes.Add(key);
      types.Remove(key);
    });

    removedTypes.Should().BeEmpty();
    types.Should().ContainKeys(plusTypeKey, dotTypeKey);
    types[dotTypeKey].Node.Metrics[MetricIdentifier.AltCoverBranchCoverage].Value.Should().Be(5m);
  }

  [Test]
  public void Reconcile_WhenMethodCoverageConflictExists_SkipsTransfer()
  {
    var plusTypeKey = "My.Namespace.Outer+Inner";
    var dotTypeKey = "My.Namespace.Outer.Inner";
    var methodName = "Method";

    var plusMemberFqn = $"{plusTypeKey}.{methodName}(System.String)";
    var dotMemberFqn = $"{dotTypeKey}.{methodName}(System.String)";

    var plusType = CreateTypeEntry(
        plusTypeKey,
        CreateMetrics((MetricIdentifier.AltCoverSequenceCoverage, 0m)),
        CreateMember("Method(...)", plusMemberFqn, CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 3m))));
    var dotType = CreateTypeEntry(
        dotTypeKey,
        CreateMetrics((MetricIdentifier.AltCoverSequenceCoverage, 0m)),
        CreateMember("Method(...)", dotMemberFqn, CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 4m))));

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [plusTypeKey] = plusType,
      [dotTypeKey] = dotType
    };
    var removedTypes = new List<string>();

    PlainNestedTypeCoverageReconciler.Reconcile(types, new Dictionary<string, MemberMetricsNode>(), (key, entry) =>
    {
      removedTypes.Add(key);
      types.Remove(key);
    });

    removedTypes.Should().BeEmpty();
    types.Should().ContainKeys(plusTypeKey, dotTypeKey);
    types[dotTypeKey].Node.Members.Should().ContainSingle();
    types[dotTypeKey].Node.Members[0].Metrics[MetricIdentifier.AltCoverBranchCoverage].Value.Should().Be(4m);
  }

  [Test]
  public void Reconcile_WhenDotTypeIsMissing_DoesNotRemovePlusType()
  {
    var plusTypeKey = "My.Namespace.Outer+Inner";
    var plusType = CreateTypeEntry(
        plusTypeKey,
        CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 1m)));

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [plusTypeKey] = plusType
    };
    var removedTypes = new List<string>();

    PlainNestedTypeCoverageReconciler.Reconcile(types, new Dictionary<string, MemberMetricsNode>(), (key, entry) =>
    {
      removedTypes.Add(key);
      types.Remove(key);
    });

    removedTypes.Should().BeEmpty();
    types.Should().ContainKey(plusTypeKey);
  }

  [Test]
  public void Reconcile_WithUnparseablePlusType_SkipsRemoval()
  {
    var plusTypeKey = "My.Namespace.Outer+<Inner>";
    var plusType = CreateTypeEntry(
        plusTypeKey,
        CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 1m)));

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [plusTypeKey] = plusType
    };
    var removedTypes = new List<string>();

    PlainNestedTypeCoverageReconciler.Reconcile(types, new Dictionary<string, MemberMetricsNode>(), (key, entry) =>
    {
      removedTypes.Add(key);
      types.Remove(key);
    });

    removedTypes.Should().BeEmpty();
    types.Should().ContainKey(plusTypeKey);
  }

  [Test]
  public void Reconcile_WhenOnlyMemberHasCoverage_TransfersMemberButNotTypeMetrics()
  {
    var plusTypeKey = "My.Namespace.Outer+Inner";
    var dotTypeKey = "My.Namespace.Outer.Inner";
    var methodFqn = $"{plusTypeKey}.Method()";

    var plusType = CreateTypeEntry(
        plusTypeKey,
        metrics: new Dictionary<MetricIdentifier, MetricValue>(),
        CreateMember("Method(...)", methodFqn, CreateMetrics((MetricIdentifier.AltCoverSequenceCoverage, 1m))));

    var dotType = CreateTypeEntry(
        dotTypeKey,
        metrics: new Dictionary<MetricIdentifier, MetricValue>(),
        CreateMember("Other(...)", $"{dotTypeKey}.Other()", CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 0m))));

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [plusTypeKey] = plusType,
      [dotTypeKey] = dotType
    };
    var members = new Dictionary<string, MemberMetricsNode>(StringComparer.Ordinal);
    var removedTypes = new List<string>();

    PlainNestedTypeCoverageReconciler.Reconcile(types, members, (key, entry) =>
    {
      removedTypes.Add(key);
      types.Remove(key);
    });

    removedTypes.Should().ContainSingle().Which.Should().Be(plusTypeKey);
    types.Should().NotContainKey(plusTypeKey);

    var reconciledType = types[dotTypeKey].Node;
    reconciledType.Metrics.Should().BeEmpty();
    reconciledType.Members.Should().HaveCount(2);

    var reconciledMember = reconciledType.Members.Should().ContainSingle(m => m.FullyQualifiedName == "My.Namespace.Outer.Inner.Method()").Subject;
    reconciledMember.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(1m);
    reconciledMember.IncludesIteratorStateMachineCoverage.Should().BeTrue();
    members.Should().ContainKey("My.Namespace.Outer.Inner.Method()");
  }

  private static TypeEntry CreateTypeEntry(
      string typeFqn,
      IDictionary<MetricIdentifier, MetricValue>? metrics = null,
      params MemberMetricsNode[] members)
  {
    var typeNode = new TypeMetricsNode
    {
      Name = typeFqn,
      FullyQualifiedName = typeFqn,
      Metrics = metrics ?? new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode>(members)
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Assembly",
      FullyQualifiedName = "Assembly"
    };

    return new TypeEntry(typeNode, assembly);
  }

  private static MemberMetricsNode CreateMember(
      string displayName,
      string fullyQualifiedName,
      IDictionary<MetricIdentifier, MetricValue>? metrics = null,
      bool includesIteratorCoverage = false)
  {
    return new MemberMetricsNode
    {
      Name = displayName,
      FullyQualifiedName = fullyQualifiedName,
      Metrics = metrics ?? new Dictionary<MetricIdentifier, MetricValue>(),
      IncludesIteratorStateMachineCoverage = includesIteratorCoverage
    };
  }

  private static Dictionary<MetricIdentifier, MetricValue> CreateMetrics(
      params (MetricIdentifier metric, decimal value)[] metrics)
  {
    var result = new Dictionary<MetricIdentifier, MetricValue>();
    foreach (var (metric, value) in metrics)
    {
      result[metric] = new MetricValue { Value = value };
    }

    return result;
  }
}

