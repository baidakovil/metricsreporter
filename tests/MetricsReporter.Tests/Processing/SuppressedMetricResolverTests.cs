namespace MetricsReporter.Tests.Processing;

using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;

[TestFixture]
[Category("Unit")]
public sealed class SuppressedMetricResolverTests
{
  [Test]
  public void TryResolve_PrefersRuleIdMetric()
  {
    var node = CreateNodeWithMetric(MetricIdentifier.SarifIdeRuleViolations);

    SuppressedMetricResolver.TryResolve(node, "IDE0028", out var identifier).Should().BeTrue();
    identifier.Should().Be(MetricIdentifier.SarifIdeRuleViolations);
  }

  [Test]
  public void TryResolve_FallsBackWhenPreferredUnavailable()
  {
    var node = CreateNodeWithMetric(MetricIdentifier.SarifCaRuleViolations);

    SuppressedMetricResolver.TryResolve(node, "IDE0028", out var identifier).Should().BeTrue();
    identifier.Should().Be(MetricIdentifier.SarifCaRuleViolations);
  }

  [Test]
  public void IsKnownMetric_ReturnsTrueForValidMetricName()
    => SuppressedMetricResolver.IsKnownMetric("SarifIdeRuleViolations").Should().BeTrue();

  private static MetricsNode CreateNodeWithMetric(MetricIdentifier metric)
  {
    var member = new MemberMetricsNode
    {
      Name = "SampleMember",
      FullyQualifiedName = "Sample.Namespace.SampleMember"
    };
    member.Metrics[metric] = new MetricValue { Value = 1m };
    return member;
  }
}


