namespace MetricsReporter.Tests.Processing.Parsers;

using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Processing.Parsers;

[TestFixture]
[Category("Unit")]
public sealed class SarifRuleViolationFactoryTests
{
  [Test]
  public void CreateCodeElement_ValidCaRule_BuildsBreakdownEntry()
  {
    // Arrange
    var sourceLocation = new SourceLocation
    {
      Path = "Sample.cs",
      StartLine = 10,
      EndLine = 12
    };
    var location = new SarifLocation(sourceLocation, "file:///C:/Repo/Sample.cs");

    // Act
    var element = SarifRuleViolationFactory.CreateCodeElement(
        "CA1502",
        MetricIdentifier.SarifCaRuleViolations,
        location,
        "Cyclomatic complexity exceeded.");

    // Assert
    element.Kind.Should().Be(CodeElementKind.Member);
    element.Name.Should().Be("CA1502");
    element.Source.Should().Be(sourceLocation);

    element.Metrics.Should().ContainKey(MetricIdentifier.SarifCaRuleViolations);
    var metric = element.Metrics[MetricIdentifier.SarifCaRuleViolations];
    metric.Value.Should().Be(1);
    metric.Breakdown.Should().NotBeNull().And.ContainKey("CA1502");

    var entry = metric.Breakdown!["CA1502"];
    entry.Count.Should().Be(1);
    entry.Violations.Should().ContainSingle();
    var violation = entry.Violations[0];
    violation.Message.Should().Be("Cyclomatic complexity exceeded.");
    violation.Uri.Should().Be("file:///C:/Repo/Sample.cs");
    violation.StartLine.Should().Be(10);
    violation.EndLine.Should().Be(12);
  }

  [Test]
  public void CreateCodeElement_InvalidRule_DoesNotCreateBreakdown()
  {
    // Arrange
    var sourceLocation = new SourceLocation
    {
      Path = "Sample.cs",
      StartLine = 5,
      EndLine = 5
    };
    var location = new SarifLocation(sourceLocation, null);

    // Act
    var element = SarifRuleViolationFactory.CreateCodeElement(
        "SYSLIB1045",
        MetricIdentifier.SarifCaRuleViolations,
        location,
        null);

    // Assert
    element.Metrics.Should().ContainKey(MetricIdentifier.SarifCaRuleViolations);
    var metric = element.Metrics[MetricIdentifier.SarifCaRuleViolations];
    metric.Value.Should().Be(1);
    metric.Breakdown.Should().BeNull("invalid rule identifiers are filtered by RuleIdValidator");
  }

  [Test]
  public void CreateCodeElement_ValidIdeRule_BuildsBreakdownEntry()
  {
    // Arrange
    var sourceLocation = new SourceLocation
    {
      Path = "Sample.cs",
      StartLine = 15,
      EndLine = 15
    };
    var location = new SarifLocation(sourceLocation, "file:///C:/Repo/Sample.cs");

    // Act
    var element = SarifRuleViolationFactory.CreateCodeElement(
        "IDE0051",
        MetricIdentifier.SarifIdeRuleViolations,
        location,
        "Private member is unused.");

    // Assert
    element.Metrics.Should().ContainKey(MetricIdentifier.SarifIdeRuleViolations);
    var metric = element.Metrics[MetricIdentifier.SarifIdeRuleViolations];
    metric.Value.Should().Be(1);
    metric.Breakdown.Should().NotBeNull().And.ContainKey("IDE0051");

    var entry = metric.Breakdown!["IDE0051"];
    entry.Count.Should().Be(1);
    entry.Violations.Should().ContainSingle();
    var violation = entry.Violations[0];
    violation.Message.Should().Be("Private member is unused.");
    violation.Uri.Should().Be("file:///C:/Repo/Sample.cs");
    violation.StartLine.Should().Be(15);
    violation.EndLine.Should().Be(15);
  }

  [Test]
  public void CreateCodeElement_WithNullMessageText_StillCreatesBreakdown()
  {
    // Arrange
    var sourceLocation = new SourceLocation
    {
      Path = "Sample.cs",
      StartLine = 10,
      EndLine = 12
    };
    var location = new SarifLocation(sourceLocation, "file:///C:/Repo/Sample.cs");

    // Act
    var element = SarifRuleViolationFactory.CreateCodeElement(
        "CA1502",
        MetricIdentifier.SarifCaRuleViolations,
        location,
        null);

    // Assert
    var metric = element.Metrics[MetricIdentifier.SarifCaRuleViolations];
    metric.Breakdown.Should().NotBeNull().And.ContainKey("CA1502");
    var violation = metric.Breakdown!["CA1502"].Violations[0];
    violation.Message.Should().BeNull();
  }

