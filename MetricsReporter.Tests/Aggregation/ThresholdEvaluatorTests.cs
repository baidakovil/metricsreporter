namespace MetricsReporter.Tests.Aggregation;

using System.Collections.Generic;
using FluentAssertions;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using NUnit.Framework;

[TestFixture]
[Category("Unit")]
public sealed class ThresholdEvaluatorTests
{
  // Verifies null metric values are treated as not applicable to avoid misreporting missing data.
  [Test]
  public void Evaluate_NullValue_ReturnsNotApplicable()
  {
    // Arrange
    var thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>();

    // Act
    var result = ThresholdEvaluator.Evaluate(
        MetricIdentifier.OpenCoverBranchCoverage,
        null,
        thresholds,
        MetricSymbolLevel.Member);

    // Assert
    result.Should().Be(ThresholdStatus.NotApplicable);
  }

  // Ensures metrics without configured thresholds default to success so absence of policy does not block reporting.
  [Test]
  public void Evaluate_ThresholdDefinitionMissing_ReturnsSuccess()
  {
    // Arrange
    var thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>();

    // Act
    var result = ThresholdEvaluator.Evaluate(
        MetricIdentifier.OpenCoverBranchCoverage,
        85,
        thresholds,
        MetricSymbolLevel.Member);

    // Assert
    result.Should().Be(ThresholdStatus.Success);
  }

  // Confirms the evaluator uses an exact symbol-level threshold when available instead of falling back to broader defaults.
  [Test]
  public void Evaluate_LevelThresholdExists_UsesRequestedLevel()
  {
    // Arrange
    var thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>
    {
      [MetricIdentifier.OpenCoverBranchCoverage] = new MetricThresholdDefinition
      {
        Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>
        {
          [MetricSymbolLevel.Member] = new() { Warning = 90, Error = 80, HigherIsBetter = true }
        }
      }
    };

    // Act
    var result = ThresholdEvaluator.Evaluate(
        MetricIdentifier.OpenCoverBranchCoverage,
        85,
        thresholds,
        MetricSymbolLevel.Member);

    // Assert
    result.Should().Be(ThresholdStatus.Warning);
  }

  // Validates the evaluator falls back to the type-level threshold when the requested level is missing, exercising the secondary lookup branch.
  [Test]
  public void Evaluate_LevelMissing_UsesTypeFallback()
  {
    // Arrange
    var thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>
    {
      [MetricIdentifier.OpenCoverBranchCoverage] = new MetricThresholdDefinition
      {
        Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>
        {
          [MetricSymbolLevel.Type] = new() { Warning = 10, Error = 20, HigherIsBetter = false }
        }
      }
    };

    // Act
    var result = ThresholdEvaluator.Evaluate(
        MetricIdentifier.OpenCoverBranchCoverage,
        25,
        thresholds,
        MetricSymbolLevel.Member);

    // Assert
    result.Should().Be(ThresholdStatus.Error);
  }

  // Verifies that when neither the requested level nor type fallback is defined, the evaluator treats the metric as successful instead of failing.
  [Test]
  public void Evaluate_NoApplicableLevels_ReturnsSuccess()
  {
    // Arrange
    var thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>
    {
      [MetricIdentifier.OpenCoverBranchCoverage] = new MetricThresholdDefinition
      {
        Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>
        {
          [MetricSymbolLevel.Namespace] = new() { Warning = 95, Error = 90, HigherIsBetter = true }
        }
      }
    };

    // Act
    var result = ThresholdEvaluator.Evaluate(
        MetricIdentifier.OpenCoverBranchCoverage,
        50,
        thresholds,
        MetricSymbolLevel.Member);

    // Assert
    result.Should().Be(ThresholdStatus.Success);
  }
}

