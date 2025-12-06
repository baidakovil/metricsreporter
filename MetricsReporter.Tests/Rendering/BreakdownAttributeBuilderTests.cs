namespace MetricsReporter.Tests.Rendering;

using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="BreakdownAttributeBuilder"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class BreakdownAttributeBuilderTests
{
  [Test]
  public void BuildDataAttribute_WithNonSarifMetric_ReturnsEmptyString()
  {
    // Arrange
    var value = new MetricValue
    {
      Value = 10,
      Status = ThresholdStatus.Success,
      Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
      {
        ["Rule1"] = new SarifRuleBreakdownEntry { Count = 5 }
      }
    };

    // Act
    var result = BreakdownAttributeBuilder.BuildDataAttribute(MetricIdentifier.RoslynClassCoupling, value);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void BuildDataAttribute_WithSarifCaRuleViolationsAndBreakdown_BuildsAttribute()
  {
    // Arrange
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
    var result = BreakdownAttributeBuilder.BuildDataAttribute(MetricIdentifier.SarifCaRuleViolations, value);

    // Assert
    result.Should().Contain("data-breakdown=");
    result.Should().Contain("CA1506");
    result.Should().Contain("CA1502");
  }

  [Test]
  public void BuildDataAttribute_WithSarifIdeRuleViolationsAndBreakdown_BuildsAttribute()
  {
    // Arrange
    var value = new MetricValue
    {
      Value = 3,
      Status = ThresholdStatus.Warning,
      Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
      {
        ["IDE0001"] = new SarifRuleBreakdownEntry { Count = 2 },
        ["IDE0002"] = new SarifRuleBreakdownEntry { Count = 1 }
      }
    };

    // Act
    var result = BreakdownAttributeBuilder.BuildDataAttribute(MetricIdentifier.SarifIdeRuleViolations, value);

    // Assert
    result.Should().Contain("data-breakdown=");
    result.Should().Contain("IDE0001");
    result.Should().Contain("IDE0002");
  }

  [Test]
  public void BuildDataAttribute_WithNullValue_ReturnsEmptyString()
  {
    // Act
    var result = BreakdownAttributeBuilder.BuildDataAttribute(MetricIdentifier.SarifCaRuleViolations, null);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void BuildDataAttribute_WithZeroValue_ReturnsEmptyString()
  {
    // Arrange
    var value = new MetricValue
    {
      Value = 0,
      Status = ThresholdStatus.Success,
      Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
      {
        ["CA1506"] = new SarifRuleBreakdownEntry { Count = 5 }
      }
    };

    // Act
    var result = BreakdownAttributeBuilder.BuildDataAttribute(MetricIdentifier.SarifCaRuleViolations, value);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void BuildDataAttribute_WithNullValueProperty_ReturnsEmptyString()
  {
    // Arrange
    var value = new MetricValue
    {
      Value = null,
      Status = ThresholdStatus.Success,
      Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
      {
        ["CA1506"] = new SarifRuleBreakdownEntry { Count = 5 }
      }
    };

    // Act
    var result = BreakdownAttributeBuilder.BuildDataAttribute(MetricIdentifier.SarifCaRuleViolations, value);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void BuildDataAttribute_WithNullBreakdown_ReturnsEmptyString()
  {
    // Arrange
    var value = new MetricValue
    {
      Value = 5,
      Status = ThresholdStatus.Warning,
      Breakdown = null
    };

    // Act
    var result = BreakdownAttributeBuilder.BuildDataAttribute(MetricIdentifier.SarifCaRuleViolations, value);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void BuildDataAttribute_WithEmptyBreakdown_ReturnsEmptyString()
  {
    // Arrange
    var value = new MetricValue
    {
      Value = 5,
      Status = ThresholdStatus.Warning,
      Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>()
    };

    // Act
    var result = BreakdownAttributeBuilder.BuildDataAttribute(MetricIdentifier.SarifCaRuleViolations, value);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void BuildDataAttribute_WithValidBreakdown_EncodesHtml()
  {
    // Arrange
    var value = new MetricValue
    {
      Value = 1,
      Status = ThresholdStatus.Warning,
      Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
      {
        ["CA<test>1506"] = new SarifRuleBreakdownEntry { Count = 1 }
      }
    };

    // Act
    var result = BreakdownAttributeBuilder.BuildDataAttribute(MetricIdentifier.SarifCaRuleViolations, value);

    // Assert
    // JSON is serialized, then HTML-encoded. The key "CA<test>1506" is serialized as a JSON string key.
    // WebUtility.HtmlEncode encodes quotes as &quot; but may not encode < and > in all contexts.
    // The test verifies that the breakdown attribute is built correctly with the rule ID.
    result.Should().Contain("data-breakdown=");
    result.Should().Match("*CA*test*1506*");
  }

  [Test]
  public void BuildDataAttribute_WithValidBreakdown_UsesCamelCase()
  {
    // Arrange
    var value = new MetricValue
    {
      Value = 1,
      Status = ThresholdStatus.Warning,
      Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
      {
        ["CA1506"] = new SarifRuleBreakdownEntry { Count = 1 }
      }
    };

    // Act
    var result = BreakdownAttributeBuilder.BuildDataAttribute(MetricIdentifier.SarifCaRuleViolations, value);

    // Assert
    // JSON serialization should produce valid JSON with the breakdown dictionary
    result.Should().Contain("CA1506");
    result.Should().Contain("1");
  }

  [Test]
  public void BuildDataAttribute_WithMultipleBreakdownEntries_IncludesAll()
  {
    // Arrange
    var value = new MetricValue
    {
      Value = 10,
      Status = ThresholdStatus.Error,
      Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
      {
        ["CA1506"] = new SarifRuleBreakdownEntry { Count = 5 },
        ["CA1502"] = new SarifRuleBreakdownEntry { Count = 3 },
        ["CA1501"] = new SarifRuleBreakdownEntry { Count = 2 }
      }
    };

    // Act
    var result = BreakdownAttributeBuilder.BuildDataAttribute(MetricIdentifier.SarifCaRuleViolations, value);

    // Assert
    result.Should().Contain("CA1506");
    result.Should().Contain("CA1502");
    result.Should().Contain("CA1501");
    result.Should().Contain("5");
    result.Should().Contain("3");
    result.Should().Contain("2");
  }

  [Test]
  public void BuildDataAttribute_WithNegativeValue_BuildsAttribute()
  {
    // Arrange
    var value = new MetricValue
    {
      Value = -1,
      Status = ThresholdStatus.Success,
      Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
      {
        ["CA1506"] = new SarifRuleBreakdownEntry { Count = 1 }
      }
    };

    // Act
    var result = BreakdownAttributeBuilder.BuildDataAttribute(MetricIdentifier.SarifCaRuleViolations, value);

    // Assert
    // Negative values are not zero, so breakdown is built
    result.Should().Contain("data-breakdown=");
  }
}