  [Test]
  public void CreateCodeElement_WithNullOriginalUri_StillCreatesBreakdown()
  {
    // Arrange
    var sourceLocation = new SourceLocation
    {
      Path = "Sample.cs",
      StartLine = 10,
      EndLine = 12
    };
    var location = new SarifLocation(sourceLocation, null);

    // Act
    var element = SarifRuleViolationFactory.CreateCodeElement(
        "CA1502",
        MetricIdentifier.SarifCaRuleViolations,
        location,
        "Test message");

    // Assert
    var metric = element.Metrics[MetricIdentifier.SarifCaRuleViolations];
    metric.Breakdown.Should().NotBeNull().And.ContainKey("CA1502");
    var violation = metric.Breakdown!["CA1502"].Violations[0];
    violation.Uri.Should().BeNull();
  }

  [Test]
  public void CreateCodeElement_WithNullSourceLocationLines_StillCreatesBreakdown()
  {
    // Arrange
    var sourceLocation = new SourceLocation
    {
      Path = "Sample.cs",
      StartLine = null,
      EndLine = null
    };
    var location = new SarifLocation(sourceLocation, "file:///C:/Repo/Sample.cs");

    // Act
    var element = SarifRuleViolationFactory.CreateCodeElement(
        "CA1502",
        MetricIdentifier.SarifCaRuleViolations,
        location,
        "Test message");

    // Assert
    var metric = element.Metrics[MetricIdentifier.SarifCaRuleViolations];
    metric.Breakdown.Should().NotBeNull().And.ContainKey("CA1502");
    var violation = metric.Breakdown!["CA1502"].Violations[0];
    violation.StartLine.Should().BeNull();
    violation.EndLine.Should().BeNull();
  }

  [Test]
  public void CreateCodeElement_WithEmptyRuleId_DoesNotCreateBreakdown()
  {
    // Arrange
    var sourceLocation = new SourceLocation
    {
      Path = "Sample.cs",
      StartLine = 10,
      EndLine = 10
    };
    var location = new SarifLocation(sourceLocation, "file:///C:/Repo/Sample.cs");

    // Act
    var element = SarifRuleViolationFactory.CreateCodeElement(
        string.Empty,
        MetricIdentifier.SarifCaRuleViolations,
        location,
        "Test message");

    // Assert
    var metric = element.Metrics[MetricIdentifier.SarifCaRuleViolations];
    metric.Value.Should().Be(1);
    metric.Breakdown.Should().BeNull("empty rule ID is invalid");
  }

  [Test]
  public void CreateCodeElement_WithMalformedCaRule_DoesNotCreateBreakdown()
  {
    // Arrange - CA rule but too short (CA123 instead of CA1234)
    var sourceLocation = new SourceLocation
    {
      Path = "Sample.cs",
      StartLine = 10,
      EndLine = 10
    };
    var location = new SarifLocation(sourceLocation, "file:///C:/Repo/Sample.cs");

    // Act
    var element = SarifRuleViolationFactory.CreateCodeElement(
        "CA123",
        MetricIdentifier.SarifCaRuleViolations,
        location,
        "Test message");

    // Assert
    var metric = element.Metrics[MetricIdentifier.SarifCaRuleViolations];
    metric.Value.Should().Be(1);
    metric.Breakdown.Should().BeNull("malformed CA rule ID (too short) is rejected by RuleIdValidator");
  }

  [Test]
  public void CreateCodeElement_WithMalformedIdeRule_DoesNotCreateBreakdown()
  {
    // Arrange - IDE rule but too short (IDE12 instead of IDE0012)
    var sourceLocation = new SourceLocation
    {
      Path = "Sample.cs",
      StartLine = 10,
      EndLine = 10
    };
    var location = new SarifLocation(sourceLocation, "file:///C:/Repo/Sample.cs");

    // Act
    var element = SarifRuleViolationFactory.CreateCodeElement(
        "IDE12",
        MetricIdentifier.SarifIdeRuleViolations,
        location,
        "Test message");

    // Assert
    var metric = element.Metrics[MetricIdentifier.SarifIdeRuleViolations];
    metric.Value.Should().Be(1);
    metric.Breakdown.Should().BeNull("malformed IDE rule ID (too short) is rejected by RuleIdValidator");
  }
}


