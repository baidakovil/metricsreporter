namespace MetricsReporter.Tests.Services;

using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using MetricsReporter.Services;

/// <summary>
/// Unit tests for <see cref="ThresholdConfiguration"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class ThresholdConfigurationTests
{
  [Test]
  public void Empty_ReturnsConfigurationWithEmptyDictionary()
  {
    // Act
    var configuration = ThresholdConfiguration.Empty;

    // Assert
    configuration.Should().NotBeNull();
    var dictionary = configuration.AsDictionary();
    dictionary.Should().NotBeNull();
    dictionary.Should().BeEmpty();
  }


  [Test]
  public void From_WithEmptyDictionary_ReturnsConfigurationWithEmptyDictionary()
  {
    // Arrange
    var thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>();

    // Act
    var configuration = ThresholdConfiguration.From(thresholds);

    // Assert
    configuration.Should().NotBeNull();
    var dictionary = configuration.AsDictionary();
    dictionary.Should().NotBeNull();
    dictionary.Should().BeEmpty();
  }

  [Test]
  public void From_WithValidDictionary_ReturnsConfigurationWithSameDictionary()
  {
    // Arrange
    var thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>
    {
      [MetricIdentifier.RoslynClassCoupling] = new MetricThresholdDefinition
      {
        Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>
        {
          [MetricSymbolLevel.Type] = new MetricThreshold { Warning = 40, Error = 50, HigherIsBetter = false }
        }
      },
      [MetricIdentifier.AltCoverSequenceCoverage] = new MetricThresholdDefinition
      {
        Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>
        {
          [MetricSymbolLevel.Type] = new MetricThreshold { Warning = 70, Error = 50, HigherIsBetter = true }
        }
      }
    };

    // Act
    var configuration = ThresholdConfiguration.From(thresholds);

    // Assert
    configuration.Should().NotBeNull();
    var dictionary = configuration.AsDictionary();
    dictionary.Should().NotBeNull();
    dictionary.Should().HaveCount(2);
    dictionary.Should().ContainKey(MetricIdentifier.RoslynClassCoupling);
    dictionary.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
  }

  [Test]
  public void AsDictionary_ReturnsSameDictionaryInstance_NotACopy()
  {
    // Arrange
    var thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>
    {
      [MetricIdentifier.RoslynClassCoupling] = new MetricThresholdDefinition
      {
        Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>
        {
          [MetricSymbolLevel.Type] = new MetricThreshold { Warning = 40, Error = 50, HigherIsBetter = false }
        }
      }
    };

    var configuration = ThresholdConfiguration.From(thresholds);
    var dictionary1 = configuration.AsDictionary();
    var dictionary2 = configuration.AsDictionary();

    // Assert - AsDictionary returns the same dictionary instance (not a copy)
    dictionary1.Should().BeSameAs(dictionary2);
    dictionary1.Should().HaveCount(1);
    dictionary1.Should().ContainKey(MetricIdentifier.RoslynClassCoupling);
  }

  [Test]
  public void From_WithDictionaryContainingAllMetricIdentifiers_ReturnsCompleteConfiguration()
  {
    // Arrange
    var thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>();
    foreach (var metricId in Enum.GetValues<MetricIdentifier>())
    {
      thresholds[metricId] = new MetricThresholdDefinition
      {
        Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>()
      };
    }

    // Act
    var configuration = ThresholdConfiguration.From(thresholds);

    // Assert
    configuration.Should().NotBeNull();
    var dictionary = configuration.AsDictionary();
    dictionary.Should().HaveCount(Enum.GetValues<MetricIdentifier>().Length);
  }

  [Test]
  public void Empty_ReturnsSameInstance_OnMultipleCalls()
  {
    // Act
    var instance1 = ThresholdConfiguration.Empty;
    var instance2 = ThresholdConfiguration.Empty;

    // Assert
    instance1.Should().BeSameAs(instance2);
  }
}


