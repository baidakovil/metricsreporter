namespace MetricsReporter.Tests.Rendering;

using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="MetricCellAttributeBuilder"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class MetricCellAttributeBuilderTests
{
  [Test]
  public void BuildAttributes_WithNullValue_ReturnsNaStatus()
  {
    // Arrange
    var builder = new MetricCellAttributeBuilder(null);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, null);

    // Assert
    status.Should().Be("na");
    hasDelta.Should().BeFalse();
    suppressedAttr.Should().BeEmpty();
    suppressionDataAttr.Should().BeEmpty();
    breakdownAttr.Should().BeEmpty();
  }

  [Test]
  public void BuildAttributes_WithErrorStatus_ReturnsErrorStatus()
  {
    // Arrange
    var builder = new MetricCellAttributeBuilder(null);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 50,
      Status = ThresholdStatus.Error
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    status.Should().Be("error");
    hasDelta.Should().BeFalse();
  }

  [Test]
  public void BuildAttributes_WithWarningStatus_ReturnsWarningStatus()
  {
    // Arrange
    var builder = new MetricCellAttributeBuilder(null);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 30,
      Status = ThresholdStatus.Warning
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    status.Should().Be("warning");
  }

  [Test]
  public void BuildAttributes_WithOkStatus_ReturnsOkStatus()
  {
    // Arrange
    var builder = new MetricCellAttributeBuilder(null);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 10,
      Status = ThresholdStatus.Success
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    status.Should().Be("success");
  }

  [Test]
  public void BuildAttributes_WithPositiveDelta_SetsHasDelta()
  {
    // Arrange
    var builder = new MetricCellAttributeBuilder(null);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 25,
      Status = ThresholdStatus.Success,
      Delta = 5
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    hasDelta.Should().BeTrue();
  }

  [Test]
  public void BuildAttributes_WithNegativeDelta_SetsHasDelta()
  {
    // Arrange
    var builder = new MetricCellAttributeBuilder(null);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 20,
      Status = ThresholdStatus.Success,
      Delta = -5
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    hasDelta.Should().BeTrue();
  }

  [Test]
  public void BuildAttributes_WithZeroDelta_DoesNotSetHasDelta()
  {
    // Arrange
    var builder = new MetricCellAttributeBuilder(null);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 25,
      Status = ThresholdStatus.Success,
      Delta = 0
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    hasDelta.Should().BeFalse();
  }

  [Test]
  public void BuildAttributes_WithNullDelta_DoesNotSetHasDelta()
  {
    // Arrange
    var builder = new MetricCellAttributeBuilder(null);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 25,
      Status = ThresholdStatus.Success,
      Delta = null
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    hasDelta.Should().BeFalse();
  }

  [Test]
  public void BuildAttributes_WithSuppressedMetric_BuildsSuppressedAttribute()
  {
    // Arrange
    var suppressedIndex = new Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>
    {
      [("Sample.Namespace.SampleType", MetricIdentifier.RoslynClassCoupling)] = new SuppressedSymbolInfo
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "Justified"
      }
    };

    var builder = new MetricCellAttributeBuilder(suppressedIndex);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 50,
      Status = ThresholdStatus.Error
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    suppressedAttr.Should().Be(" data-suppressed=\"true\"");
    suppressionDataAttr.Should().Contain("data-suppression-info=");
    suppressionDataAttr.Should().Contain("CA1506");
  }

  [Test]
  public void BuildAttributes_WithNonSuppressedMetric_DoesNotBuildSuppressedAttribute()
  {
    // Arrange
    var suppressedIndex = new Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>();
    var builder = new MetricCellAttributeBuilder(suppressedIndex);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 50,
      Status = ThresholdStatus.Error
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    suppressedAttr.Should().BeEmpty();
    suppressionDataAttr.Should().BeEmpty();
  }

  [Test]
  public void BuildAttributes_WithNullFqn_DoesNotFindSuppression()
  {
    // Arrange
    var suppressedIndex = new Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>
    {
      [("Sample.Namespace.SampleType", MetricIdentifier.RoslynClassCoupling)] = new SuppressedSymbolInfo
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "Justified"
      }
    };

    var builder = new MetricCellAttributeBuilder(suppressedIndex);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = null,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 50,
      Status = ThresholdStatus.Error
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    suppressedAttr.Should().BeEmpty();
    suppressionDataAttr.Should().BeEmpty();
  }

  [Test]
  public void BuildAttributes_WithSarifMetricAndBreakdown_BuildsBreakdownAttribute()
  {
    // Arrange
    var builder = new MetricCellAttributeBuilder(null);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 5,
      Status = ThresholdStatus.Warning,
      Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
      {
        ["CA1506"] = new SarifRuleBreakdownEntry { Count = 3 },
        ["CA1502"] = new SarifRuleBreakdownEntry { Count = 2 }
      }
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.SarifCaRuleViolations, value);

    // Assert
    breakdownAttr.Should().Contain("data-breakdown=");
    breakdownAttr.Should().Contain("CA1506");
    breakdownAttr.Should().Contain("CA1502");
  }

  [Test]
  public void BuildAttributes_WithNonSarifMetric_DoesNotBuildBreakdownAttribute()
  {
    // Arrange
    var builder = new MetricCellAttributeBuilder(null);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 50,
      Status = ThresholdStatus.Error,
      Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
      {
        ["CA1506"] = new SarifRuleBreakdownEntry { Count = 50 }
      }
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    breakdownAttr.Should().BeEmpty();
  }

  [Test]
  public void BuildAttributes_WithAllAttributes_ReturnsAll()
  {
    // Arrange
    var suppressedIndex = new Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>
    {
      [("Sample.Namespace.SampleType", MetricIdentifier.SarifCaRuleViolations)] = new SuppressedSymbolInfo
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "SarifCaRuleViolations",
        RuleId = "CA1506",
        Justification = "Justified"
      }
    };

    var builder = new MetricCellAttributeBuilder(suppressedIndex);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 5,
      Status = ThresholdStatus.Warning,
      Delta = 2,
      Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
      {
        ["CA1506"] = new SarifRuleBreakdownEntry { Count = 3 },
        ["CA1502"] = new SarifRuleBreakdownEntry { Count = 2 }
      }
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.SarifCaRuleViolations, value);

    // Assert
    status.Should().Be("warning");
    hasDelta.Should().BeTrue();
    suppressedAttr.Should().Be(" data-suppressed=\"true\"");
    suppressionDataAttr.Should().Contain("data-suppression-info=");
    breakdownAttr.Should().Contain("data-breakdown=");
  }

  [Test]
  public void BuildAttributes_WithSuppressionForDifferentMetric_DoesNotSuppress()
  {
    // Arrange
    var suppressedIndex = new Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>
    {
      [("Sample.Namespace.SampleType", MetricIdentifier.RoslynCyclomaticComplexity)] = new SuppressedSymbolInfo
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "RoslynCyclomaticComplexity",
        RuleId = "CA1502",
        Justification = "Justified"
      }
    };

    var builder = new MetricCellAttributeBuilder(suppressedIndex);
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var value = new MetricValue
    {
      Value = 50,
      Status = ThresholdStatus.Error
    };

    // Act
    var (status, hasDelta, suppressedAttr, suppressionDataAttr, breakdownAttr) =
      builder.BuildAttributes(node, MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    suppressedAttr.Should().BeEmpty();
    suppressionDataAttr.Should().BeEmpty();
  }
}


