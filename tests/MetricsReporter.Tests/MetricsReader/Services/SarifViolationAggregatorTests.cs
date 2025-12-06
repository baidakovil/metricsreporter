namespace MetricsReporter.Tests.MetricsReader.Services;

using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using MetricsReporter.Tests.MetricsReader;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Unit tests for <see cref="SarifViolationAggregator"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class SarifViolationAggregatorTests
{
  private ISuppressedSymbolChecker? _mockSuppressedChecker;

  [SetUp]
  public void SetUp()
  {
    _mockSuppressedChecker = Substitute.For<ISuppressedSymbolChecker>();
  }

  [Test]
  public void Constructor_NullSuppressedChecker_ThrowsArgumentNullException()
  {
    // Act
    var act = () => new SarifViolationAggregator(null!);

    // Assert
    act.Should().Throw<System.ArgumentNullException>()
      .WithParameterName("suppressedSymbolChecker");
  }

  [Test]
  public void AggregateViolations_EmptyNodes_ReturnsEmptyDictionary()
  {
    // Arrange
    var aggregator = new SarifViolationAggregator(_mockSuppressedChecker!);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.SarifCaRuleViolations, MetricsReaderSymbolKind.Any, false);

    // Act
    var result = aggregator.AggregateViolations(Enumerable.Empty<MetricsNode>(), filter, null);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void AggregateViolations_NodeWithoutMetric_ReturnsEmptyDictionary()
  {
    // Arrange
    var metrics = new Dictionary<MetricIdentifier, MetricValue>(); // Empty metrics
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", metrics);

    var aggregator = new SarifViolationAggregator(_mockSuppressedChecker!);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.SarifCaRuleViolations, MetricsReaderSymbolKind.Any, false);

    // Act
    var result = aggregator.AggregateViolations(new[] { typeNode }, filter, null);

    // Assert
    result.Should().BeEmpty();
    _mockSuppressedChecker!.DidNotReceive().IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>(), Arg.Any<string?>());
  }

  [Test]
  public void AggregateViolations_NodeWithoutBreakdown_ReturnsEmptyDictionary()
  {
    // Arrange
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
      {
        Value = 10,
        Status = ThresholdStatus.Success,
        Breakdown = null
      }
    };
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", metrics);

    var aggregator = new SarifViolationAggregator(_mockSuppressedChecker!);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.SarifCaRuleViolations, MetricsReaderSymbolKind.Any, false);

    // Act
    var result = aggregator.AggregateViolations(new[] { typeNode }, filter, null);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void AggregateViolations_NodeWithEmptyBreakdown_ReturnsEmptyDictionary()
  {
    // Arrange
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
      {
        Value = 10,
        Status = ThresholdStatus.Success,
        Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>()
      }
    };
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", metrics);

    var aggregator = new SarifViolationAggregator(_mockSuppressedChecker!);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.SarifCaRuleViolations, MetricsReaderSymbolKind.Any, false);

    // Act
    var result = aggregator.AggregateViolations(new[] { typeNode }, filter, null);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void AggregateViolations_NodeWithBreakdown_CreatesGroup()
  {
    // Arrange
    var breakdownEntry = new SarifRuleBreakdownEntry
    {
      Count = 5,
      Violations = new List<SarifRuleViolationDetail>
      {
        new SarifRuleViolationDetail { Message = "Test violation", Uri = "file://test.cs", StartLine = 10, EndLine = 10 }
      }
    };
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
      {
        Value = 10,
        Status = ThresholdStatus.Success,
        Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
        {
          ["CA1506"] = breakdownEntry
        }
      }
    };
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", metrics);

    _mockSuppressedChecker!.IsSuppressed("Rca.Loader.Services.Type", MetricIdentifier.SarifCaRuleViolations, "CA1506")
      .Returns(false);

    var aggregator = new SarifViolationAggregator(_mockSuppressedChecker);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.SarifCaRuleViolations, MetricsReaderSymbolKind.Any, false);

    // Act
    var result = aggregator.AggregateViolations(new[] { typeNode }, filter, null);

    // Assert
    result.Should().HaveCount(1);
    result.Should().ContainKey("CA1506");
    var group = result["CA1506"];
    group.RuleId.Should().Be("CA1506");
    group.Count.Should().Be(5);
    group.Violations.Should().HaveCount(1);
  }

  [Test]
  public void AggregateViolations_SuppressedViolationWithIncludeSuppressedFalse_ExcludesViolation()
  {
    // Arrange
    var breakdownEntry = new SarifRuleBreakdownEntry
    {
      Count = 5,
      Violations = new List<SarifRuleViolationDetail>()
    };
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
      {
        Value = 10,
        Status = ThresholdStatus.Success,
        Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
        {
          ["CA1506"] = breakdownEntry
        }
      }
    };
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", metrics);

    _mockSuppressedChecker!.IsSuppressed("Rca.Loader.Services.Type", MetricIdentifier.SarifCaRuleViolations, "CA1506")
      .Returns(true);

    var aggregator = new SarifViolationAggregator(_mockSuppressedChecker);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.SarifCaRuleViolations, MetricsReaderSymbolKind.Any, false);

    // Act
    var result = aggregator.AggregateViolations(new[] { typeNode }, filter, null);

    // Assert
    result.Should().BeEmpty();
    _mockSuppressedChecker.Received(1).IsSuppressed("Rca.Loader.Services.Type", MetricIdentifier.SarifCaRuleViolations, "CA1506");
  }

  [Test]
  public void AggregateViolations_SuppressedViolationWithIncludeSuppressedTrue_IncludesViolation()
  {
    // Arrange
    var breakdownEntry = new SarifRuleBreakdownEntry
    {
      Count = 5,
      Violations = new List<SarifRuleViolationDetail>()
    };
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
      {
        Value = 10,
        Status = ThresholdStatus.Success,
        Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
        {
          ["CA1506"] = breakdownEntry
        }
      }
    };
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", metrics);

    _mockSuppressedChecker!.IsSuppressed("Rca.Loader.Services.Type", MetricIdentifier.SarifCaRuleViolations, "CA1506")
      .Returns(true);

    var aggregator = new SarifViolationAggregator(_mockSuppressedChecker);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.SarifCaRuleViolations, MetricsReaderSymbolKind.Any, true);

    // Act
    var result = aggregator.AggregateViolations(new[] { typeNode }, filter, null);

    // Assert
    result.Should().HaveCount(1);
    result.Should().ContainKey("CA1506");
    _mockSuppressedChecker.DidNotReceive().IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>(), Arg.Any<string?>());
  }

  [Test]
  public void AggregateViolations_MultipleRules_CreatesMultipleGroups()
  {
    // Arrange
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
      {
        Value = 10,
        Status = ThresholdStatus.Success,
        Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
        {
          ["CA1506"] = new SarifRuleBreakdownEntry { Count = 5, Violations = new List<SarifRuleViolationDetail>() },
          ["CA1502"] = new SarifRuleBreakdownEntry { Count = 3, Violations = new List<SarifRuleViolationDetail>() }
        }
      }
    };
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", metrics);

    _mockSuppressedChecker!.IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>(), Arg.Any<string?>())
      .Returns(false);

    var aggregator = new SarifViolationAggregator(_mockSuppressedChecker);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.SarifCaRuleViolations, MetricsReaderSymbolKind.Any, false);

    // Act
    var result = aggregator.AggregateViolations(new[] { typeNode }, filter, null);

    // Assert
    result.Should().HaveCount(2);
    result.Should().ContainKey("CA1506");
    result.Should().ContainKey("CA1502");
    result["CA1506"].Count.Should().Be(5);
    result["CA1502"].Count.Should().Be(3);
  }

  [Test]
  public void AggregateViolations_NullBreakdownEntry_SkipsEntry()
  {
    // Arrange
    var breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
    {
      ["CA1506"] = null!,
      ["CA1502"] = new SarifRuleBreakdownEntry { Count = 3, Violations = new List<SarifRuleViolationDetail>() }
    };
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
      {
        Value = 10,
        Status = ThresholdStatus.Success,
        Breakdown = breakdown
      }
    };
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", metrics);

    _mockSuppressedChecker!.IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>(), Arg.Any<string?>())
      .Returns(false);

    var aggregator = new SarifViolationAggregator(_mockSuppressedChecker);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.SarifCaRuleViolations, MetricsReaderSymbolKind.Any, false);

    // Act
    var result = aggregator.AggregateViolations(new[] { typeNode }, filter, null);

    // Assert
    result.Should().HaveCount(1);
    result.Should().ContainKey("CA1502");
    result.Should().NotContainKey("CA1506");
  }

  [Test]
  public void AggregateViolations_RuleDescriptionProvided_UsesDescription()
  {
    // Arrange
    var breakdownEntry = new SarifRuleBreakdownEntry
    {
      Count = 5,
      Violations = new List<SarifRuleViolationDetail>()
    };
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
      {
        Value = 10,
        Status = ThresholdStatus.Success,
        Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
        {
          ["CA1506"] = breakdownEntry
        }
      }
    };
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", metrics);

    var ruleDescriptions = new Dictionary<string, RuleDescription>
    {
      ["CA1506"] = new RuleDescription { ShortDescription = "Avoid excessive class coupling" }
    };

    _mockSuppressedChecker!.IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>(), Arg.Any<string?>())
      .Returns(false);

    var aggregator = new SarifViolationAggregator(_mockSuppressedChecker);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.SarifCaRuleViolations, MetricsReaderSymbolKind.Any, false);

    // Act
    var result = aggregator.AggregateViolations(new[] { typeNode }, filter, ruleDescriptions);

    // Assert
    result.Should().HaveCount(1);
    result["CA1506"].ShortDescription.Should().Be("Avoid excessive class coupling");
  }

  [Test]
  public void AggregateViolations_MultipleNodesWithSameRule_AggregatesCounts()
  {
    // Arrange
    var breakdownEntry1 = new SarifRuleBreakdownEntry
    {
      Count = 5,
      Violations = new List<SarifRuleViolationDetail>()
    };
    var breakdownEntry2 = new SarifRuleBreakdownEntry
    {
      Count = 3,
      Violations = new List<SarifRuleViolationDetail>()
    };

    var metrics1 = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
      {
        Value = 10,
        Status = ThresholdStatus.Success,
        Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
        {
          ["CA1506"] = breakdownEntry1
        }
      }
    };
    var metrics2 = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
      {
        Value = 20,
        Status = ThresholdStatus.Success,
        Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
        {
          ["CA1506"] = breakdownEntry2
        }
      }
    };

    var typeNode1 = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type1", metrics1);
    var typeNode2 = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type2", metrics2);

    _mockSuppressedChecker!.IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>(), Arg.Any<string?>())
      .Returns(false);

    var aggregator = new SarifViolationAggregator(_mockSuppressedChecker);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.SarifCaRuleViolations, MetricsReaderSymbolKind.Any, false);

    // Act
    var result = aggregator.AggregateViolations(new[] { typeNode1, typeNode2 }, filter, null);

    // Assert
    result.Should().HaveCount(1);
    result["CA1506"].Count.Should().Be(8); // 5 + 3
  }

  [Test]
  public void AggregateViolations_CaseInsensitiveRuleId_CombinesGroups()
  {
    // Arrange
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
      {
        Value = 10,
        Status = ThresholdStatus.Success,
        Breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
        {
          ["CA1506"] = new SarifRuleBreakdownEntry { Count = 5, Violations = new List<SarifRuleViolationDetail>() },
          ["ca1506"] = new SarifRuleBreakdownEntry { Count = 3, Violations = new List<SarifRuleViolationDetail>() }
        }
      }
    };
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", metrics);

    _mockSuppressedChecker!.IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>(), Arg.Any<string?>())
      .Returns(false);

    var aggregator = new SarifViolationAggregator(_mockSuppressedChecker);
    var filter = new SymbolFilter("Rca.Loader.Services", MetricIdentifier.SarifCaRuleViolations, MetricsReaderSymbolKind.Any, false);

    // Act
    var result = aggregator.AggregateViolations(new[] { typeNode }, filter, null);

    // Assert
    result.Should().HaveCount(1);
    result.Should().ContainKey("CA1506");
    // Both entries should be added to the same group (case-insensitive)
    result["CA1506"].Count.Should().Be(8);
  }
}


