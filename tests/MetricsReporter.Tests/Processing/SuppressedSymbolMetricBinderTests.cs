namespace MetricsReporter.Tests.Processing;

using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;

[TestFixture]
[Category("Unit")]
public sealed class SuppressedSymbolMetricBinderTests
{
  private const string MemberFullyQualifiedName = "Sample.Namespace.SampleType.SuppressedSymbols";
  [Test]
  public void Bind_AssignsMetricWhenNodeProvidesSarifMetric()
  {
    var solution = CreateSolutionWithMember(out var member);
    member.Metrics[MetricIdentifier.SarifIdeRuleViolations] = new MetricValue { Value = 1m };

    var suppressed = new List<SuppressedSymbolInfo>
    {
      new()
      {
        FullyQualifiedName = MemberFullyQualifiedName,
        RuleId = "IDE0028"
      }
    };

    SuppressedSymbolMetricBinder.Bind(solution, suppressed);

    suppressed[0].Metric.Should().Be(MetricIdentifier.SarifIdeRuleViolations.ToString());
  }

  [Test]
  public void Bind_DoesNotOverwriteValidMetric()
  {
    var solution = CreateSolutionWithMember(out var member);
    member.Metrics[MetricIdentifier.SarifIdeRuleViolations] = new MetricValue { Value = 1m };

    var suppressed = new List<SuppressedSymbolInfo>
    {
      new()
      {
        FullyQualifiedName = MemberFullyQualifiedName,
        RuleId = "IDE0028",
        Metric = MetricIdentifier.SarifIdeRuleViolations.ToString()
      }
    };

    SuppressedSymbolMetricBinder.Bind(solution, suppressed);

    suppressed[0].Metric.Should().Be(MetricIdentifier.SarifIdeRuleViolations.ToString());
  }

  private static SolutionMetricsNode CreateSolutionWithMember(out MemberMetricsNode member)
  {
    var solution = new SolutionMetricsNode
    {
      Name = "Root",
      FullyQualifiedName = "Root"
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly"
    };

    var ns = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace"
    };

    var type = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType"
    };

    member = new MemberMetricsNode
    {
      Name = "SuppressedProperty",
      FullyQualifiedName = MemberFullyQualifiedName,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    type.Members.Add(member);
    ns.Types.Add(type);
    assembly.Namespaces.Add(ns);
    solution.Assemblies.Add(assembly);

    return solution;
  }
}


