namespace MetricsReporter.Tests.Aggregation;

using System.Collections.Generic;
using FluentAssertions;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using NUnit.Framework;

[TestFixture]
[Category("Unit")]
public sealed class MetricsNodeLookupTests
{
  // Ensures lookups with blank identifiers fail fast to avoid populating output parameters unexpectedly.
  [Test]
  public void TryGetNode_WhitespaceInput_ReturnsFalse()
  {
    // Arrange
    var lookup = MetricsNodeLookup.Create(CreateSolution(out _));

    // Act
    var result = lookup.TryGetNode("  ", out var node);

    // Assert
    result.Should().BeFalse();
    node.Should().BeNull();
  }

  // Verifies known members are retrievable after building the lookup index.
  [Test]
  public void TryGetNode_NodeExists_ReturnsExpectedNode()
  {
    // Arrange
    var lookup = MetricsNodeLookup.Create(CreateSolution(out var memberFqn));

    // Act
    var result = lookup.TryGetNode(memberFqn, out var node);

    // Assert
    result.Should().BeTrue();
    node.Should().BeOfType<MemberMetricsNode>();
    node!.FullyQualifiedName.Should().Be(memberFqn);
  }

  // Confirms missing identifiers are rejected and do not populate the out parameter.
  [Test]
  public void TryGetNode_UnknownNode_ReturnsFalse()
  {
    // Arrange
    var lookup = MetricsNodeLookup.Create(CreateSolution(out _));

    // Act
    var result = lookup.TryGetNode("Sample.Namespace.Type.Other()", out var node);

    // Assert
    result.Should().BeFalse();
    node.Should().BeNull();
  }

  private static SolutionMetricsNode CreateSolution(out string memberFqn)
  {
    memberFqn = "Sample.Namespace.Type.Method()";
    var typeFqn = "Sample.Namespace.Type";
    var namespaceFqn = "Sample.Namespace";

    var member = new MemberMetricsNode
    {
      Name = "Method",
      FullyQualifiedName = memberFqn
    };

    var type = new TypeMetricsNode
    {
      Name = "Type",
      FullyQualifiedName = typeFqn,
      Members = new List<MemberMetricsNode> { member }
    };

    var ns = new NamespaceMetricsNode
    {
      Name = namespaceFqn,
      FullyQualifiedName = namespaceFqn,
      Types = new List<TypeMetricsNode> { type }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Namespaces = new List<NamespaceMetricsNode> { ns }
    };

    return new SolutionMetricsNode
    {
      Name = "Solution",
      FullyQualifiedName = "Solution",
      Assemblies = new List<AssemblyMetricsNode> { assembly }
    };
  }
}

