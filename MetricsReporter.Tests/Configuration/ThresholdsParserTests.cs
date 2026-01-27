namespace MetricsReporter.Tests.Configuration;

using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Configuration;
using MetricsReporter.Model;

[TestFixture]
[Category("Unit")]
public sealed class ThresholdsParserTests
{
  [Test]
  public void Parse_NullInput_ProducesDefaultThresholds()
  {
    // Act
    var result = ThresholdsParser.Parse(null);

    // Assert
    result.Should().HaveCount(Enum.GetValues<MetricIdentifier>().Length);
    result[MetricIdentifier.OpenCoverSequenceCoverage].Levels[MetricSymbolLevel.Type].HigherIsBetter.Should().BeTrue();
    result[MetricIdentifier.SarifCaRuleViolations].Levels[MetricSymbolLevel.Type].HigherIsBetter.Should().BeFalse();
  }

  [Test]
  public void Parse_CustomOverrides_UpdatesSpecifiedMetrics()
  {
    // Arrange
    const string customJson = """
        {
          "metrics": [
            {
              "name": "OpenCoverSequenceCoverage",
              "higherIsBetter": true,
              "symbolThresholds": {
                "Type": { "warning": 80, "error": 70 }
              }
            },
            {
              "name": "SarifCaRuleViolations",
              "higherIsBetter": false,
              "symbolThresholds": {
                "Type": { "warning": 1, "error": 2 }
              }
            }
          ]
        }
        """;

    // Act
    var result = ThresholdsParser.Parse(customJson);

    // Assert
    result[MetricIdentifier.OpenCoverSequenceCoverage].Levels[MetricSymbolLevel.Type].Warning.Should().Be(80);
    result[MetricIdentifier.OpenCoverSequenceCoverage].Levels[MetricSymbolLevel.Type].Error.Should().Be(70);
    result[MetricIdentifier.SarifCaRuleViolations].Levels[MetricSymbolLevel.Type].Warning.Should().Be(1);
    result[MetricIdentifier.SarifCaRuleViolations].Levels[MetricSymbolLevel.Type].Error.Should().Be(2);
  }

  [Test]
  public void Parse_SymbolAwareFormat_UpdatesSpecificLevels()
  {
    // Arrange
    const string json = """
        {
          "metrics": [
            {
              "name": "RoslynDepthOfInheritance",
              "description": "Avoid excessive inheritance depth.",
              "higherIsBetter": false,
              "symbolThresholds": {
                "Type": { "warning": 5, "error": 7 },
                "Member": { "warning": null, "error": null }
              }
            }
          ]
        }
        """;

    // Act
    var result = ThresholdsParser.Parse(json);

    // Assert
    var definition = result[MetricIdentifier.RoslynDepthOfInheritance];
    definition.Description.Should().Be("Avoid excessive inheritance depth.");
    definition.Levels[MetricSymbolLevel.Type].Warning.Should().Be(5);
    definition.Levels[MetricSymbolLevel.Type].Error.Should().Be(7);
    definition.Levels[MetricSymbolLevel.Type].HigherIsBetter.Should().BeFalse();

    definition.Levels[MetricSymbolLevel.Member].Warning.Should().BeNull();
    definition.Levels[MetricSymbolLevel.Member].Error.Should().BeNull();
    definition.Levels[MetricSymbolLevel.Member].HigherIsBetter.Should().BeFalse();
  }

  [Test]
  public void Parse_PositiveDeltaNeutralFlag_IsAppliedAcrossLevels()
  {
    // Arrange
    const string json = """
        {
          "metrics": [
            {
              "name": "RoslynSourceLines",
              "higherIsBetter": false,
              "positiveDeltaNeutral": true
            }
          ]
        }
        """;

    // Act
    var result = ThresholdsParser.Parse(json);

    // Assert
    result[MetricIdentifier.RoslynSourceLines].Levels[MetricSymbolLevel.Type].PositiveDeltaNeutral.Should().BeTrue();
  }

  [Test]
  public void Parse_InvalidMetricEntries_AreIgnored()
  {
    // Arrange
    const string json = """
        {
          "metrics": [
            { "description": "missing name entry" },
            { "name": "   " },
            { "name": "UnknownMetric", "symbolThresholds": { "Type": { "warning": 10, "error": 1 } } }
          ]
        }
        """;

    // Act
    var result = ThresholdsParser.Parse(json);

    // Assert
    result.Should().HaveCount(Enum.GetValues<MetricIdentifier>().Length);
    result[MetricIdentifier.OpenCoverBranchCoverage].Levels[MetricSymbolLevel.Type].Warning.Should().Be(70);
    result[MetricIdentifier.OpenCoverBranchCoverage].Levels[MetricSymbolLevel.Type].Error.Should().Be(55);
  }

  [Test]
  public void Parse_NumericMetricIdentifier_AddsNewDefinitionUsingFallbackMetadata()
  {
    // Arrange
    const string json = """
        {
          "metrics": [
            {
              "name": "999",
              "higherIsBetter": false,
              "positiveDeltaNeutral": true,
              "symbolThresholds": {
                "Member": { "warning": 2, "error": 3 }
              }
            }
          ]
        }
        """;

    // Act
    var result = ThresholdsParser.Parse(json);

    // Assert
    var customIdentifier = (MetricIdentifier)999;
    result.Should().ContainKey(customIdentifier);

    var definition = result[customIdentifier];
    definition.Levels.Should().HaveCount(Enum.GetValues<MetricSymbolLevel>().Length);
    definition.Levels[MetricSymbolLevel.Member].Warning.Should().Be(2);
    definition.Levels[MetricSymbolLevel.Member].Error.Should().Be(3);
    definition.Levels[MetricSymbolLevel.Member].HigherIsBetter.Should().BeFalse();
    definition.Levels[MetricSymbolLevel.Member].PositiveDeltaNeutral.Should().BeTrue();
    definition.Levels[MetricSymbolLevel.Solution].HigherIsBetter.Should().BeFalse();
    definition.Levels[MetricSymbolLevel.Solution].PositiveDeltaNeutral.Should().BeTrue();
  }

  [Test]
  public void Parse_InvalidJson_ThrowsInvalidOperationException()
  {
    // Arrange
    const string invalidJson = "{'OpenCoverSequenceCoverage':{'warning':}";

    // Act
    var act = () => ThresholdsParser.Parse(invalidJson);

    // Assert
    act.Should().Throw<InvalidOperationException>();
  }
}


