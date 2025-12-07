using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MetricsReporter.Cli.Commands;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Configuration;
using NUnit.Framework;

namespace MetricsReporter.Tests.Cli.Commands;

[TestFixture]
[Category("Unit")]
internal sealed class ScriptSelectionTests
{
  [Test]
  public void SelectReadScripts_WhenMetricMatches_ReturnsGenericAndMetricScripts()
  {
    // Arrange
    var scripts = new ResolvedScripts(
      Generate: [],
      ReadAny: new[] { "common.ps1" },
      ReadByMetric:
      [
        new MetricScript { Metrics = new[] { "Complexity", "Coupling" }, Path = "metric.ps1" },
        new MetricScript { Metrics = new[] { "Other" }, Path = "other.ps1" }
      ],
      TestAny: [],
      TestByMetric: []);

    var metrics = new[] { "coupling" };

    // Act
    var result = ScriptSelection.SelectReadScripts(scripts, metrics);

    // Assert
    result.Should().BeEquivalentTo(new[] { "common.ps1", "metric.ps1" });
  }

  [Test]
  public void SelectTestScripts_WhenMetricsDoNotMatch_ReturnsGenericScriptsOnly()
  {
    // Arrange
    var scripts = new ResolvedScripts(
      Generate: [],
      ReadAny: [],
      ReadByMetric: [],
      TestAny: new[] { "shared.ps1" },
      TestByMetric:
      [
        new MetricScript { Metrics = new[] { "Depth" }, Path = "depth.ps1" }
      ]);

    var metrics = new[] { "Coupling" };

    // Act
    var result = ScriptSelection.SelectTestScripts(scripts, metrics);

    // Assert
    result.Should().Equal("shared.ps1");
  }
}

[TestFixture]
[Category("Unit")]
internal sealed class MetricScriptParserTests
{
  [Test]
  public void Parse_WithValidEntries_TrimsPartsAndIgnoresInvalidOnes()
  {
    // Arrange
    var inputs = new List<string>
    {
      "MetricA= scripts/run-a.ps1 ",
      "  MetricB :run-b.ps1",
      "invalid-entry",
      "MetricC=",
      ""
    };
    var separators = new[] { '=', ':' };

    // Act
    var result = MetricScriptParser.Parse(inputs, separators);

    // Assert
    result.Should().HaveCount(2);
    result[0].Metric.Should().Be("MetricA");
    result[0].Path.Should().Be("scripts/run-a.ps1");
    result[1].Metric.Should().Be("MetricB");
    result[1].Path.Should().Be("run-b.ps1");
  }
}

