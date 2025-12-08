using System;
using FluentAssertions;
using MetricsReporter.Cli.Commands;
using NUnit.Framework;

namespace MetricsReporter.Tests.Cli.Commands;

[TestFixture]
[Category("Unit")]
internal sealed class MetricAliasParserTests
{
  [Test]
  public void Parse_WithNull_ReturnsNull()
  {
    MetricAliasParser.Parse(null).Should().BeNull();
  }

  [Test]
  public void Parse_WithValidObject_ReturnsDictionary()
  {
    var result = MetricAliasParser.Parse(@"{""RoslynCyclomaticComplexity"": [""cc"",""cyclomatic""]}");

    result.Should().NotBeNull();
    result!["RoslynCyclomaticComplexity"].Should().BeEquivalentTo("cc", "cyclomatic");
  }

  [TestCase("[]", "Metric aliases must be a JSON object.")]
  [TestCase(@"{""Metric"":""not-array""}", "must be an array")]
  [TestCase(@"{""Metric"":[]}", "must be a non-empty array")]
  [TestCase(@"{""Metric"":["" ""]}", "must not contain empty")]
  [TestCase(@"{""Metric"":[1]}", "must contain only strings")]
  public void Parse_WithInvalidShape_Throws(string json, string expectedMessagePart)
  {
    Action act = () => MetricAliasParser.Parse(json);

    act.Should().Throw<ArgumentException>()
      .Which.Message.Should().Contain(expectedMessagePart);
  }
}


