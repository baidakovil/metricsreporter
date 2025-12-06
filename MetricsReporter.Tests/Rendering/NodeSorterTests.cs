namespace MetricsReporter.Tests.Rendering;

using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="NodeSorter"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class NodeSorterTests
{
  [Test]
  public void SortAssemblies_MixedCaseNames_SortsCaseInsensitively()
  {
    // Arrange
    var assemblies = new[]
    {
      new AssemblyMetricsNode { Name = "Zebra.Assembly" },
      new AssemblyMetricsNode { Name = "alpha.Assembly" },
      new AssemblyMetricsNode { Name = "Beta.Assembly" }
    };

    // Act
    var result = NodeSorter.SortAssemblies(assemblies);

    // Assert
    result.Should().HaveCount(3);
    result.Select(a => a.Name).Should().ContainInOrder("alpha.Assembly", "Beta.Assembly", "Zebra.Assembly");
  }

  [Test]
  public void SortAssemblies_EmptyCollection_ReturnsEmpty()
  {
    // Arrange
    var assemblies = Array.Empty<AssemblyMetricsNode>();

    // Act
    var result = NodeSorter.SortAssemblies(assemblies);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void SortNamespaces_MixedCaseNames_SortsCaseInsensitively()
  {
    // Arrange
    var namespaces = new[]
    {
      new NamespaceMetricsNode { Name = "Zebra.Namespace" },
      new NamespaceMetricsNode { Name = "alpha.Namespace" },
      new NamespaceMetricsNode { Name = "Beta.Namespace" }
    };

    // Act
    var result = NodeSorter.SortNamespaces(namespaces);

    // Assert
    result.Should().HaveCount(3);
    result.Select(n => n.Name).Should().ContainInOrder("alpha.Namespace", "Beta.Namespace", "Zebra.Namespace");
  }

  [Test]
  public void SortTypes_MixedCaseNames_SortsCaseInsensitively()
  {
    // Arrange
    var types = new[]
    {
      new TypeMetricsNode { Name = "ZebraType" },
      new TypeMetricsNode { Name = "alphaType" },
      new TypeMetricsNode { Name = "BetaType" }
    };

    // Act
    var result = NodeSorter.SortTypes(types);

    // Assert
    result.Should().HaveCount(3);
    result.Select(t => t.Name).Should().ContainInOrder("alphaType", "BetaType", "ZebraType");
  }

  [Test]
  public void SortMembers_MixedCaseNames_SortsCaseInsensitively()
  {
    // Arrange
    var members = new[]
    {
      new MemberMetricsNode { Name = "ZebraMethod" },
      new MemberMetricsNode { Name = "alphaMethod" },
      new MemberMetricsNode { Name = "BetaMethod" }
    };

    // Act
    var result = NodeSorter.SortMembers(members);

    // Assert
    result.Should().HaveCount(3);
    result.Select(m => m.Name).Should().ContainInOrder("alphaMethod", "BetaMethod", "ZebraMethod");
  }

  [Test]
  public void SortAssemblies_SingleItem_ReturnsSameItem()
  {
    // Arrange
    var assemblies = new[]
    {
      new AssemblyMetricsNode { Name = "Single.Assembly" }
    };

    // Act
    var result = NodeSorter.SortAssemblies(assemblies);

    // Assert
    result.Should().HaveCount(1);
    result.First().Name.Should().Be("Single.Assembly");
  }

  [Test]
  public void SortNamespaces_AlreadySorted_ReturnsSameOrder()
  {
    // Arrange
    var namespaces = new[]
    {
      new NamespaceMetricsNode { Name = "A.Namespace" },
      new NamespaceMetricsNode { Name = "B.Namespace" },
      new NamespaceMetricsNode { Name = "C.Namespace" }
    };

    // Act
    var result = NodeSorter.SortNamespaces(namespaces);

    // Assert
    result.Select(n => n.Name).Should().ContainInOrder("A.Namespace", "B.Namespace", "C.Namespace");
  }
}


