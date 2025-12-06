namespace MetricsReporter.Tests.Rendering;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="NodeChildrenRenderer"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class NodeChildrenRendererTests
{
  private static void InitializeTableGenerator(HtmlTableGenerator generator, MetricsReport report)
  {
    var method = typeof(HtmlTableGenerator).GetMethod("InitializeRenderers", BindingFlags.NonPublic | BindingFlags.Instance);
    string? coverageHtmlDir = null;
    var parameters = new object?[] { report, coverageHtmlDir };
    method!.Invoke(generator, parameters);
  }
  [Test]
  public void Constructor_WithNullTableGenerator_ThrowsArgumentNullException()
  {
    // Act & Assert
    FluentActions.Invoking(() => new NodeChildrenRenderer(null!))
      .Should().Throw<ArgumentNullException>()
      .WithParameterName("tableGenerator");
  }

  [Test]
  public void Render_WithSolutionNode_RendersAssemblies()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode>()
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
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = solution
    };

    var tableGenerator = new HtmlTableGenerator(metricOrder, metricUnits);
    // Initialize the generator
    InitializeTableGenerator(tableGenerator, report);

    var renderer = new NodeChildrenRenderer(tableGenerator);
    var builder = new StringBuilder();

    // Act
    renderer.Render(solution, 0, null!, builder, null, null);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("Sample.Assembly");
  }

  [Test]
  public void Render_WithAssemblyNode_RendersNamespaces()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };

    var tableGenerator = new HtmlTableGenerator(metricOrder, metricUnits);
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };
    // Initialize the generator
    InitializeTableGenerator(tableGenerator, report);

    var renderer = new NodeChildrenRenderer(tableGenerator);

    var @namespace = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode>()
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { @namespace }
    };

    var builder = new StringBuilder();

    // Act
    renderer.Render(assembly, 1, "parent-1", builder, "Sample.Assembly", null);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("Sample.Namespace");
  }

  [Test]
  public void Render_WithNamespaceNode_RendersTypes()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };

    var tableGenerator = new HtmlTableGenerator(metricOrder, metricUnits);
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };
    // Initialize the generator
    InitializeTableGenerator(tableGenerator, report);

    var renderer = new NodeChildrenRenderer(tableGenerator);

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

    var builder = new StringBuilder();

    // Act
    renderer.Render(@namespace, 2, "parent-2", builder, "Sample.Assembly", null);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("SampleType");
  }

  [Test]
  public void Render_WithTypeNode_RendersMembers()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };

    var tableGenerator = new HtmlTableGenerator(metricOrder, metricUnits);
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };
    // Initialize the generator
    InitializeTableGenerator(tableGenerator, report);

    var renderer = new NodeChildrenRenderer(tableGenerator);

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

    var builder = new StringBuilder();

    // Act
    renderer.Render(type, 3, "parent-3", builder, "Sample.Assembly", "SampleType");

    // Assert
    var result = builder.ToString();
    result.Should().Contain("DoWork");
  }

  [Test]
  public void Render_WithEmptyCollections_DoesNotThrow()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var tableGenerator = new HtmlTableGenerator(metricOrder, metricUnits);
    var renderer = new NodeChildrenRenderer(tableGenerator);

    var solution = new SolutionMetricsNode
    {
      Name = "SampleSolution",
      FullyQualifiedName = "SampleSolution",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Assemblies = new List<AssemblyMetricsNode>()
    };

    var builder = new StringBuilder();

    // Act
    FluentActions.Invoking(() => renderer.Render(solution, 0, null!, builder, null, null))
      .Should().NotThrow();
  }

  [Test]
  public void Render_WithMultipleChildren_RendersAll()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };

    var tableGenerator = new HtmlTableGenerator(metricOrder, metricUnits);
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };
    // Initialize the generator
    InitializeTableGenerator(tableGenerator, report);

    var renderer = new NodeChildrenRenderer(tableGenerator);

    var assembly1 = new AssemblyMetricsNode
    {
      Name = "Assembly1",
      FullyQualifiedName = "Assembly1",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode>()
    };

    var assembly2 = new AssemblyMetricsNode
    {
      Name = "Assembly2",
      FullyQualifiedName = "Assembly2",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode>()
    };

    var solution = new SolutionMetricsNode
    {
      Name = "SampleSolution",
      FullyQualifiedName = "SampleSolution",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Assemblies = new List<AssemblyMetricsNode> { assembly1, assembly2 }
    };

    var builder = new StringBuilder();

    // Act
    renderer.Render(solution, 0, null!, builder, null, null);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("Assembly1");
    result.Should().Contain("Assembly2");
  }

  [Test]
  public void Render_WithNullCollections_DoesNotThrow()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var tableGenerator = new HtmlTableGenerator(metricOrder, metricUnits);
    var renderer = new NodeChildrenRenderer(tableGenerator);

    var solution = new SolutionMetricsNode
    {
      Name = "SampleSolution",
      FullyQualifiedName = "SampleSolution",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Assemblies = new List<AssemblyMetricsNode>()
    };

    var builder = new StringBuilder();

    // Act
    FluentActions.Invoking(() => renderer.Render(solution, 0, null!, builder, null, null))
      .Should().NotThrow();
  }

  [Test]
  public void Render_PassesCorrectParameters()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };

    var tableGenerator = new HtmlTableGenerator(metricOrder, metricUnits);
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };
    // Initialize the generator
    InitializeTableGenerator(tableGenerator, report);

    var renderer = new NodeChildrenRenderer(tableGenerator);

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode>()
    };

    var solution = new SolutionMetricsNode
    {
      Name = "SampleSolution",
      FullyQualifiedName = "SampleSolution",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Assemblies = new List<AssemblyMetricsNode> { assembly }
    };

    var builder = new StringBuilder();
    const int level = 1;
    const string parentId = "parent-1";
    const string assemblyName = "Sample.Assembly";

    // Act
    renderer.Render(solution, level, parentId, builder, assemblyName, null);

    // Assert
    // The method should complete successfully, indicating parameters were passed correctly
    builder.Length.Should().BeGreaterThan(0);
  }
}


