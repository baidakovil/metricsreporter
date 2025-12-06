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
    result[MetricIdentifier.AltCoverSequenceCoverage].Levels[MetricSymbolLevel.Type].HigherIsBetter.Should().BeTrue();
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
              "name": "AltCoverSequenceCoverage",
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
    result[MetricIdentifier.AltCoverSequenceCoverage].Levels[MetricSymbolLevel.Type].Warning.Should().Be(80);
    result[MetricIdentifier.AltCoverSequenceCoverage].Levels[MetricSymbolLevel.Type].Error.Should().Be(70);
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
  public void Parse_InvalidJson_ThrowsInvalidOperationException()
  {
    // Arrange
    const string invalidJson = "{'AltCoverSequenceCoverage':{'warning':}";

    // Act
    var act = () => ThresholdsParser.Parse(invalidJson);

    // Assert
    act.Should().Throw<InvalidOperationException>();
  }
}


