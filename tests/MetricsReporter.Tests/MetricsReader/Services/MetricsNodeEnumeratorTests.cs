namespace MetricsReporter.Tests.MetricsReader.Services;

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Tests.MetricsReader;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Unit tests for <see cref="MetricsNodeEnumerator"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class MetricsNodeEnumeratorTests
{
  [Test]
  public void Constructor_NullReport_ThrowsArgumentNullException()
  {
    // Act
    var act = () => new MetricsNodeEnumerator(null!);

    // Assert
    act.Should().Throw<System.ArgumentNullException>()
      .WithParameterName("report");
  }

  [Test]
  public void EnumerateTypeNodes_EmptyReport_ReturnsEmpty()
  {
    // Arrange
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());
    var enumerator = new MetricsNodeEnumerator(report);

    // Act
    var result = enumerator.EnumerateTypeNodes();

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void EnumerateTypeNodes_SingleType_ReturnsType()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.SampleType", 10, ThresholdStatus.Success);
    var report = MetricsReaderCommandTestData.CreateReport(new[] { typeNode });
    var enumerator = new MetricsNodeEnumerator(report);

    // Act
    var result = enumerator.EnumerateTypeNodes().ToList();

    // Assert
    result.Should().HaveCount(1);
    result[0].FullyQualifiedName.Should().Be("Rca.Loader.Services.SampleType");
  }

  [Test]
  public void EnumerateTypeNodes_MultipleTypes_ReturnsAllTypes()
  {
    // Arrange
    var typeNodes = new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type1", 10, ThresholdStatus.Success),
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type2", 20, ThresholdStatus.Warning),
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type3", 30, ThresholdStatus.Error)
    };
    var report = MetricsReaderCommandTestData.CreateReport(typeNodes);
    var enumerator = new MetricsNodeEnumerator(report);

    // Act
    var result = enumerator.EnumerateTypeNodes().ToList();

    // Assert
    result.Should().HaveCount(3);
    result.Select(t => t.FullyQualifiedName).Should().Contain("Rca.Loader.Services.Type1", "Rca.Loader.Services.Type2", "Rca.Loader.Services.Type3");
  }

  [Test]
  public void EnumerateTypeNodes_MultipleAssemblies_ReturnsAllTypes()
  {
    // Arrange
    var type1 = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type1", 10, ThresholdStatus.Success);
    var type2 = MetricsReaderCommandTestData.CreateTypeNode("Rca.UI.Services.Type2", 20, ThresholdStatus.Warning);

    var report1 = MetricsReaderCommandTestData.CreateReport(new[] { type1 }, null, "Rca.Loader.Services", "Rca.Loader");
    var report2 = MetricsReaderCommandTestData.CreateReport(new[] { type2 }, null, "Rca.UI.Services", "Rca.UI");

    // Create a combined report
    var combinedReport = new MetricsReport
    {
      Metadata = report1.Metadata,
      Solution = new SolutionMetricsNode
      {
        Name = "combined",
        Assemblies = report1.Solution.Assemblies.Concat(report2.Solution.Assemblies).ToList()
      }
    };

    var enumerator = new MetricsNodeEnumerator(combinedReport);

    // Act
    var result = enumerator.EnumerateTypeNodes().ToList();

    // Assert
    result.Should().HaveCount(2);
  }

  [Test]
  public void EnumerateMemberNodes_EmptyReport_ReturnsEmpty()
  {
    // Arrange
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());
    var enumerator = new MetricsNodeEnumerator(report);

    // Act
    var result = enumerator.EnumerateMemberNodes();

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void EnumerateMemberNodes_TypeWithoutMembers_ReturnsEmpty()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.SampleType", 10, ThresholdStatus.Success);
    var report = MetricsReaderCommandTestData.CreateReport(new[] { typeNode });
    var enumerator = new MetricsNodeEnumerator(report);

    // Act
    var result = enumerator.EnumerateMemberNodes().ToList();

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void EnumerateMemberNodes_TypeWithMembers_ReturnsAllMembers()
  {
    // Arrange
    var member1 = MetricsReaderCommandTestData.CreateMemberNode("Rca.Loader.Services.Type.Method1", 5, ThresholdStatus.Success);
    var member2 = MetricsReaderCommandTestData.CreateMemberNode("Rca.Loader.Services.Type.Method2", 15, ThresholdStatus.Warning);
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.Type",
      10,
      ThresholdStatus.Success,
      new[] { member1, member2 });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { typeNode });
    var enumerator = new MetricsNodeEnumerator(report);

    // Act
    var result = enumerator.EnumerateMemberNodes().ToList();

    // Assert
    result.Should().HaveCount(2);
    result.Select(m => m.FullyQualifiedName).Should().Contain("Rca.Loader.Services.Type.Method1", "Rca.Loader.Services.Type.Method2");
  }

  [Test]
  public void EnumerateNodes_TypeKind_ReturnsOnlyTypes()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", 10, ThresholdStatus.Success);
    var report = MetricsReaderCommandTestData.CreateReport(new[] { typeNode });
    var enumerator = new MetricsNodeEnumerator(report);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.RoslynCyclomaticComplexity, MetricsReaderSymbolKind.Type, false);

    // Act
    var result = enumerator.EnumerateNodes(filter).ToList();

    // Assert
    result.Should().HaveCount(1);
    result[0].Kind.Should().Be(CodeElementKind.Type);
  }

  [Test]
  public void EnumerateNodes_MemberKind_ReturnsOnlyMembers()
  {
    // Arrange
    var member = MetricsReaderCommandTestData.CreateMemberNode("Rca.Loader.Services.Type.Method", 5, ThresholdStatus.Success);
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.Type",
      10,
      ThresholdStatus.Success,
      new[] { member });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { typeNode });
    var enumerator = new MetricsNodeEnumerator(report);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.RoslynCyclomaticComplexity, MetricsReaderSymbolKind.Member, false);

    // Act
    var result = enumerator.EnumerateNodes(filter).ToList();

    // Assert
    result.Should().HaveCount(1);
    result[0].Kind.Should().Be(CodeElementKind.Member);
  }

  [Test]
  public void EnumerateNodes_AnyKind_ReturnsTypesAndMembers()
  {
    // Arrange
    var member = MetricsReaderCommandTestData.CreateMemberNode("Rca.Loader.Services.Type.Method", 5, ThresholdStatus.Success);
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.Type",
      10,
      ThresholdStatus.Success,
      new[] { member });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { typeNode });
    var enumerator = new MetricsNodeEnumerator(report);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.RoslynCyclomaticComplexity, MetricsReaderSymbolKind.Any, false);

    // Act
    var result = enumerator.EnumerateNodes(filter).ToList();

    // Assert
    result.Should().HaveCount(2);
    result.Select(n => n.Kind).Should().Contain(new[] { CodeElementKind.Type, CodeElementKind.Member });
  }

  [Test]
  public void EnumerateNodes_NamespaceFilter_FiltersByNamespace()
  {
    // Arrange
    var type1 = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type1", 10, ThresholdStatus.Success);
    var type2 = MetricsReaderCommandTestData.CreateTypeNode("Rca.UI.Services.Type2", 20, ThresholdStatus.Warning);

    var report1 = MetricsReaderCommandTestData.CreateReport(new[] { type1 }, null, "Rca.Loader.Services", "Rca.Loader");
    var report2 = MetricsReaderCommandTestData.CreateReport(new[] { type2 }, null, "Rca.UI.Services", "Rca.UI");

    var combinedReport = new MetricsReport
    {
      Metadata = report1.Metadata,
      Solution = new SolutionMetricsNode
      {
        Name = "combined",
        Assemblies = report1.Solution.Assemblies.Concat(report2.Solution.Assemblies).ToList()
      }
    };

    var enumerator = new MetricsNodeEnumerator(combinedReport);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.RoslynCyclomaticComplexity, MetricsReaderSymbolKind.Type, false);

    // Act
    var result = enumerator.EnumerateNodes(filter).ToList();

    // Assert
    result.Should().HaveCount(1);
    result[0].FullyQualifiedName.Should().Be("Rca.Loader.Services.Type1");
  }

  [Test]
  public void EnumerateNodes_EmptyNamespaceFilter_ReturnsAll()
  {
    // Arrange
    var type1 = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type1", 10, ThresholdStatus.Success);
    var type2 = MetricsReaderCommandTestData.CreateTypeNode("Rca.UI.Services.Type2", 20, ThresholdStatus.Warning);

    var report1 = MetricsReaderCommandTestData.CreateReport(new[] { type1 }, null, "Rca.Loader.Services", "Rca.Loader");
    var report2 = MetricsReaderCommandTestData.CreateReport(new[] { type2 }, null, "Rca.UI.Services", "Rca.UI");

    var combinedReport = new MetricsReport
    {
      Metadata = report1.Metadata,
      Solution = new SolutionMetricsNode
      {
        Name = "combined",
        Assemblies = report1.Solution.Assemblies.Concat(report2.Solution.Assemblies).ToList()
      }
    };

    var enumerator = new MetricsNodeEnumerator(combinedReport);
    var filter = new SymbolFilter(string.Empty, MetricIdentifier.RoslynCyclomaticComplexity, MetricsReaderSymbolKind.Type, false);

    // Act
    var result = enumerator.EnumerateNodes(filter).ToList();

    // Assert
    result.Should().HaveCount(2);
  }

  [Test]
  public void EnumerateNodes_UnknownSymbolKind_ReturnsEmpty()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", 10, ThresholdStatus.Success);
    var report = MetricsReaderCommandTestData.CreateReport(new[] { typeNode });
    var enumerator = new MetricsNodeEnumerator(report);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.RoslynCyclomaticComplexity, (MetricsReaderSymbolKind)999, false); // Unknown kind

    // Act
    var result = enumerator.EnumerateNodes(filter).ToList();

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void EnumerateNodes_NamespaceMismatch_ReturnsEmpty()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", 10, ThresholdStatus.Success);
    var report = MetricsReaderCommandTestData.CreateReport(new[] { typeNode });
    var enumerator = new MetricsNodeEnumerator(report);
    var filter = new SymbolFilter("Rca.UI.Services", MetricIdentifier.RoslynCyclomaticComplexity, MetricsReaderSymbolKind.Type, false); // Different namespace

    // Act
    var result = enumerator.EnumerateNodes(filter).ToList();

    // Assert
    result.Should().BeEmpty();
  }
}


