namespace MetricsReporter.Tests.Rendering;

using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="RowStateCalculator"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class RowStateCalculatorTests
{
  [Test]
  public void Constructor_WithNullMetricOrder_ThrowsArgumentNullException()
  {
    // Act & Assert
    FluentActions.Invoking(() => new RowStateCalculator(null!, null))
      .Should().Throw<ArgumentNullException>()
      .WithParameterName("metricOrder");
  }

  [Test]
  public void Calculate_WithErrorStatus_SetsHasError()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var calculator = new RowStateCalculator(metricOrder, null);

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

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasError.Should().BeTrue();
    result.HasWarning.Should().BeFalse();
    result.HasSuppressed.Should().BeFalse();
    result.HasDelta.Should().BeFalse();
  }

  [Test]
  public void Calculate_WithWarningStatus_SetsHasWarning()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var calculator = new RowStateCalculator(metricOrder, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 30,
          Status = ThresholdStatus.Warning
        }
      }
    };

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasError.Should().BeFalse();
    result.HasWarning.Should().BeTrue();
    result.HasSuppressed.Should().BeFalse();
    result.HasDelta.Should().BeFalse();
  }

  [Test]
  public void Calculate_WithSuppressedMetric_SetsHasSuppressed()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
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

    var calculator = new RowStateCalculator(metricOrder, suppressedIndex);

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

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasError.Should().BeFalse(); // Suppressed metrics don't contribute to error state
    result.HasWarning.Should().BeFalse();
    result.HasSuppressed.Should().BeTrue();
    result.HasDelta.Should().BeFalse();
  }

  [Test]
  public void Calculate_WithDelta_SetsHasDelta()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var calculator = new RowStateCalculator(metricOrder, null);

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

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasError.Should().BeFalse();
    result.HasWarning.Should().BeFalse();
    result.HasSuppressed.Should().BeFalse();
    result.HasDelta.Should().BeTrue();
  }

  [Test]
  public void Calculate_WithZeroDelta_DoesNotSetHasDelta()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var calculator = new RowStateCalculator(metricOrder, null);

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
          Delta = 0
        }
      }
    };

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasDelta.Should().BeFalse();
  }

  [Test]
  public void Calculate_WithNegativeDelta_SetsHasDelta()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var calculator = new RowStateCalculator(metricOrder, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 20,
          Status = ThresholdStatus.Success,
          Delta = -5
        }
      }
    };

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasDelta.Should().BeTrue();
  }

  [Test]
  public void Calculate_WithMultipleMetrics_CombinesStates()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.RoslynCyclomaticComplexity,
      MetricIdentifier.RoslynMaintainabilityIndex
    };
    var calculator = new RowStateCalculator(metricOrder, null);

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
        },
        [MetricIdentifier.RoslynCyclomaticComplexity] = new MetricValue
        {
          Value = 30,
          Status = ThresholdStatus.Warning
        },
        [MetricIdentifier.RoslynMaintainabilityIndex] = new MetricValue
        {
          Value = 60,
          Status = ThresholdStatus.Success,
          Delta = 10
        }
      }
    };

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasError.Should().BeTrue();
    result.HasWarning.Should().BeTrue();
    result.HasDelta.Should().BeTrue();
    result.HasSuppressed.Should().BeFalse();
  }

  [Test]
  public void Calculate_WithMissingMetrics_IgnoresMissing()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.RoslynCyclomaticComplexity
    };
    var calculator = new RowStateCalculator(metricOrder, null);

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
        // RoslynCyclomaticComplexity is missing
      }
    };

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasError.Should().BeTrue();
    result.HasWarning.Should().BeFalse();
  }

  [Test]
  public void Calculate_WithNullFqn_DoesNotFindSuppression()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
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

    var calculator = new RowStateCalculator(metricOrder, suppressedIndex);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = null, // Null FQN
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 50,
          Status = ThresholdStatus.Error
        }
      }
    };

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasSuppressed.Should().BeFalse();
    result.HasError.Should().BeTrue(); // Not suppressed, so error is present
  }

  [Test]
  public void Calculate_WithEmptyFqn_DoesNotFindSuppression()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
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

    var calculator = new RowStateCalculator(metricOrder, suppressedIndex);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = string.Empty, // Empty FQN
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 50,
          Status = ThresholdStatus.Error
        }
      }
    };

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasSuppressed.Should().BeFalse();
    result.HasError.Should().BeTrue();
  }

  [Test]
  public void Calculate_WithNullSuppressedIndex_DoesNotSetHasSuppressed()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var calculator = new RowStateCalculator(metricOrder, null);

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

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasSuppressed.Should().BeFalse();
  }

  [Test]
  public void Calculate_WithAllFlagsSet_ReturnsAllTrue()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.RoslynCyclomaticComplexity,
      MetricIdentifier.RoslynMaintainabilityIndex
    };
    var suppressedIndex = new Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>
    {
      [("Sample.Namespace.SampleType", MetricIdentifier.RoslynMaintainabilityIndex)] = new SuppressedSymbolInfo
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "RoslynMaintainabilityIndex",
        RuleId = "CA1501",
        Justification = "Justified"
      }
    };

    var calculator = new RowStateCalculator(metricOrder, suppressedIndex);

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
        },
        [MetricIdentifier.RoslynCyclomaticComplexity] = new MetricValue
        {
          Value = 30,
          Status = ThresholdStatus.Warning,
          Delta = 5
        }
      }
    };

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasError.Should().BeTrue();
    result.HasWarning.Should().BeTrue();
    result.HasSuppressed.Should().BeTrue();
    result.HasDelta.Should().BeTrue();
  }

  [Test]
  public void Calculate_WithNoMetrics_ReturnsAllFalse()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var calculator = new RowStateCalculator(metricOrder, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasError.Should().BeFalse();
    result.HasWarning.Should().BeFalse();
    result.HasSuppressed.Should().BeFalse();
    result.HasDelta.Should().BeFalse();
  }

  [Test]
  public void Calculate_WithSuppressionForDifferentMetric_DoesNotSuppress()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
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

    var calculator = new RowStateCalculator(metricOrder, suppressedIndex);

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

    // Act
    var result = calculator.Calculate(node);

    // Assert
    result.HasSuppressed.Should().BeFalse();
    result.HasError.Should().BeTrue();
  }
}


