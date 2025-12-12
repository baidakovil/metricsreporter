using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using NUnit.Framework;

namespace MetricsReporter.Tests.Aggregation;

[TestFixture]
[Category("Unit")]
internal sealed class PlainNestedTypeCoverageReconcilerTests
{
  // Ensures full reconciliation moves coverage from iterator-generated plus types to their dot equivalents and cleans up the source entry.
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

  // Confirms dot types that already contain coverage are left untouched so iterator types are not removed prematurely.
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

  // Validates reconciliation halts when both iterator and dot members already carry coverage to avoid corrupting metrics.
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

  // Verifies iterator types remain when a matching dot type cannot be located, preserving original metrics.
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

  // Guards against parsing failures on synthetic plus-type names by leaving the iterator type untouched.
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

  // Confirms member-only coverage is transferred while leaving type-level metrics empty to avoid inflating aggregates.
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

  // Ensures members without resolvable names or coverage are ignored so reconciliation does not create empty artifacts.
  [Test]
  public void Reconcile_MembersWithoutNamesOrCoverage_AreIgnored()
  {
    var plusTypeKey = "My.Namespace.Outer+Inner";
    var dotTypeKey = "My.Namespace.Outer.Inner";

    var namelessMember = CreateMember(string.Empty, string.Empty, CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 5m)));
    var zeroCoverageMember = CreateMember("Method", $"{plusTypeKey}.Method", CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 0m)));

    var plusType = CreateTypeEntry(
        plusTypeKey,
        metrics: new Dictionary<MetricIdentifier, MetricValue>(),
        namelessMember,
        zeroCoverageMember);
    var dotType = CreateTypeEntry(dotTypeKey);

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [plusTypeKey] = plusType,
      [dotTypeKey] = dotType
    };
    var members = new Dictionary<string, MemberMetricsNode>(StringComparer.Ordinal);

    PlainNestedTypeCoverageReconciler.Reconcile(types, members, (key, entry) => types.Remove(key));

    types.Should().ContainKey(dotTypeKey);
    types.Should().NotContainKey(plusTypeKey);
    types[dotTypeKey].Node.Members.Should().BeEmpty();
    members.Should().BeEmpty();
  }

  // Verifies duplicate dot members retain the first definition so coverage is copied once without overwriting later duplicates.
  [Test]
  public void Reconcile_DuplicateDotMembers_TransfersCoverageToFirstMatch()
  {
    var plusTypeKey = "My.Namespace.Outer+Inner";
    var dotTypeKey = "My.Namespace.Outer.Inner";
    var methodName = "Method";

    var plusMemberFqn = $"{plusTypeKey}.{methodName}(System.String)";
    var dotMemberFqn = $"{dotTypeKey}.{methodName}(System.String)";
    var alternateDotMemberFqn = $"{dotTypeKey}.{methodName}(System.Int32)";

    var plusType = CreateTypeEntry(
        plusTypeKey,
        metrics: new Dictionary<MetricIdentifier, MetricValue>(),
        CreateMember("Method(...)", plusMemberFqn, CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 3m))));

    var firstDotMember = CreateMember("Method(...)", dotMemberFqn, CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 0m)));
    var secondDotMember = CreateMember("Method(...)", alternateDotMemberFqn, CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 0m)));
    var dotType = CreateTypeEntry(
        dotTypeKey,
        metrics: new Dictionary<MetricIdentifier, MetricValue>(),
        firstDotMember,
        secondDotMember);

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [plusTypeKey] = plusType,
      [dotTypeKey] = dotType
    };

    PlainNestedTypeCoverageReconciler.Reconcile(types, new Dictionary<string, MemberMetricsNode>(StringComparer.Ordinal), (key, entry) => types.Remove(key));

    types.Should().ContainKey(dotTypeKey);
    types.Should().NotContainKey(plusTypeKey);
    firstDotMember.Metrics[MetricIdentifier.AltCoverBranchCoverage].Value.Should().Be(3m);
    secondDotMember.Metrics[MetricIdentifier.AltCoverBranchCoverage].Value.Should().Be(0m);
    firstDotMember.IncludesIteratorStateMachineCoverage.Should().BeTrue();
    secondDotMember.IncludesIteratorStateMachineCoverage.Should().BeFalse();
  }

  // Confirms fallback member FQNs are constructed when iterator members use unexpected prefixes, exercising the BuildDotMemberFqn fallback.
  [Test]
  public void Reconcile_MismatchedMemberFqn_UsesFallbackMemberName()
  {
    var plusTypeKey = "My.Namespace.Outer+Inner";
    var dotTypeKey = "My.Namespace.Outer.Inner";

    var plusMember = CreateMember(
        "OtherMethod(...)",
        "Another.Namespace.OtherMethod(System.Int32)",
        CreateMetrics((MetricIdentifier.AltCoverSequenceCoverage, 2m)));

    var plusType = CreateTypeEntry(
        plusTypeKey,
        metrics: new Dictionary<MetricIdentifier, MetricValue>(),
        plusMember);
    var dotType = CreateTypeEntry(dotTypeKey);

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [plusTypeKey] = plusType,
      [dotTypeKey] = dotType
    };

    PlainNestedTypeCoverageReconciler.Reconcile(types, new Dictionary<string, MemberMetricsNode>(StringComparer.Ordinal), (key, entry) => types.Remove(key));

    types.Should().ContainKey(dotTypeKey);
    types.Should().NotContainKey(plusTypeKey);

    var reconciledMember = types[dotTypeKey].Node.Members.Should().ContainSingle().Subject;
    reconciledMember.FullyQualifiedName.Should().Be($"{dotTypeKey}.OtherMethod(...)");
    reconciledMember.Name.Should().Be("OtherMethod");
    reconciledMember.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(2m);
  }

  // Ensures parameterless iterator signatures still yield correct member names when transferring coverage.
  [Test]
  public void Reconcile_MemberWithoutParameters_PreservesMethodName()
  {
    var plusTypeKey = "My.Namespace.Outer+Inner";
    var dotTypeKey = "My.Namespace.Outer.Inner";
    var plusMemberFqn = $"{plusTypeKey}.ParameterlessMethod";

    var plusType = CreateTypeEntry(
        plusTypeKey,
        metrics: new Dictionary<MetricIdentifier, MetricValue>(),
        CreateMember("ParameterlessMethod", plusMemberFqn, CreateMetrics((MetricIdentifier.AltCoverBranchCoverage, 4m))));
    var dotType = CreateTypeEntry(dotTypeKey);

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [plusTypeKey] = plusType,
      [dotTypeKey] = dotType
    };
    var members = new Dictionary<string, MemberMetricsNode>(StringComparer.Ordinal);

    PlainNestedTypeCoverageReconciler.Reconcile(types, members, (key, entry) => types.Remove(key));

    var reconciledMember = types[dotTypeKey].Node.Members.Should().ContainSingle().Subject;
    reconciledMember.FullyQualifiedName.Should().Be($"{dotTypeKey}.ParameterlessMethod");
    reconciledMember.Name.Should().Be("ParameterlessMethod");
    members.Should().ContainKey($"{dotTypeKey}.ParameterlessMethod");
  }

  // Validates defensive name parsing paths when method identifiers are missing dots or contain whitespace placeholders.
  [Test]
  public void ExtractMemberDisplayName_UnusualInputs_FallBackToSafeValues()
  {
    var method = typeof(PlainNestedTypeCoverageReconciler).GetMethod("ExtractMemberDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
    method.Should().NotBeNull();

    var fallback = "Fallback";
    var whitespaceResult = (string)method!.Invoke(null, new object?[] { string.Empty, fallback })!;
    var noDotWithParams = (string)method.Invoke(null, new object?[] { "Method(System.String)", fallback })!;
    var noDotNoParams = (string)method.Invoke(null, new object?[] { "Method", fallback })!;
    var blankNameResult = (string)method.Invoke(null, new object?[] { "Namespace.Type.   (System.String)", fallback })!;

    whitespaceResult.Should().Be(fallback);
    noDotWithParams.Should().Be("Method");
    noDotNoParams.Should().Be(fallback);
    blankNameResult.Should().Be(fallback);
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

