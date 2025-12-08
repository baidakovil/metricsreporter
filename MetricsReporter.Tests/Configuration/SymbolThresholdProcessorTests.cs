namespace MetricsReporter.Tests.Configuration;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Configuration;
using MetricsReporter.Model;
using System.Text.Json;
/// <summary>
/// Unit tests for <see cref="SymbolThresholdProcessor"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class SymbolThresholdProcessorTests
{
  [Test]
  public void ApplySymbolThresholds_WithSymbolThresholds_AppliesThresholds()
  {
    // Arrange
    var json = """
      {
        "symbolThresholds": {
          "Type": { "warning": 80, "error": 70 },
          "Member": { "warning": 50, "error": 40 }
        }
      }
      """;
    var element = JsonDocument.Parse(json).RootElement;
    var definition = new MetricThresholdDefinition
    {
      Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>()
    };
    MetricThreshold CreateThreshold(decimal? warning, decimal? error)
    {
      return new MetricThreshold
      {
        Warning = warning,
        Error = error,
        HigherIsBetter = true,
        PositiveDeltaNeutral = false
      };
    }
    // Act
    SymbolThresholdProcessor.ApplySymbolThresholds(element, definition, CreateThreshold);
    // Assert
    definition.Levels.Should().ContainKey(MetricSymbolLevel.Type);
    definition.Levels[MetricSymbolLevel.Type].Warning.Should().Be(80);
    definition.Levels[MetricSymbolLevel.Type].Error.Should().Be(70);
    definition.Levels[MetricSymbolLevel.Member].Warning.Should().Be(50);
    definition.Levels[MetricSymbolLevel.Member].Error.Should().Be(40);
  }
  [Test]
  public void ApplySymbolThresholds_WithoutSymbolThresholds_EnsuresAllLevelsPresent()
  {
    // Arrange
    var json = "{}";
    var element = JsonDocument.Parse(json).RootElement;
    var definition = new MetricThresholdDefinition
    {
      Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>()
    };
    MetricThreshold CreateThreshold(decimal? warning, decimal? error)
    {
      return new MetricThreshold
      {
        Warning = warning,
        Error = error,
        HigherIsBetter = true,
        PositiveDeltaNeutral = false
      };
    }
    // Act
    SymbolThresholdProcessor.ApplySymbolThresholds(element, definition, CreateThreshold);
    // Assert
    definition.Levels.Should().ContainKey(MetricSymbolLevel.Solution);
    definition.Levels.Should().ContainKey(MetricSymbolLevel.Assembly);
    definition.Levels.Should().ContainKey(MetricSymbolLevel.Namespace);
    definition.Levels.Should().ContainKey(MetricSymbolLevel.Type);
    definition.Levels.Should().ContainKey(MetricSymbolLevel.Member);
  }
  [Test]
  public void ApplySymbolThresholds_WithExistingLevels_UpdatesWithNewThresholds()
  {
    // Arrange
    var json = """
      {
        "symbolThresholds": {
          "Type": { "warning": 80, "error": 70 }
        }
      }
      """;
    var element = JsonDocument.Parse(json).RootElement;
    var definition = new MetricThresholdDefinition
    {
      Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>
      {
        [MetricSymbolLevel.Type] = new MetricThreshold
        {
          Warning = 100,
          Error = 90,
          HigherIsBetter = false,
          PositiveDeltaNeutral = true
        }
      }
    };
    MetricThreshold CreateThreshold(decimal? warning, decimal? error)
    {
      return new MetricThreshold
      {
        Warning = warning,
        Error = error,
        HigherIsBetter = true,
        PositiveDeltaNeutral = false
      };
    }
    // Act
    SymbolThresholdProcessor.ApplySymbolThresholds(element, definition, CreateThreshold);
    // Assert
    definition.Levels[MetricSymbolLevel.Type].Warning.Should().Be(80);
    definition.Levels[MetricSymbolLevel.Type].Error.Should().Be(70);
    definition.Levels[MetricSymbolLevel.Type].HigherIsBetter.Should().BeTrue();
  }
  [Test]
  public void ApplySymbolThresholds_WithNullValues_HandlesNulls()
  {
    // Arrange
    var json = """
      {
        "symbolThresholds": {
          "Type": { "warning": null, "error": null }
        }
      }
      """;
    var element = JsonDocument.Parse(json).RootElement;
    var definition = new MetricThresholdDefinition
    {
      Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>()
    };
    MetricThreshold CreateThreshold(decimal? warning, decimal? error)
    {
      return new MetricThreshold
      {
        Warning = warning,
        Error = error,
        HigherIsBetter = true,
        PositiveDeltaNeutral = false
      };
    }
    // Act
    SymbolThresholdProcessor.ApplySymbolThresholds(element, definition, CreateThreshold);
    // Assert
    definition.Levels[MetricSymbolLevel.Type].Warning.Should().BeNull();
    definition.Levels[MetricSymbolLevel.Type].Error.Should().BeNull();
  }

  [Test]
  public void ApplySymbolThresholds_WithMissingErrorValue_UsesNull()
  {
    // Arrange
    var json = """
      {
        "symbolThresholds": {
          "Type": { "warning": 25 }
        }
      }
      """;
    var element = JsonDocument.Parse(json).RootElement;
    var definition = new MetricThresholdDefinition
    {
      Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>()
    };
    MetricThreshold CreateThreshold(decimal? warning, decimal? error)
    {
      return new MetricThreshold
      {
        Warning = warning,
        Error = error,
        HigherIsBetter = true,
        PositiveDeltaNeutral = false
      };
    }

    // Act
    SymbolThresholdProcessor.ApplySymbolThresholds(element, definition, CreateThreshold);

    // Assert
    definition.Levels[MetricSymbolLevel.Type].Warning.Should().Be(25);
    definition.Levels[MetricSymbolLevel.Type].Error.Should().BeNull();
  }
  [Test]
  public void ApplySymbolThresholds_WithInvalidLevel_IgnoresInvalidLevel()
  {
    // Arrange
    var json = """
      {
        "symbolThresholds": {
          "InvalidLevel": { "warning": 80, "error": 70 },
          "Type": { "warning": 80, "error": 70 }
        }
      }
      """;
    var element = JsonDocument.Parse(json).RootElement;
    var definition = new MetricThresholdDefinition
    {
      Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>()
    };
    MetricThreshold CreateThreshold(decimal? warning, decimal? error)
    {
      return new MetricThreshold
      {
        Warning = warning,
        Error = error,
        HigherIsBetter = true,
        PositiveDeltaNeutral = false
      };
    }
    // Act
    SymbolThresholdProcessor.ApplySymbolThresholds(element, definition, CreateThreshold);
    // Assert
    definition.Levels.Should().ContainKey(MetricSymbolLevel.Type);
    definition.Levels.Should().NotContainKey((MetricSymbolLevel)999); // Invalid level should not be added
  }
  [Test]
  public void ApplySymbolThresholds_WithNonObjectValue_IgnoresInvalidValue()
  {
    // Arrange
    var json = """
      {
        "symbolThresholds": {
          "Type": "invalid"
        }
      }
      """;
    var element = JsonDocument.Parse(json).RootElement;
    var definition = new MetricThresholdDefinition
    {
      Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>()
    };
    MetricThreshold CreateThreshold(decimal? warning, decimal? error)
    {
      return new MetricThreshold
      {
        Warning = warning,
        Error = error,
        HigherIsBetter = true,
        PositiveDeltaNeutral = false
      };
    }
    // Act
    SymbolThresholdProcessor.ApplySymbolThresholds(element, definition, CreateThreshold);
    // Assert
    // Type level should still be created with defaults
    definition.Levels.Should().ContainKey(MetricSymbolLevel.Type);
  }
}






