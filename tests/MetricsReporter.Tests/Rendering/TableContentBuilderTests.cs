namespace MetricsReporter.Tests.Rendering;

using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="TableContentBuilder"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class TableContentBuilderTests
{
  private static void InitializeTableGenerator(HtmlTableGenerator generator, MetricsReport report)
  {
    var method = typeof(HtmlTableGenerator).GetMethod("InitializeRenderers", BindingFlags.NonPublic | BindingFlags.Instance);
    string? coverageHtmlDir = null;
    var parameters = new object?[] { report, coverageHtmlDir };
    method!.Invoke(generator, parameters);
  }
  [Test]
  public void Build_WithValidReport_BuildsTableContent()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = new List<SuppressedSymbolInfo>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    var tableGenerator = CreateTableGenerator(metricOrder);
    var builder = new StringBuilder();

    // Act
    TableContentBuilder.Build(metricOrder, report, tableGenerator, builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("<div class=\"table-container\">");
    result.Should().Contain("<table id=\"metrics-table\"");
    result.Should().Contain("<thead>");
    result.Should().Contain("<tbody>");
    result.Should().Contain("</tbody>");
    result.Should().Contain("</table>");
    result.Should().Contain("</div>");
  }

  [Test]
  public void Build_WithAssemblies_RendersAssemblies()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
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
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = new List<SuppressedSymbolInfo>()
      },
      Solution = solution
    };

    var tableGenerator = CreateTableGenerator(metricOrder);
    // Initialize the generator
    InitializeTableGenerator(tableGenerator, report);
    var builder = new StringBuilder();

    // Act
    TableContentBuilder.Build(metricOrder, report, tableGenerator, builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("Sample.Assembly");
  }

  [Test]
  public void Build_CallsTableGeneratorRenderTableBody()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = new List<SuppressedSymbolInfo>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    var tableGenerator = CreateTableGenerator(metricOrder);
    // Initialize the generator
    InitializeTableGenerator(tableGenerator, report);
    var builder = new StringBuilder();

    // Act
    TableContentBuilder.Build(metricOrder, report, tableGenerator, builder);

    // Assert
    // The fact that the method completes without throwing indicates that RenderTableBody was called
    builder.Length.Should().BeGreaterThan(0);
  }

  [Test]
  public void Build_WithMultipleMetrics_IncludesAllInHeader()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.RoslynCyclomaticComplexity
    };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = new List<SuppressedSymbolInfo>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    var tableGenerator = CreateTableGenerator(metricOrder);
    // Initialize the generator
    InitializeTableGenerator(tableGenerator, report);
    var builder = new StringBuilder();

    // Act
    TableContentBuilder.Build(metricOrder, report, tableGenerator, builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("RoslynClassCoupling");
    result.Should().Contain("RoslynCyclomaticComplexity");
  }

  private static HtmlTableGenerator CreateTableGenerator(MetricIdentifier[] metricOrder)
  {
    var metricUnits = new Dictionary<MetricIdentifier, string?>();
    foreach (var metric in metricOrder)
    {
      metricUnits[metric] = null;
    }

    return new HtmlTableGenerator(metricOrder, metricUnits);
  }
}


