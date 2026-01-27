namespace MetricsReporter.Tests.Aggregation;

using System;
using System.Collections.Generic;
using FluentAssertions;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using NUnit.Framework;

[TestFixture]
[Category("Unit")]
public sealed class StructuralElementMergerMemberKindTests
{
  [Test]
  public void MergeMember_FieldExcludedWithoutSarif_DropsMember()
  {
    // Arrange
    var (merger, solution) = CreateMerger(excludeFields: true);
    MergeAssembly(merger, "Sample.Assembly");
    var field = CreateMemberElement("Sample.Assembly.Sample.Type.Field", "Sample.Assembly.Sample.Type", "Sample.Assembly", MemberKind.Field, hasSarif: false);

    // Act
    merger.MergeMember(field);

    // Assert
    solution.Assemblies.Should().HaveCount(1);
    solution.Assemblies[0].Namespaces.Should().BeEmpty("field without SARIF should be filtered out when excludeFields=true and should not create namespace/type/member nodes");
  }

  [Test]
  public void MergeMember_FieldWithSarif_NotDropped()
  {
    // Arrange
    var (merger, solution) = CreateMerger(excludeFields: true);
    MergeAssembly(merger, "Sample.Assembly");
    var field = CreateMemberElement("Sample.Assembly.Sample.Type.Field", "Sample.Assembly.Sample.Type", "Sample.Assembly", MemberKind.Field, hasSarif: true);

    // Act
    merger.MergeMember(field);

    // Assert
    solution.Assemblies.Should().HaveCount(1);
    var type = solution.Assemblies[0].Namespaces[0].Types[0];
    type.Members.Should().ContainSingle();
    type.Members[0].MemberKind.Should().Be(MemberKind.Field);
    type.Members[0].HasSarifViolations.Should().BeTrue();
  }

  [Test]
  public void MergeMember_RoslynPropertyOverridesOpenCoverMethod()
  {
    // Arrange: OpenCover (method) arrives first, Roslyn (property) later.
    var (merger, solution) = CreateMerger(excludeFields: false);
    MergeAssembly(merger, "Sample.Assembly");
    var fqn = "Sample.Assembly.Sample.Type.Count";
    var typeFqn = "Sample.Assembly.Sample.Type";
    var assembly = "Sample.Assembly";
    var openCoverMethod = CreateMemberElement(fqn, typeFqn, assembly, MemberKind.Method, hasSarif: false);
    var roslynProperty = CreateMemberElement(fqn, typeFqn, assembly, MemberKind.Property, hasSarif: false);

    // Act
    merger.MergeMember(openCoverMethod);
    merger.MergeMember(roslynProperty);

    // Assert
    var type = solution.Assemblies[0].Namespaces[0].Types[0];
    type.Members.Should().ContainSingle();
    type.Members[0].MemberKind.Should().Be(MemberKind.Property, "Roslyn-provided kind should override OpenCover Method kind");
  }

  private static (StructuralElementMerger Merger, SolutionMetricsNode Solution) CreateMerger(bool excludeFields)
  {
    var solution = new SolutionMetricsNode { Name = "Solution", FullyQualifiedName = "Solution" };
    var merger = new StructuralElementMerger(
      solution,
      new Dictionary<string, AssemblyMetricsNode>(StringComparer.OrdinalIgnoreCase),
      new Dictionary<string, NamespaceEntry>(StringComparer.Ordinal),
      new Dictionary<string, List<NamespaceEntry>>(StringComparer.Ordinal),
      new Dictionary<string, TypeEntry>(StringComparer.Ordinal),
      new Dictionary<string, MemberMetricsNode>(StringComparer.Ordinal),
      new MemberFilter(),
      MemberKindFilter.Create(false, false, excludeFields, false),
      new AssemblyFilter(),
      new TypeFilter());
    return (merger, solution);
  }

  private static void MergeAssembly(StructuralElementMerger merger, string assemblyName)
  {
    var assemblyElement = new ParsedCodeElement(CodeElementKind.Assembly, assemblyName, assemblyName)
    {
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    merger.MergeAssembly(assemblyElement);
  }

  private static ParsedCodeElement CreateMemberElement(
    string fqn,
    string parentFqn,
    string assemblyName,
    MemberKind memberKind,
    bool hasSarif)
  {
    return new ParsedCodeElement(CodeElementKind.Member, fqn, fqn)
    {
      ParentFullyQualifiedName = parentFqn,
      ContainingAssemblyName = assemblyName,
      MemberKind = memberKind,
      HasSarifViolations = hasSarif,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };
  }
}


