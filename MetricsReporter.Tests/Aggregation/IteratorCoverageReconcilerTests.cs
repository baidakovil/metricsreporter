namespace MetricsReporter.Tests.Aggregation;

using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using NUnit.Framework;

[TestFixture]
[Category("Unit")]
public sealed class IteratorCoverageReconcilerTests
{
  // Ensures iterator coverage is transferred to the user method and the compiler-generated type is removed when only the iterator carries metrics.
  [Test]
  public void Reconcile_IteratorHasCoverage_TransfersMetricsAndRemovesIterator()
  {
    // Arrange
    var outerTypeKey = "Sample.Namespace.Type";
    var iteratorKey = $"{outerTypeKey}+<Enumerate>d__1";

    var methodMetrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.OpenCoverBranchCoverage] = new MetricValue { Value = null }
    };
    var iteratorMetrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.OpenCoverSequenceCoverage] = new MetricValue { Value = 5m },
      [MetricIdentifier.OpenCoverBranchCoverage] = new MetricValue { Value = 2m }
    };

    var method = CreateMember($"{outerTypeKey}.Enumerate()", methodMetrics);
    var outerType = CreateTypeEntry(outerTypeKey, members: method);
    var iteratorType = CreateTypeEntry(iteratorKey, iteratorMetrics);

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [outerTypeKey] = outerType,
      [iteratorKey] = iteratorType
    };
    var removedKeys = new List<string>();

    // Act
    IteratorCoverageReconciler.Reconcile(types, (key, entry) =>
    {
      removedKeys.Add(key);
      types.Remove(key);
    });

    // Assert
    removedKeys.Should().Contain(iteratorKey);
    types.Should().ContainKey(outerTypeKey);
    types.Should().NotContainKey(iteratorKey);
    method.Metrics[MetricIdentifier.OpenCoverSequenceCoverage].Value.Should().Be(5m);
    method.Metrics[MetricIdentifier.OpenCoverBranchCoverage].Value.Should().Be(2m);
    method.IncludesIteratorStateMachineCoverage.Should().BeTrue();
  }

  // Verifies reconciliation skips iterator removal when both the outer method and iterator already have coverage.
  [Test]
  public void Reconcile_MethodAlreadyHasCoverage_DoesNotMoveMetrics()
  {
    // Arrange
    var outerTypeKey = "Sample.Namespace.AsyncType";
    var iteratorKey = $"{outerTypeKey}+<Run>d__2";

    var methodMetrics = CreateMetrics((MetricIdentifier.OpenCoverSequenceCoverage, 3m));
    var iteratorMetrics = CreateMetrics((MetricIdentifier.OpenCoverSequenceCoverage, 4m));

    var method = CreateMember($"{outerTypeKey}.Run()", methodMetrics);
    var outerType = CreateTypeEntry(outerTypeKey, members: method);
    var iteratorType = CreateTypeEntry(iteratorKey, iteratorMetrics);

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [outerTypeKey] = outerType,
      [iteratorKey] = iteratorType
    };
    var removedKeys = new List<string>();

    // Act
    IteratorCoverageReconciler.Reconcile(types, (key, entry) =>
    {
      removedKeys.Add(key);
      types.Remove(key);
    });

    // Assert
    removedKeys.Should().BeEmpty();
    types.Should().ContainKeys(outerTypeKey, iteratorKey);
    method.Metrics[MetricIdentifier.OpenCoverSequenceCoverage].Value.Should().Be(3m);
    iteratorType.Node.Metrics[MetricIdentifier.OpenCoverSequenceCoverage].Value.Should().Be(4m);
  }

  // Confirms iterator types with no coverage are removed without altering the target method metrics.
  [Test]
  public void Reconcile_NoCoverage_RemovesIteratorWithoutTouchingMethod()
  {
    // Arrange
    var outerTypeKey = "Sample.Namespace.Iterator";
    var iteratorKey = $"{outerTypeKey}+<Walk>d__3";

    var method = CreateMember($"{outerTypeKey}.Walk()", new Dictionary<MetricIdentifier, MetricValue>());
    var outerType = CreateTypeEntry(outerTypeKey, members: method);
    var iteratorType = CreateTypeEntry(iteratorKey, new Dictionary<MetricIdentifier, MetricValue>());

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [outerTypeKey] = outerType,
      [iteratorKey] = iteratorType
    };
    var removedKeys = new List<string>();

    // Act
    IteratorCoverageReconciler.Reconcile(types, (key, entry) =>
    {
      removedKeys.Add(key);
      types.Remove(key);
    });

    // Assert
    removedKeys.Should().Contain(iteratorKey);
    types.Should().ContainKey(outerTypeKey);
    types.Should().NotContainKey(iteratorKey);
    method.Metrics.Should().BeEmpty();
    method.IncludesIteratorStateMachineCoverage.Should().BeFalse();
  }

  // Covers early exit when the iterator key cannot be resolved from the type dictionary.
  [Test]
  public void ProcessIteratorType_MissingIteratorEntry_DoesNothing()
  {
    // Arrange
    var processMethod = typeof(IteratorCoverageReconciler).GetMethod("ProcessIteratorType", BindingFlags.NonPublic | BindingFlags.Static);
    processMethod.Should().NotBeNull();
    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal);
    var removed = new List<string>();

    // Act
    processMethod!.Invoke(null, new object?[] { types, "Missing+<Method>d__1", new Action<string, TypeEntry>((key, entry) => removed.Add(key)) });

    // Assert
    removed.Should().BeEmpty();
    types.Should().BeEmpty();
  }

  // Ensures iterator types with missing outer types are ignored without invoking removal logic.
  [Test]
  public void ProcessIteratorType_OuterTypeMissing_SkipsRemoval()
  {
    // Arrange
    var processMethod = typeof(IteratorCoverageReconciler).GetMethod("ProcessIteratorType", BindingFlags.NonPublic | BindingFlags.Static);
    processMethod.Should().NotBeNull();

    var iteratorKey = "Sample.Namespace.Type+<MissingOuter>d__4";
    var iteratorType = CreateTypeEntry(iteratorKey, new Dictionary<MetricIdentifier, MetricValue>());

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [iteratorKey] = iteratorType
    };
    var removed = new List<string>();

    // Act
    processMethod!.Invoke(null, new object?[] { types, iteratorKey, new Action<string, TypeEntry>((key, entry) => removed.Add(key)) });

    // Assert
    removed.Should().BeEmpty();
    types.Should().ContainKey(iteratorKey);
  }

  // Validates iterator reconciliation aborts when the target method cannot be located on the outer type.
  [Test]
  public void ProcessIteratorType_TargetMethodMissing_DoesNotRemoveIterator()
  {
    // Arrange
    var processMethod = typeof(IteratorCoverageReconciler).GetMethod("ProcessIteratorType", BindingFlags.NonPublic | BindingFlags.Static);
    processMethod.Should().NotBeNull();

    var outerTypeKey = "Sample.Namespace.Type";
    var iteratorKey = $"{outerTypeKey}+<Iterate>d__5";

    var outerType = CreateTypeEntry(outerTypeKey, members: CreateMember($"{outerTypeKey}.Other()", new Dictionary<MetricIdentifier, MetricValue>()));
    var iteratorType = CreateTypeEntry(iteratorKey, new Dictionary<MetricIdentifier, MetricValue>());

    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal)
    {
      [outerTypeKey] = outerType,
      [iteratorKey] = iteratorType
    };
    var removed = new List<string>();

    // Act
    processMethod!.Invoke(null, new object?[] { types, iteratorKey, new Action<string, TypeEntry>((key, entry) => removed.Add(key)) });

    // Assert
    removed.Should().BeEmpty();
    types.Should().ContainKeys(outerTypeKey, iteratorKey);
  }

  // Validates iterator metadata parsing rejects malformed names and never assigns outputs for invalid inputs.
  [Test]
  public void TryExtractIteratorInfo_InvalidPatterns_ReturnFalse()
  {
    // Arrange
    var method = typeof(IteratorCoverageReconciler).GetMethod("TryExtractIteratorInfo", BindingFlags.NonPublic | BindingFlags.Static);
    method.Should().NotBeNull();

    // Act
    foreach (var input in new[] { null, string.Empty, "TypeWithoutPlus", "Outer+Inner", "Outer+<Bad>d__", "Outer+<Bad>d__X" })
    {
      var args = new object?[] { input!, string.Empty, string.Empty };
      var success = (bool)method!.Invoke(null, args)!;

      // Assert
      success.Should().BeFalse();
      args[1].Should().Be(string.Empty);
      args[2].Should().Be(string.Empty);
    }
  }

  // Confirms iterator metadata parsing extracts the outer type and method name for valid compiler-generated types.
  [Test]
  public void TryExtractIteratorInfo_ValidPattern_ReturnsParsedNames()
  {
    // Arrange
    var method = typeof(IteratorCoverageReconciler).GetMethod("TryExtractIteratorInfo", BindingFlags.NonPublic | BindingFlags.Static);
    method.Should().NotBeNull();
    var iteratorKey = "Sample.Namespace.Type+<Execute>d__5";

    var args = new object?[] { iteratorKey, string.Empty, string.Empty };

    // Act
    var success = (bool)method!.Invoke(null, args)!;

    // Assert
    success.Should().BeTrue();
    args[1].Should().Be("Sample.Namespace.Type");
    args[2].Should().Be("Execute");
  }

  // Ensures methods without fully qualified names are ignored when searching for iterator targets.
  [Test]
  public void FindMethodOnType_EmptyFullyQualifiedName_ReturnsNull()
  {
    // Arrange
    var methodInfo = typeof(IteratorCoverageReconciler).GetMethod("FindMethodOnType", BindingFlags.NonPublic | BindingFlags.Static);
    methodInfo.Should().NotBeNull();
    var typeNode = new TypeMetricsNode
    {
      Name = "Sample.Namespace.Type",
      FullyQualifiedName = "Sample.Namespace.Type",
      Members = new List<MemberMetricsNode>
      {
        new() { Name = "IteratorMethod", FullyQualifiedName = " " }
      }
    };

    // Act
    var result = methodInfo!.Invoke(null, new object?[] { typeNode, "IteratorMethod" });

    // Assert
    result.Should().BeNull();
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
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly"
    };

    return new TypeEntry(typeNode, assembly);
  }

  private static MemberMetricsNode CreateMember(
      string fullyQualifiedName,
      IDictionary<MetricIdentifier, MetricValue> metrics)
  {
    return new MemberMetricsNode
    {
      Name = fullyQualifiedName,
      FullyQualifiedName = fullyQualifiedName,
      Metrics = metrics
    };
  }

  private static Dictionary<MetricIdentifier, MetricValue> CreateMetrics(params (MetricIdentifier metric, decimal value)[] metrics)
  {
    var result = new Dictionary<MetricIdentifier, MetricValue>();
    foreach (var (metric, value) in metrics)
    {
      result[metric] = new MetricValue { Value = value };
    }

    return result;
  }
}

