namespace MetricsReporter.Tests.Aggregation;

using System.Collections.Generic;
using FluentAssertions;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using NUnit.Framework;
using System.Reflection;

[TestFixture]
[Category("Unit")]
public sealed class SuppressedMetricResolverTests
{
  // Ensures preferred metrics derived from rule IDs are selected when the node contains the matching metric.
  [Test]
  public void TryResolve_PreferredMetricPresent_ReturnsPreferred()
  {
    // Arrange
    var node = CreateNode(new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifIdeRuleViolations] = new MetricValue { Value = 1 }
    });

    // Act
    var result = SuppressedMetricResolver.TryResolve(node, "IDE0051", out var identifier);

    // Assert
    result.Should().BeTrue();
    identifier.Should().Be(MetricIdentifier.SarifIdeRuleViolations);
  }

  // Verifies resolution falls back to available SARIF metrics when the preferred metric is missing or empty.
  [Test]
  public void TryResolve_PreferredMetricMissing_UsesFallback()
  {
    // Arrange
    var node = CreateNode(new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifIdeRuleViolations] = new MetricValue { Value = null },
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue { Value = 2 }
    });

    // Act
    var result = SuppressedMetricResolver.TryResolve(node, "IDE0001", out var identifier);

    // Assert
    result.Should().BeTrue();
    identifier.Should().Be(MetricIdentifier.SarifCaRuleViolations);
  }

  // Confirms resolution fails cleanly when neither preferred nor fallback metrics are present.
  [Test]
  public void TryResolve_NoMatchingMetrics_ReturnsFalse()
  {
    // Arrange
    var node = CreateNode(new Dictionary<MetricIdentifier, MetricValue>());

    // Act
    var result = SuppressedMetricResolver.TryResolve(node, "CA1000", out var identifier);

    // Assert
    result.Should().BeFalse();
    identifier.Should().Be(default);
  }

  // Checks metric name validation handles known and unknown identifiers.
  [Test]
  public void IsKnownMetric_ValidatesMetricNames()
  {
    // Act & Assert
    SuppressedMetricResolver.IsKnownMetric("SarifCaRuleViolations").Should().BeTrue();
    SuppressedMetricResolver.IsKnownMetric("  ").Should().BeFalse();
    SuppressedMetricResolver.IsKnownMetric("UnknownMetric").Should().BeFalse();
  }

  // Covers preferred metric selection when rule identifiers are null, empty, or unknown.
  [Test]
  public void GetPreferredMetric_NullOrUnknown_ReturnsNull()
  {
    // Arrange
    var method = typeof(SuppressedMetricResolver).GetMethod("GetPreferredMetric", BindingFlags.NonPublic | BindingFlags.Static);
    method.Should().NotBeNull();

    // Act & Assert
    method!.Invoke(null, new object?[] { null }).Should().BeNull();
    method.Invoke(null, new object?[] { " " }).Should().BeNull();
    method.Invoke(null, new object?[] { "RS001" }).Should().BeNull();
  }

  // Ensures metrics present with null values are treated as absent.
  [Test]
  public void NodeHasMetric_NullValue_ReturnsFalse()
  {
    // Arrange
    var method = typeof(SuppressedMetricResolver).GetMethod("NodeHasMetric", BindingFlags.NonPublic | BindingFlags.Static);
    method.Should().NotBeNull();
    var node = CreateNode(new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new() { Value = null }
    });

    // Act
    var result = (bool)method!.Invoke(null, new object?[] { node, MetricIdentifier.SarifCaRuleViolations })!;

    // Assert
    result.Should().BeFalse();
  }

  // Verifies missing metric entries return false without populating output.
  [Test]
  public void NodeHasMetric_MissingMetric_ReturnsFalse()
  {
    // Arrange
    var method = typeof(SuppressedMetricResolver).GetMethod("NodeHasMetric", BindingFlags.NonPublic | BindingFlags.Static);
    method.Should().NotBeNull();
    var node = CreateNode(new Dictionary<MetricIdentifier, MetricValue>());

    // Act
    var result = (bool)method!.Invoke(null, new object?[] { node, MetricIdentifier.SarifIdeRuleViolations })!;

    // Assert
    result.Should().BeFalse();
  }

  // Ensures metrics with null container entries are treated as absent.
  [Test]
  public void NodeHasMetric_NullEntry_ReturnsFalse()
  {
    // Arrange
    var method = typeof(SuppressedMetricResolver).GetMethod("NodeHasMetric", BindingFlags.NonPublic | BindingFlags.Static);
    method.Should().NotBeNull();
    var node = CreateNode(new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifIdeRuleViolations] = null!
    });

    // Act
    var result = (bool)method!.Invoke(null, new object?[] { node, MetricIdentifier.SarifIdeRuleViolations })!;

    // Assert
    result.Should().BeFalse();
  }

  // Confirms populated metrics return true to drive resolver selection.
  [Test]
  public void NodeHasMetric_WithValue_ReturnsTrue()
  {
    // Arrange
    var method = typeof(SuppressedMetricResolver).GetMethod("NodeHasMetric", BindingFlags.NonPublic | BindingFlags.Static);
    method.Should().NotBeNull();
    var node = CreateNode(new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new() { Value = 3 }
    });

    // Act
    var result = (bool)method!.Invoke(null, new object?[] { node, MetricIdentifier.SarifCaRuleViolations })!;

    // Assert
    result.Should().BeTrue();
  }

  private static MetricsNode CreateNode(IDictionary<MetricIdentifier, MetricValue> metrics)
  {
    return new MemberMetricsNode
    {
      Name = "Node",
      FullyQualifiedName = "Node",
      Metrics = metrics
    };
  }
}

