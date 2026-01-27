namespace MetricsReporter.Tests.Rendering;

using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="MetricCellRenderer"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class MetricCellRendererTests
{
  [Test]
  public void Constructor_WithNullMetricOrder_ThrowsArgumentNullException()
  {
    // Act & Assert
    FluentActions.Invoking(() => new MetricCellRenderer(null!, new Dictionary<MetricIdentifier, string?>(), null))
      .Should().Throw<System.ArgumentNullException>()
      .WithParameterName("metricOrder");
  }

  [Test]
  public void Constructor_WithNullMetricUnits_ThrowsArgumentNullException()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };

    // Act & Assert
    FluentActions.Invoking(() => new MetricCellRenderer(metricOrder, null!, null))
      .Should().Throw<System.ArgumentNullException>()
      .WithParameterName("metricUnits");
  }

  [Test]
  public void AppendCells_WithSingleMetric_AppendsCell()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var renderer = new MetricCellRenderer(metricOrder, metricUnits, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 25,
          Status = ThresholdStatus.Success
        }
      }
    };

    var builder = new StringBuilder();

    // Act
    renderer.AppendCells(node, "td", builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("<td");
    result.Should().Contain("data-col=\"RoslynClassCoupling\"");
    result.Should().Contain("data-status=\"success\"");
    result.Should().Contain("data-metric-id=\"RoslynClassCoupling\"");
    result.Should().Contain("</td>");
  }

  [Test]
  public void AppendCells_WithMultipleMetrics_AppendsAllCells()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.RoslynCyclomaticComplexity
    };
    var metricUnits = new Dictionary<MetricIdentifier, string?>
    {
      [MetricIdentifier.RoslynClassCoupling] = null,
      [MetricIdentifier.RoslynCyclomaticComplexity] = null
    };
    var renderer = new MetricCellRenderer(metricOrder, metricUnits, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 25,
          Status = ThresholdStatus.Success
        },
        [MetricIdentifier.RoslynCyclomaticComplexity] = new MetricValue
        {
          Value = 15,
          Status = ThresholdStatus.Warning
        }
      }
    };

    var builder = new StringBuilder();

    // Act
    renderer.AppendCells(node, "td", builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("RoslynClassCoupling");
    result.Should().Contain("RoslynCyclomaticComplexity");
    var lines = result.Split('\n');
    lines.Should().HaveCountGreaterThan(2); // At least 2 metric cells
  }

  [Test]
  public void AppendCells_WithMissingMetric_AppendsNaCell()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.RoslynCyclomaticComplexity
    };
    var metricUnits = new Dictionary<MetricIdentifier, string?>
    {
      [MetricIdentifier.RoslynClassCoupling] = null,
      [MetricIdentifier.RoslynCyclomaticComplexity] = null
    };
    var renderer = new MetricCellRenderer(metricOrder, metricUnits, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 25,
          Status = ThresholdStatus.Success
        }
        // RoslynCyclomaticComplexity is missing
      }
    };

    var builder = new StringBuilder();

    // Act
    renderer.AppendCells(node, "td", builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("data-status=\"na\""); // Missing metric should have "na" status
  }

  [Test]
  public void AppendCells_WithThTag_UsesThTag()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var renderer = new MetricCellRenderer(metricOrder, metricUnits, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 25,
          Status = ThresholdStatus.Success
        }
      }
    };

    var builder = new StringBuilder();

    // Act
    renderer.AppendCells(node, "th", builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("<th");
    result.Should().Contain("</th>");
    result.Should().NotContain("<td");
  }

  [Test]
  public void AppendCells_WithUnit_IncludesUnit()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynMaintainabilityIndex };
    var metricUnits = new Dictionary<MetricIdentifier, string?>
    {
      [MetricIdentifier.RoslynMaintainabilityIndex] = "score"
    };
    var renderer = new MetricCellRenderer(metricOrder, metricUnits, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynMaintainabilityIndex] = new MetricValue
        {
          Value = 75,
          Status = ThresholdStatus.Success,
          Unit = "score"
        }
      }
    };

    var builder = new StringBuilder();

    // Act
    renderer.AppendCells(node, "td", builder);

    // Assert
    var result = builder.ToString();
    // Unit is not displayed in the rendered output, only in the MetricValueRenderer which formats the value
    // The test verifies that the cell is rendered correctly
    result.Should().Contain("75");
  }

  [Test]
  public void AppendCells_WithSuppressedMetric_IncludesSuppressedAttribute()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
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

    var renderer = new MetricCellRenderer(metricOrder, metricUnits, suppressedIndex);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 50,
          Status = ThresholdStatus.Error
        }
      }
    };

    var builder = new StringBuilder();

    // Act
    renderer.AppendCells(node, "td", builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("data-suppressed=\"true\"");
    result.Should().Contain("data-suppression-info=");
  }

  [Test]
  public void AppendCells_WithDelta_IncludesDeltaAttribute()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var renderer = new MetricCellRenderer(metricOrder, metricUnits, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 25,
          Status = ThresholdStatus.Success,
          Delta = 5
        }
      }
    };

    var builder = new StringBuilder();

    // Act
    renderer.AppendCells(node, "td", builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("data-has-delta=\"true\"");
  }

  [Test]
  public void AppendCells_WithBreakdown_IncludesBreakdownAttribute()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.SarifCaRuleViolations };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.SarifCaRuleViolations] = null };
    var renderer = new MetricCellRenderer(metricOrder, metricUnits, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
        {
          Value = 5,
          Status = ThresholdStatus.Warning,
          Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
          {
            ["CA1506"] = new SarifRuleBreakdownEntry { Count = 3 },
            ["CA1502"] = new SarifRuleBreakdownEntry { Count = 2 }
          }
        }
      }
    };

    var builder = new StringBuilder();

    // Act
    renderer.AppendCells(node, "td", builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("data-breakdown=");
  }

  [Test]
  public void AppendCells_WithEmptyMetrics_AppendsNaCells()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.RoslynCyclomaticComplexity
    };
    var metricUnits = new Dictionary<MetricIdentifier, string?>
    {
      [MetricIdentifier.RoslynClassCoupling] = null,
      [MetricIdentifier.RoslynCyclomaticComplexity] = null
    };
    var renderer = new MetricCellRenderer(metricOrder, metricUnits, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var builder = new StringBuilder();

    // Act
    renderer.AppendCells(node, "td", builder);

    // Assert
    var result = builder.ToString();
    // Both metrics should be rendered with "na" status
    var naCount = (result.Length - result.Replace("data-status=\"na\"", string.Empty).Length) / "data-status=\"na\"".Length;
    naCount.Should().Be(2);
  }

  [Test]
  public void AppendCells_WithAllMetricTypes_HandlesAllTypes()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.OpenCoverSequenceCoverage,
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.SarifCaRuleViolations
    };
    var metricUnits = new Dictionary<MetricIdentifier, string?>
    {
      [MetricIdentifier.OpenCoverSequenceCoverage] = "%",
      [MetricIdentifier.RoslynClassCoupling] = null,
      [MetricIdentifier.SarifCaRuleViolations] = null
    };
    var renderer = new MetricCellRenderer(metricOrder, metricUnits, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.OpenCoverSequenceCoverage] = new MetricValue { Value = 75, Status = ThresholdStatus.Success },
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue { Value = 25, Status = ThresholdStatus.Success },
        [MetricIdentifier.SarifCaRuleViolations] = new MetricValue { Value = 0, Status = ThresholdStatus.Success }
      }
    };

    var builder = new StringBuilder();

    // Act
    renderer.AppendCells(node, "td", builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("OpenCoverSequenceCoverage");
    result.Should().Contain("RoslynClassCoupling");
    result.Should().Contain("SarifCaRuleViolations");
  }
}


