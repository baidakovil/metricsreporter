namespace MetricsReporter.Tests.Aggregation;

using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using NUnit.Framework;

[TestFixture]
[Category("Unit")]
public sealed class SarifMetricExtractorTests
{
  // Ensures elements without a source path or metrics are rejected before metric extraction.
  [Test]
  public void IsValidElement_MissingSourceOrMetrics_ReturnsFalse()
  {
    // Arrange
    var isValid = GetExtractorMethod("IsValidElement");
    var noSource = CreateElement(metrics: CreateMetrics(1m), source: null);
    var emptyMetrics = CreateElement(metrics: new Dictionary<MetricIdentifier, MetricValue>(), source: new SourceLocation { Path = "file.cs" });

    // Act
    var nullSourceResult = (bool)isValid.Invoke(null, new object?[] { noSource })!;
    var emptyMetricsResult = (bool)isValid.Invoke(null, new object?[] { emptyMetrics })!;

    // Assert
    nullSourceResult.Should().BeFalse();
    emptyMetricsResult.Should().BeFalse();
  }

  // Confirms valid elements with paths and metrics pass validation.
  [Test]
  public void IsValidElement_WithPathAndMetrics_ReturnsTrue()
  {
    // Arrange
    var isValid = GetExtractorMethod("IsValidElement");
    var element = CreateElement(metrics: CreateMetrics(2m), source: new SourceLocation { Path = "file.cs" });

    // Act
    var result = (bool)isValid.Invoke(null, new object?[] { element })!;

    // Assert
    result.Should().BeTrue();
  }

  // Verifies the extractor skips metrics with null values so empty coverage entries do not leak through.
  [Test]
  public void ExtractFirstMetric_NullMetric_ReturnsNull()
  {
    // Arrange
    var extractFirstMetric = GetExtractorMethod("ExtractFirstMetric");
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.AltCoverSequenceCoverage] = new MetricValue { Value = null },
      [MetricIdentifier.AltCoverBranchCoverage] = new MetricValue { Value = 25m }
    };
    var element = CreateElement(metrics: metrics, source: new SourceLocation { Path = "file.cs" });

    // Act
    var firstMetric = (KeyValuePair<MetricIdentifier, MetricValue>?)extractFirstMetric.Invoke(null, new object?[] { element });

    // Assert
    firstMetric.Should().BeNull();
  }

  // Confirms the first populated metric is returned with its value when present.
  [Test]
  public void ExtractFirstMetric_WithValue_ReturnsPopulatedMetric()
  {
    // Arrange
    var extractFirstMetric = GetExtractorMethod("ExtractFirstMetric");
    var element = CreateElement(metrics: CreateMetrics(10m), source: new SourceLocation { Path = "file.cs" });

    // Act
    var result = (KeyValuePair<MetricIdentifier, MetricValue>?)extractFirstMetric.Invoke(null, new object?[] { element });

    // Assert
    result.Should().NotBeNull();
    result!.Value.Key.Should().Be(MetricIdentifier.AltCoverSequenceCoverage);
    result.Value.Value.Value.Should().Be(10m);
  }

  // Ensures line resolution prefers the start line when available and falls back to end line otherwise.
  [Test]
  public void GetLineFromSource_PrefersStartLine_UsesEndLineWhenMissing()
  {
    // Arrange
    var getLine = GetExtractorMethod("GetLineFromSource");
    var withStart = new SourceLocation { Path = "file.cs", StartLine = 5, EndLine = 10 };
    var withoutStart = new SourceLocation { Path = "file.cs", StartLine = null, EndLine = 15 };

    // Act
    var startLine = (int?)getLine.Invoke(null, new object?[] { withStart });
    var endLine = (int?)getLine.Invoke(null, new object?[] { withoutStart });

    // Assert
    startLine.Should().Be(5);
    endLine.Should().Be(15);
  }

  private static MethodInfo GetExtractorMethod(string name)
  {
    var extractorType = typeof(SarifMetricsApplier).GetNestedType("SarifMetricExtractor", BindingFlags.NonPublic);
    extractorType.Should().NotBeNull();
    var method = extractorType!.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
    method.Should().NotBeNull();
    return method!;
  }

  private static ParsedCodeElement CreateElement(
      IDictionary<MetricIdentifier, MetricValue> metrics,
      SourceLocation? source)
  {
    return new ParsedCodeElement(CodeElementKind.Member, "Name", "Fqn")
    {
      Source = source,
      Metrics = metrics
    };
  }

  private static Dictionary<MetricIdentifier, MetricValue> CreateMetrics(decimal value)
  {
    return new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.AltCoverSequenceCoverage] = new MetricValue { Value = value }
    };
  }
}

