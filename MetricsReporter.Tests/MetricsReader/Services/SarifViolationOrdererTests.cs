namespace MetricsReporter.Tests.MetricsReader.Services;

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.Model;

/// <summary>
/// Unit tests for <see cref="SarifViolationOrderer"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class SarifViolationOrdererTests
{
  [Test]
  public void OrderGroups_EmptyGroups_ReturnsEmptyList()
  {
    // Arrange
    var orderer = new SarifViolationOrderer();
    var builders = Enumerable.Empty<SarifViolationGroupBuilder>();

    // Act
    var result = orderer.OrderGroups(builders);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void OrderGroups_SingleGroup_ReturnsSingleGroup()
  {
    // Arrange
    var orderer = new SarifViolationOrderer();
    var builder = CreateBuilder("CA1506", "Test rule");
    builder.Add(5, new List<SarifRuleViolationDetail>(), CreateTestNode());
    var builders = new[] { builder };

    // Act
    var result = orderer.OrderGroups(builders);

    // Assert
    result.Should().HaveCount(1);
    result[0].RuleId.Should().Be("CA1506");
    result[0].Count.Should().Be(5);
  }

  [Test]
  public void OrderGroups_MultipleGroups_OrdersByCountDescending()
  {
    // Arrange
    var orderer = new SarifViolationOrderer();
    var builder1 = CreateBuilder("CA1506", "Rule 1");
    builder1.Add(3, new List<SarifRuleViolationDetail>(), CreateTestNode());

    var builder2 = CreateBuilder("CA1502", "Rule 2");
    builder2.Add(10, new List<SarifRuleViolationDetail>(), CreateTestNode());

    var builder3 = CreateBuilder("CA1505", "Rule 3");
    builder3.Add(5, new List<SarifRuleViolationDetail>(), CreateTestNode());

    var builders = new[] { builder1, builder2, builder3 };

    // Act
    var result = orderer.OrderGroups(builders).ToList();

    // Assert
    result.Should().HaveCount(3);
    result[0].RuleId.Should().Be("CA1502"); // Highest count (10)
    result[0].Count.Should().Be(10);
    result[1].RuleId.Should().Be("CA1505"); // Middle count (5)
    result[1].Count.Should().Be(5);
    result[2].RuleId.Should().Be("CA1506"); // Lowest count (3)
    result[2].Count.Should().Be(3);
  }

  [Test]
  public void OrderGroups_EqualCounts_OrdersByRuleIdAscending()
  {
    // Arrange
    var orderer = new SarifViolationOrderer();
    var builder1 = CreateBuilder("CA1506", "Rule 1");
    builder1.Add(5, new List<SarifRuleViolationDetail>(), CreateTestNode());

    var builder2 = CreateBuilder("CA1502", "Rule 2");
    builder2.Add(5, new List<SarifRuleViolationDetail>(), CreateTestNode());

    var builder3 = CreateBuilder("CA1505", "Rule 3");
    builder3.Add(5, new List<SarifRuleViolationDetail>(), CreateTestNode());

    var builders = new[] { builder1, builder2, builder3 };

    // Act
    var result = orderer.OrderGroups(builders).ToList();

    // Assert
    result.Should().HaveCount(3);
    result[0].RuleId.Should().Be("CA1502"); // Alphabetically first
    result[1].RuleId.Should().Be("CA1505");
    result[2].RuleId.Should().Be("CA1506"); // Alphabetically last
  }

  [Test]
  public void OrderGroups_CaseInsensitiveRuleIdComparison_UsesOrdinalIgnoreCase()
  {
    // Arrange
    var orderer = new SarifViolationOrderer();
    var builder1 = CreateBuilder("ca1506", "Rule 1");
    builder1.Add(5, new List<SarifRuleViolationDetail>(), CreateTestNode());

    var builder2 = CreateBuilder("CA1502", "Rule 2");
    builder2.Add(5, new List<SarifRuleViolationDetail>(), CreateTestNode());

    var builder3 = CreateBuilder("CA1505", "Rule 3");
    builder3.Add(5, new List<SarifRuleViolationDetail>(), CreateTestNode());

    var builders = new[] { builder1, builder2, builder3 };

    // Act
    var result = orderer.OrderGroups(builders).ToList();

    // Assert
    result.Should().HaveCount(3);
    result[0].RuleId.Should().Be("CA1502");
    result[1].RuleId.Should().Be("CA1505");
    result[2].RuleId.Should().Be("ca1506");
  }

  [Test]
  public void OrderGroups_MixedCountsAndRuleIds_OrdersCorrectly()
  {
    // Arrange
    var orderer = new SarifViolationOrderer();
    var builder1 = CreateBuilder("CA1506", "Rule 1");
    builder1.Add(10, new List<SarifRuleViolationDetail>(), CreateTestNode()); // Highest count

    var builder2 = CreateBuilder("CA1502", "Rule 2");
    builder2.Add(5, new List<SarifRuleViolationDetail>(), CreateTestNode()); // Equal counts below

    var builder3 = CreateBuilder("CA1505", "Rule 3");
    builder3.Add(5, new List<SarifRuleViolationDetail>(), CreateTestNode()); // Equal counts below

    var builder4 = CreateBuilder("CA1501", "Rule 4");
    builder4.Add(3, new List<SarifRuleViolationDetail>(), CreateTestNode()); // Lowest count

    var builders = new[] { builder1, builder2, builder3, builder4 };

    // Act
    var result = orderer.OrderGroups(builders).ToList();

    // Assert
    result.Should().HaveCount(4);
    result[0].RuleId.Should().Be("CA1506"); // Highest count (10)
    result[1].RuleId.Should().Be("CA1502"); // Count 5, alphabetically first
    result[2].RuleId.Should().Be("CA1505"); // Count 5, alphabetically second
    result[3].RuleId.Should().Be("CA1501"); // Lowest count (3)
  }

  [Test]
  public void OrderGroups_ZeroCounts_OrdersByRuleId()
  {
    // Arrange
    var orderer = new SarifViolationOrderer();
    var builder1 = CreateBuilder("CA1506", "Rule 1");
    // No Add call, count stays 0

    var builder2 = CreateBuilder("CA1502", "Rule 2");
    // No Add call, count stays 0

    var builders = new[] { builder1, builder2 };

    // Act
    var result = orderer.OrderGroups(builders).ToList();

    // Assert
    result.Should().HaveCount(2);
    result[0].Count.Should().Be(0);
    result[1].Count.Should().Be(0);
    result[0].RuleId.Should().Be("CA1502"); // Alphabetically first
    result[1].RuleId.Should().Be("CA1506");
  }

  [Test]
  public void OrderGroups_NullShortDescription_HandlesNull()
  {
    // Arrange
    var orderer = new SarifViolationOrderer();
    var builder = CreateBuilder("CA1506");
    builder.Add(5, new List<SarifRuleViolationDetail>(), CreateTestNode());
    var builders = new[] { builder };

    // Act
    var result = orderer.OrderGroups(builders);

    // Assert
    result.Should().HaveCount(1);
    result[0].ShortDescription.Should().BeNull();
  }

  private static SarifViolationGroupBuilder CreateBuilder(string ruleId, string? description = null)
    => new(ruleId, description, MetricIdentifier.SarifCaRuleViolations);

  private static TypeMetricsNode CreateTestNode()
  {
    return new TypeMetricsNode
    {
      Name = "TestType",
      FullyQualifiedName = "Rca.Loader.Services.TestType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };
  }
}

