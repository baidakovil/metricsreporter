namespace MetricsReporter.Tests.Rendering;

using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Verifies that suppressed symbol metadata is reflected in the generated HTML
/// via <c>data-suppressed</c> attribute and justification tooltip.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class HtmlTableGeneratorSuppressedTests
{
  [Test]
  public void Generate_WithSuppressedMetric_MarksCellAsSuppressedAndAddsTooltip()
  {
    // Arrange
    const string typeFqn = "Sample.Namespace.SampleType";

    var typeNode = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = typeFqn,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new()
        {
          Value = 42,
          Status = ThresholdStatus.Warning
        }
      }
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { typeNode }
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

    var metadata = new ReportMetadata
    {
      SuppressedSymbols = new List<SuppressedSymbolInfo>
      {
        new()
        {
          FilePath = "src/Sample.Assembly/SampleType.cs",
          FullyQualifiedName = typeFqn,
          RuleId = "CA1506",
          Metric = nameof(MetricIdentifier.RoslynClassCoupling),
          Justification = "Suppression justification."
        }
      }
    };

    var report = new MetricsReport
    {
      Metadata = metadata,
      Solution = solution
    };

    var generator = new HtmlTableGenerator(new[]
    {
      MetricIdentifier.RoslynClassCoupling
    }, new Dictionary<MetricIdentifier, string?>
    {
      [MetricIdentifier.RoslynClassCoupling] = "score"
    });

    // Act
    var html = generator.Generate(report);

    // Assert
    html.Should().Contain("data-suppressed=\"true\"", "suppressed metric cell must be marked for styling");
    html.Should().Contain("data-suppression-info", "suppression data must be available for tooltip rendering");
    html.Should().Contain("CA1506", "suppression rule ID must be included in data attribute");
    html.Should().Contain("Suppression justification.", "suppression justification must be included in data attribute");
  }
}



