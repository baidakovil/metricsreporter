namespace MetricsReporter.Tests.Rendering;

using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="DescendantCountIndexBuilder"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class DescendantCountIndexBuilderTests
{
  [Test]
  public void Build_WithSimpleHierarchy_CalculatesCorrectCounts()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      Name = "DoWork",
      FullyQualifiedName = "Sample.Namespace.SampleType.DoWork()",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var type = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode> { member }
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { type }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { @namespace }
    };

    var solution = new SolutionMetricsNode
    {
      Name = "SampleSolution",
      FullyQualifiedName = "SampleSolution",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Assemblies = new List<AssemblyMetricsNode> { assembly }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata(),
      Solution = solution
    };

    // Act
    var result = DescendantCountIndexBuilder.Build(report);

    // Assert
    result.Should().ContainKey(solution);
    result[solution].Should().Be(4); // 1 assembly + 1 namespace + 1 type + 1 member = 4 total descendants

    result.Should().ContainKey(assembly);
    result[assembly].Should().Be(3); // 1 namespace + 1 type + 1 member = 3 total descendants

    result.Should().ContainKey(@namespace);
    result[@namespace].Should().Be(2); // 1 type + 1 member = 2 total descendants

    result.Should().ContainKey(type);
    result[type].Should().Be(1); // 1 member = 1 total descendant

    result.Should().ContainKey(member); // All nodes are added to the index, even if they have no descendants
    result[member].Should().Be(0); // Members have no descendants
  }

  [Test]
  public void Build_WithMultipleChildren_CalculatesTotalCounts()
  {
    // Arrange
    var member1 = new MemberMetricsNode
    {
      Name = "DoWork1",
      FullyQualifiedName = "Sample.Namespace.SampleType.DoWork1()",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var member2 = new MemberMetricsNode
    {
      Name = "DoWork2",
      FullyQualifiedName = "Sample.Namespace.SampleType.DoWork2()",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var type = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode> { member1, member2 }
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { type }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { @namespace }
    };

    var solution = new SolutionMetricsNode
    {
      Name = "SampleSolution",
      FullyQualifiedName = "SampleSolution",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Assemblies = new List<AssemblyMetricsNode> { assembly }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata(),
      Solution = solution
    };

    // Act
    var result = DescendantCountIndexBuilder.Build(report);

    // Assert
    result[type].Should().Be(2); // 2 members
    result[@namespace].Should().Be(3); // 1 type + 2 members = 3 total descendants
    result[assembly].Should().Be(4); // 1 namespace + 1 type + 2 members = 4 total descendants
    result[solution].Should().Be(5); // 1 assembly + 1 namespace + 1 type + 2 members = 5 total descendants
  }

  [Test]
  public void Build_WithNestedHierarchy_CalculatesRecursiveCounts()
  {
    // Arrange
    var member1 = new MemberMetricsNode
    {
      Name = "DoWork1",
      FullyQualifiedName = "Sample.Namespace.Type1.DoWork1()",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var member2 = new MemberMetricsNode
    {
      Name = "DoWork2",
      FullyQualifiedName = "Sample.Namespace.Type1.DoWork2()",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var type1 = new TypeMetricsNode
    {
      Name = "Type1",
      FullyQualifiedName = "Sample.Namespace.Type1",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode> { member1, member2 }
    };

    var type2 = new TypeMetricsNode
    {
      Name = "Type2",
      FullyQualifiedName = "Sample.Namespace.Type2",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode>()
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { type1, type2 }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { @namespace }
    };

    var solution = new SolutionMetricsNode
    {
      Name = "SampleSolution",
      FullyQualifiedName = "SampleSolution",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Assemblies = new List<AssemblyMetricsNode> { assembly }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata(),
      Solution = solution
    };

    // Act
    var result = DescendantCountIndexBuilder.Build(report);

    // Assert
    result[type1].Should().Be(2); // 2 members
    result[type2].Should().Be(0); // 0 members
    result[@namespace].Should().Be(4); // 2 types + 2 members = 4 total descendants
    result[assembly].Should().Be(5); // 1 namespace + 2 types + 2 members = 5 total descendants
    result[solution].Should().Be(6); // 1 assembly + 1 namespace + 2 types + 2 members = 6 total descendants
  }

  [Test]
  public void Build_WithEmptySolution_ReturnsEmptyDictionary()
  {
    // Arrange
    var solution = new SolutionMetricsNode
    {
      Name = "SampleSolution",
      FullyQualifiedName = "SampleSolution",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Assemblies = new List<AssemblyMetricsNode>()
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata(),
      Solution = solution
    };

    // Act
    var result = DescendantCountIndexBuilder.Build(report);

    // Assert
    result.Should().ContainKey(solution);
    result[solution].Should().Be(0); // 0 assemblies
  }

  [Test]
  public void Build_WithNullSolution_ReturnsEmptyDictionary()
  {
    // Arrange
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata(),
      Solution = null!
    };

    // Act
    var result = DescendantCountIndexBuilder.Build(report);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void Build_WithNullCollections_HandlesGracefully()
  {
    // Arrange
    var type = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode>()
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { type }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { @namespace }
    };

    var solution = new SolutionMetricsNode
    {
      Name = "SampleSolution",
      FullyQualifiedName = "SampleSolution",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Assemblies = new List<AssemblyMetricsNode> { assembly }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata(),
      Solution = solution
    };

    // Act
    var result = DescendantCountIndexBuilder.Build(report);

    // Assert
    result.Should().ContainKey(type);
    result[type].Should().Be(0); // null members collection means 0 descendants
  }

  [Test]
  public void Build_WithComplexHierarchy_CalculatesCorrectCounts()
  {
    // Arrange
    var member1 = new MemberMetricsNode { Name = "M1", FullyQualifiedName = "N.T1.M1()", Metrics = new Dictionary<MetricIdentifier, MetricValue>() };
    var member2 = new MemberMetricsNode { Name = "M2", FullyQualifiedName = "N.T1.M2()", Metrics = new Dictionary<MetricIdentifier, MetricValue>() };
    var member3 = new MemberMetricsNode { Name = "M3", FullyQualifiedName = "N.T2.M3()", Metrics = new Dictionary<MetricIdentifier, MetricValue>() };

    var type1 = new TypeMetricsNode
    {
      Name = "T1",
      FullyQualifiedName = "N.T1",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode> { member1, member2 }
    };

    var type2 = new TypeMetricsNode
    {
      Name = "T2",
      FullyQualifiedName = "N.T2",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode> { member3 }
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = "N",
      FullyQualifiedName = "N",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { type1, type2 }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "A",
      FullyQualifiedName = "A",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { @namespace }
    };

    var solution = new SolutionMetricsNode
    {
      Name = "S",
      FullyQualifiedName = "S",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Assemblies = new List<AssemblyMetricsNode> { assembly }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata(),
      Solution = solution
    };

    // Act
    var result = DescendantCountIndexBuilder.Build(report);

    // Assert
    result[type1].Should().Be(2); // 2 members
    result[type2].Should().Be(1); // 1 member
    result[@namespace].Should().Be(5); // 2 types + 3 members = 5 total descendants
    result[assembly].Should().Be(6); // 1 namespace + 2 types + 3 members = 6 total descendants
    result[solution].Should().Be(7); // 1 assembly + 1 namespace + 2 types + 3 members = 7 total descendants
  }
}


