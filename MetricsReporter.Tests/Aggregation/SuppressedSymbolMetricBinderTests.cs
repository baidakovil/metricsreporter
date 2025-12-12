namespace MetricsReporter.Tests.Aggregation;

using System.Collections.Generic;
using FluentAssertions;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using NUnit.Framework;

[TestFixture]
[Category("Unit")]
public sealed class SuppressedSymbolMetricBinderTests
{
  // Ensures binding exits quickly when no suppressed symbols are provided.
  [Test]
  public void Bind_NoSuppressedSymbols_DoesNothing()
  {
    // Arrange
    var solution = CreateSolutionWithMember("Sample.Namespace.Type.Member()", MetricIdentifier.SarifIdeRuleViolations);

    // Act
    SuppressedSymbolMetricBinder.Bind(solution, new List<SuppressedSymbolInfo>());

    // Assert
    solution.Assemblies.Should().NotBeEmpty();
  }

  // Verifies entries with missing names or already known metrics are skipped without lookup.
  [Test]
  public void Bind_IgnoresWhitespaceNamesOrKnownMetrics()
  {
    // Arrange
    var solution = CreateSolutionWithMember("Sample.Namespace.Type.Member()", MetricIdentifier.SarifIdeRuleViolations);
    var suppressed = new List<SuppressedSymbolInfo>
    {
      new() { FullyQualifiedName = " ", RuleId = "CA1000" },
      new() { FullyQualifiedName = "Sample.Namespace.Type.Member()", Metric = "SarifCaRuleViolations", RuleId = "CA1000" }
    };

    // Act
    SuppressedSymbolMetricBinder.Bind(solution, suppressed);

    // Assert
    suppressed[1].Metric.Should().Be("SarifCaRuleViolations");
  }

  // Confirms entries that cannot be resolved to nodes leave the metric unchanged.
  [Test]
  public void Bind_NodeNotFound_MetricRemainsEmpty()
  {
    // Arrange
    var solution = CreateSolutionWithMember("Sample.Namespace.Type.Member()", MetricIdentifier.SarifIdeRuleViolations);
    var suppressed = new List<SuppressedSymbolInfo>
    {
      new() { FullyQualifiedName = "Sample.Namespace.Missing.Member()", RuleId = "IDE0001" }
    };

    // Act
    SuppressedSymbolMetricBinder.Bind(solution, suppressed);

    // Assert
    suppressed[0].Metric.Should().BeEmpty();
  }

  // Ensures metrics are bound when the rule can be resolved for a known node.
  [Test]
  public void Bind_ResolvesMetricForKnownRule_SetsMetricName()
  {
    // Arrange
    var memberFqn = "Sample.Namespace.Type.Member()";
    var solution = CreateSolutionWithMember(memberFqn, MetricIdentifier.SarifIdeRuleViolations);
    var suppressed = new List<SuppressedSymbolInfo>
    {
      new() { FullyQualifiedName = memberFqn, RuleId = "IDE0051" }
    };

    // Act
    SuppressedSymbolMetricBinder.Bind(solution, suppressed);

    // Assert
    suppressed[0].Metric.Should().Be(MetricIdentifier.SarifIdeRuleViolations.ToString());
  }

  private static SolutionMetricsNode CreateSolutionWithMember(string memberFqn, MetricIdentifier metricIdentifier)
  {
    var member = new MemberMetricsNode
    {
      Name = memberFqn,
      FullyQualifiedName = memberFqn,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [metricIdentifier] = new MetricValue { Value = 1 }
      }
    };

    var type = new TypeMetricsNode
    {
      Name = "Sample.Namespace.Type",
      FullyQualifiedName = "Sample.Namespace.Type",
      Members = new List<MemberMetricsNode> { member }
    };

    var ns = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
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
      Name = "Sample.Solution",
      FullyQualifiedName = "Sample.Solution",
      Assemblies = new List<AssemblyMetricsNode> { assembly }
    };
  }
}

