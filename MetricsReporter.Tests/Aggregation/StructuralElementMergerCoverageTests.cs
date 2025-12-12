namespace MetricsReporter.Tests.Aggregation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using NUnit.Framework;
using ProcessingAssemblyFilter = MetricsReporter.Processing.AssemblyFilter;
using ProcessingMemberFilter = MetricsReporter.Processing.MemberFilter;
using ProcessingTypeFilter = MetricsReporter.Processing.TypeFilter;

[TestFixture]
[Category("Unit")]
public sealed class StructuralElementMergerCoverageTests
{
  // Verifies metric merging handles null replacements, aggregation, and source updates to exercise MergeExistingMetric branches.
  [Test]
  public void MergeAssembly_ReplacesNullAndAggregatesMetrics()
  {
    // Arrange
    var context = CreateContext();
    var first = new ParsedCodeElement(CodeElementKind.Assembly, "Sample.Assembly", "Sample.Assembly")
    {
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = new MetricValue { Value = null }
      },
      Source = new SourceLocation { Path = "first.cs" }
    };

    var replacement = new ParsedCodeElement(CodeElementKind.Assembly, "Sample.Assembly", "Sample.Assembly")
    {
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = new MetricValue { Value = 2, Status = ThresholdStatus.Warning }
      },
      Source = new SourceLocation { Path = "first.cs", StartLine = 10, EndLine = 12 }
    };

    var aggregate = new ParsedCodeElement(CodeElementKind.Assembly, "Sample.Assembly", "Sample.Assembly")
    {
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = new MetricValue { Value = 1 }
      }
    };

    // Act
    context.Merger.MergeAssembly(first);
    context.Merger.MergeAssembly(replacement);
    context.Merger.MergeAssembly(aggregate);

    // Assert
    var metric = context.Solution.Assemblies.Single().Metrics[MetricIdentifier.SarifCaRuleViolations];
    metric.Value.Should().Be(3);
    metric.Status.Should().Be(ThresholdStatus.NotApplicable);
    context.Solution.Assemblies.Single().Source!.StartLine.Should().Be(10);
  }

  // Exercises namespace reindexing when an excluded assembly forces dummy namespaces and removal from indexes.
  [Test]
  public void MergeNamespace_ExcludedAssembly_RemovesExistingIndexes()
  {
    // Arrange
    var context = CreateContext();
    var existingNamespaceNode = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var ghostAssembly = new AssemblyMetricsNode
    {
      Name = "Ghost.Assembly",
      FullyQualifiedName = "Ghost.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { existingNamespaceNode }
    };

    var existingEntry = new NamespaceEntry(existingNamespaceNode, ghostAssembly);
    context.Namespaces["Sample.Assembly::Sample.Namespace"] = existingEntry;
    context.NamespaceIndex["Sample.Namespace"] = new List<NamespaceEntry> { existingEntry };
    context.Assemblies["Sample.Assembly"] = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode>()
    };

    var element = new ParsedCodeElement(CodeElementKind.Namespace, "Sample.Namespace", "Sample.Namespace")
    {
      ParentFullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    context.Merger.MergeNamespace(element);

    // Assert
    context.Namespaces.ContainsKey("Sample.Assembly::Sample.Namespace").Should().BeFalse();
    context.NamespaceIndex["Sample.Namespace"].Should().BeEmpty();
  }

  // Ensures namespace index additions are idempotent to cover both branches in AddToNamespaceIndex.
  [Test]
  public void AddToNamespaceIndex_DoesNotDuplicateEntries()
  {
    // Arrange
    var context = CreateContext();
    var entry = new NamespaceEntry(
      new NamespaceMetricsNode
      {
        Name = "Sample.Namespace",
        FullyQualifiedName = "Sample.Namespace",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>()
      },
      new AssemblyMetricsNode
      {
        Name = "Sample.Assembly",
        FullyQualifiedName = "Sample.Assembly",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>()
      });

    var method = typeof(StructuralElementMerger).GetMethod("AddToNamespaceIndex", BindingFlags.NonPublic | BindingFlags.Instance);
    method.Should().NotBeNull("private AddToNamespaceIndex should be discoverable for coverage validation");

    // Act
    method!.Invoke(context.Merger, new object[] { "Sample.Namespace", entry });
    method.Invoke(context.Merger, new object[] { "Sample.Namespace", entry });

    // Assert
    context.NamespaceIndex["Sample.Namespace"].Should().HaveCount(1);
  }

  // Confirms type resolution removes stale entries when the assembly is missing from the workspace.
  [Test]
  public void MergeType_WithMissingAssembly_RemovesExistingEntry()
  {
    // Arrange
    var context = CreateContext();
    var ghostAssembly = new AssemblyMetricsNode
    {
      Name = "Ghost.Assembly",
      FullyQualifiedName = "Ghost.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode>()
    };
    var typeNode = new TypeMetricsNode
    {
      Name = "Ghost.Type",
      FullyQualifiedName = "Ghost.Type",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };
    var typeEntry = new TypeEntry(typeNode, ghostAssembly);
    context.Types["Ghost.Type"] = typeEntry;

    var element = new ParsedCodeElement(CodeElementKind.Type, "Ghost.Type", "Ghost.Type")
    {
      ParentFullyQualifiedName = "Ghost.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    context.Merger.MergeType(element);

    // Assert
    context.Types.ContainsKey("Ghost.Type").Should().BeFalse("existing type entries linked to missing assemblies should be replaced with dummy nodes");
  }

  // Validates member resolution falls back to declaring type parsing and replaces members when assemblies are not tracked.
  [Test]
  public void MergeMember_WithMissingAssembly_UsesDeclaringTypeResolution()
  {
    // Arrange
    var context = CreateContext();
    var typeNode = new TypeMetricsNode
    {
      Name = "Sample.Type",
      FullyQualifiedName = "Sample.Assembly.Sample.Type",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };
    var typeEntry = new TypeEntry(typeNode, new AssemblyMetricsNode
    {
      Name = "Ghost.Assembly",
      FullyQualifiedName = "Ghost.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode>()
    });
    context.Types[typeNode.FullyQualifiedName] = typeEntry;
    context.Members["Sample.Assembly.Sample.Type.Method()"] = new MemberMetricsNode
    {
      Name = "Old",
      FullyQualifiedName = "Sample.Assembly.Sample.Type.Method()",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };
    context.Assemblies["Sample.Assembly"] = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode>()
    };

    var element = new ParsedCodeElement(CodeElementKind.Member, "Method", "Sample.Assembly.Sample.Type.Method()")
    {
      ParentFullyQualifiedName = "Sample.Assembly.Sample.Type",
      ContainingAssemblyName = "Sample.Assembly",
      MemberKind = MemberKind.Method,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    context.Merger.MergeMember(element);

    // Assert
    context.Members.ContainsKey("Sample.Assembly.Sample.Type.Method()").Should().BeFalse("members tied to assemblies missing from the workspace fall back to dummy nodes");
  }

  // Covers assembly resolution fallbacks for excluded patterns and default assembly selection.
  [Test]
  public void ResolveAssemblyNameFromFqn_HandlesExcludedAndUnknownAssemblies()
  {
    // Arrange
    var context = CreateContext(excludedAssemblyPattern: "Tests");
    var method = typeof(StructuralElementMerger).GetMethod("ResolveAssemblyNameFromFqn", BindingFlags.NonPublic | BindingFlags.Instance);
    method.Should().NotBeNull("ResolveAssemblyNameFromFqn should be discoverable for coverage validation");

    // Act
    var excluded = (string)method!.Invoke(context.Merger, new object[] { "Tests.Namespace.Type" })!;
    var fallback = (string)method.Invoke(context.Merger, new object[] { "Unmapped.Namespace.Type" })!;

    // Assert
    excluded.Should().Be("Tests.Namespace.Type");
    fallback.Should().Be(context.Solution.Name);
  }

  private static MergerContext CreateContext(string? excludedAssemblyPattern = null)
  {
    var solution = new SolutionMetricsNode { Name = "Solution", FullyQualifiedName = "Solution", Metrics = new Dictionary<MetricIdentifier, MetricValue>() };
    var assemblies = new Dictionary<string, AssemblyMetricsNode>(StringComparer.OrdinalIgnoreCase);
    var namespaces = new Dictionary<string, NamespaceEntry>(StringComparer.Ordinal);
    var namespaceIndex = new Dictionary<string, List<NamespaceEntry>>(StringComparer.Ordinal);
    var types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal);
    var members = new Dictionary<string, MemberMetricsNode>(StringComparer.Ordinal);
    var assemblyFilter = string.IsNullOrWhiteSpace(excludedAssemblyPattern)
      ? new ProcessingAssemblyFilter()
      : ProcessingAssemblyFilter.FromString(excludedAssemblyPattern);

    var merger = new StructuralElementMerger(
      solution,
      assemblies,
      namespaces,
      namespaceIndex,
      types,
      members,
      new ProcessingMemberFilter(),
      MemberKindFilter.Create(false, false, false, false),
      assemblyFilter,
      new ProcessingTypeFilter());

    return new MergerContext(merger, solution, assemblies, namespaces, namespaceIndex, types, members, assemblyFilter);
  }

  private sealed record MergerContext(
    StructuralElementMerger Merger,
    SolutionMetricsNode Solution,
    Dictionary<string, AssemblyMetricsNode> Assemblies,
    Dictionary<string, NamespaceEntry> Namespaces,
    Dictionary<string, List<NamespaceEntry>> NamespaceIndex,
    Dictionary<string, TypeEntry> Types,
    Dictionary<string, MemberMetricsNode> Members,
    ProcessingAssemblyFilter AssemblyFilter);
}


