namespace MetricsReporter.Tests.Processing;

using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Processing;

/// <summary>
/// Tests for <see cref="SuppressedRuleMetricMapper"/> that validate the mapping
/// between CA rule identifiers and Roslyn metric identifiers.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class SuppressedRuleMetricMapperTests
{
  [Test]
  public void TryGetMetricIdentifier_KnownRules_ReturnsExpectedMetrics()
  {
    SuppressedRuleMetricMapper.TryGetMetricIdentifier("CA1505", out var metric1505).Should().BeTrue();
    metric1505.Should().Be(MetricIdentifier.RoslynMaintainabilityIndex);

    SuppressedRuleMetricMapper.TryGetMetricIdentifier("CA1502", out var metric1502).Should().BeTrue();
    metric1502.Should().Be(MetricIdentifier.RoslynCyclomaticComplexity);

    SuppressedRuleMetricMapper.TryGetMetricIdentifier("CA1506", out var metric1506).Should().BeTrue();
    metric1506.Should().Be(MetricIdentifier.RoslynClassCoupling);

    SuppressedRuleMetricMapper.TryGetMetricIdentifier("CA1501", out var metric1501).Should().BeTrue();
    metric1501.Should().Be(MetricIdentifier.RoslynDepthOfInheritance);
  }

  [Test]
  public void TryGetMetricName_FullCheckIdWithDescription_IsRecognized()
  {
    // WHY: In real SuppressMessage attributes the second argument often contains
    // both rule identifier and description in the form "CA1506:Avoid excessive class coupling".
    // The mapper is expected to handle this full string, not only the bare "CA1506".
    var checkId = "CA1506:Avoid excessive class coupling";

    SuppressedRuleMetricMapper.TryGetMetricName(checkId, out var metricName).Should().BeTrue();
    metricName.Should().Be(nameof(MetricIdentifier.RoslynClassCoupling));
  }
}



