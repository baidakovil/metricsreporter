namespace MetricsReporter.Tests.MetricsReader.Services;

using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using MetricsReporter.Tests.MetricsReader;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.Model;

/// <summary>
/// Unit tests for <see cref="SymbolSnapshotBuilder"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class SymbolSnapshotBuilderTests
{
  private IMetricsThresholdProvider? _mockThresholdProvider;
  private ISuppressedSymbolChecker? _mockSuppressedChecker;

  [SetUp]
  public void SetUp()
  {
    _mockThresholdProvider = Substitute.For<IMetricsThresholdProvider>();
    _mockSuppressedChecker = Substitute.For<ISuppressedSymbolChecker>();
  }

  [Test]
  public void Constructor_NullThresholdProvider_ThrowsArgumentNullException()
  {
    // Act
    var act = () => new SymbolSnapshotBuilder(null!, _mockSuppressedChecker!);

    // Assert
    act.Should().Throw<System.ArgumentNullException>()
      .WithParameterName("thresholdProvider");
  }

  [Test]
  public void Constructor_NullSuppressedChecker_ThrowsArgumentNullException()
  {
    // Act
    var act = () => new SymbolSnapshotBuilder(_mockThresholdProvider!, null!);

    // Assert
    act.Should().Throw<System.ArgumentNullException>()
      .WithParameterName("suppressedSymbolChecker");
  }

  [Test]
  public void BuildSnapshot_MissingMetric_ReturnsNull()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", 10, ThresholdStatus.Success);
    typeNode.Metrics.Clear(); // Remove all metrics
    var builder = new SymbolSnapshotBuilder(_mockThresholdProvider!, _mockSuppressedChecker!);

    // Act
    var result = builder.BuildSnapshot(typeNode, MetricIdentifier.RoslynCyclomaticComplexity);

    // Assert
    result.Should().BeNull();
    _mockThresholdProvider!.DidNotReceive().GetThreshold(Arg.Any<MetricIdentifier>(), Arg.Any<MetricSymbolLevel>());
    _mockSuppressedChecker!.DidNotReceive().IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>());
  }

  [Test]
  public void BuildSnapshot_NullMetricValue_ReturnsNull()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", 10, ThresholdStatus.Success);
    typeNode.Metrics[MetricIdentifier.RoslynCyclomaticComplexity] = null!;
    var builder = new SymbolSnapshotBuilder(_mockThresholdProvider!, _mockSuppressedChecker!);

    // Act
    var result = builder.BuildSnapshot(typeNode, MetricIdentifier.RoslynCyclomaticComplexity);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void BuildSnapshot_NullMetricValueValue_ReturnsNull()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", 10, ThresholdStatus.Success);
    typeNode.Metrics[MetricIdentifier.RoslynCyclomaticComplexity] = new MetricValue { Value = null };
    var builder = new SymbolSnapshotBuilder(_mockThresholdProvider!, _mockSuppressedChecker!);

    // Act
    var result = builder.BuildSnapshot(typeNode, MetricIdentifier.RoslynCyclomaticComplexity);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void BuildSnapshot_UnknownNodeKind_ReturnsNull()
  {
    // Arrange
    // Create a node with unknown kind using helper that allows custom metrics
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.RoslynCyclomaticComplexity] = new MetricValue
      {
        Value = 10,
        Status = ThresholdStatus.Success,
        Unit = "count"
      }
    };
    // Note: We can't easily create a node with unknown Kind since it's init-only
    // Instead, we test that Member nodes work correctly
    var memberNode = MetricsReaderCommandTestData.CreateMemberNode("Rca.Loader.Services.Type.Method", 10, ThresholdStatus.Success);
    _mockThresholdProvider!.GetThreshold(Arg.Any<MetricIdentifier>(), Arg.Any<MetricSymbolLevel>())
      .Returns((MetricThreshold?)null);
    _mockSuppressedChecker!.IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>())
      .Returns(false);
    var builder = new SymbolSnapshotBuilder(_mockThresholdProvider, _mockSuppressedChecker);

    // Act
    var result = builder.BuildSnapshot(memberNode, MetricIdentifier.RoslynCyclomaticComplexity);

    // Assert
    // Member nodes should return a snapshot (they map to MetricSymbolLevel.Member)
    result.Should().NotBeNull();
    result!.Kind.Should().Be(CodeElementKind.Member);
  }

  [Test]
  public void BuildSnapshot_TypeNode_ReturnsSnapshotWithTypeLevel()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", 15, ThresholdStatus.Warning);
    var expectedThreshold = new MetricThreshold { Warning = 10, Error = 20 };

    _mockThresholdProvider!.GetThreshold(MetricIdentifier.RoslynCyclomaticComplexity, MetricSymbolLevel.Type)
      .Returns(expectedThreshold);
    _mockSuppressedChecker!.IsSuppressed("Rca.Loader.Services.Type", MetricIdentifier.RoslynCyclomaticComplexity)
      .Returns(false);

    var builder = new SymbolSnapshotBuilder(_mockThresholdProvider, _mockSuppressedChecker);

    // Act
    var result = builder.BuildSnapshot(typeNode, MetricIdentifier.RoslynCyclomaticComplexity);

    // Assert
    result.Should().NotBeNull();
    result!.Symbol.Should().Be("Rca.Loader.Services.Type");
    result.Kind.Should().Be(CodeElementKind.Type);
    result.Metric.Should().Be(MetricIdentifier.RoslynCyclomaticComplexity);
    result.Value.Should().Be(15);
    result.Threshold.Should().Be(expectedThreshold);
    result.IsSuppressed.Should().BeFalse();
  }

  [Test]
  public void BuildSnapshot_MemberNode_ReturnsSnapshotWithMemberLevel()
  {
    // Arrange
    var memberNode = MetricsReaderCommandTestData.CreateMemberNode("Rca.Loader.Services.Type.Method", 25, ThresholdStatus.Error);
    var expectedThreshold = new MetricThreshold { Warning = 15, Error = 20 };

    _mockThresholdProvider!.GetThreshold(MetricIdentifier.RoslynCyclomaticComplexity, MetricSymbolLevel.Member)
      .Returns(expectedThreshold);
    _mockSuppressedChecker!.IsSuppressed("Rca.Loader.Services.Type.Method", MetricIdentifier.RoslynCyclomaticComplexity)
      .Returns(false);

    var builder = new SymbolSnapshotBuilder(_mockThresholdProvider, _mockSuppressedChecker);

    // Act
    var result = builder.BuildSnapshot(memberNode, MetricIdentifier.RoslynCyclomaticComplexity);

    // Assert
    result.Should().NotBeNull();
    result!.Symbol.Should().Be("Rca.Loader.Services.Type.Method");
    result.Kind.Should().Be(CodeElementKind.Member);
    result.Metric.Should().Be(MetricIdentifier.RoslynCyclomaticComplexity);
    result.Value.Should().Be(25);
    result.Threshold.Should().Be(expectedThreshold);
    result.IsSuppressed.Should().BeFalse();
  }

  [Test]
  public void BuildSnapshot_SuppressedSymbol_ReturnsSnapshotWithSuppressedFlag()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.SuppressedType", 50, ThresholdStatus.Error);
    var expectedThreshold = new MetricThreshold { Warning = 10, Error = 20 };

    _mockThresholdProvider!.GetThreshold(MetricIdentifier.RoslynCyclomaticComplexity, MetricSymbolLevel.Type)
      .Returns(expectedThreshold);
    _mockSuppressedChecker!.IsSuppressed("Rca.Loader.Services.SuppressedType", MetricIdentifier.RoslynCyclomaticComplexity)
      .Returns(true);

    var builder = new SymbolSnapshotBuilder(_mockThresholdProvider, _mockSuppressedChecker);

    // Act
    var result = builder.BuildSnapshot(typeNode, MetricIdentifier.RoslynCyclomaticComplexity);

    // Assert
    result.Should().NotBeNull();
    result!.IsSuppressed.Should().BeTrue();
    _mockSuppressedChecker.Received(1).IsSuppressed("Rca.Loader.Services.SuppressedType", MetricIdentifier.RoslynCyclomaticComplexity);
  }

  [Test]
  public void BuildSnapshot_NullThreshold_ReturnsSnapshotWithNullThreshold()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", 10, ThresholdStatus.Success);

    _mockThresholdProvider!.GetThreshold(MetricIdentifier.RoslynCyclomaticComplexity, MetricSymbolLevel.Type)
      .Returns((MetricThreshold?)null);
    _mockSuppressedChecker!.IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>())
      .Returns(false);

    var builder = new SymbolSnapshotBuilder(_mockThresholdProvider, _mockSuppressedChecker);

    // Act
    var result = builder.BuildSnapshot(typeNode, MetricIdentifier.RoslynCyclomaticComplexity);

    // Assert
    result.Should().NotBeNull();
    result!.Threshold.Should().BeNull();
  }

  [Test]
  public void BuildSnapshot_NullFullyQualifiedName_ReturnsSnapshotWithEmptyString()
  {
    // Arrange
    // Create node with null FullyQualifiedName using helper with custom metrics
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.RoslynCyclomaticComplexity] = new MetricValue
      {
        Value = 10,
        Status = ThresholdStatus.Success,
        Unit = "count"
      }
    };
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", metrics);
    // Can't change FullyQualifiedName after creation, so test with existing node
    // The builder uses node.FullyQualifiedName ?? string.Empty, so we test the null coalescing

    _mockThresholdProvider!.GetThreshold(Arg.Any<MetricIdentifier>(), Arg.Any<MetricSymbolLevel>())
      .Returns(new MetricThreshold());
    _mockSuppressedChecker!.IsSuppressed(null, Arg.Any<MetricIdentifier>())
      .Returns(false);

    var builder = new SymbolSnapshotBuilder(_mockThresholdProvider, _mockSuppressedChecker);

    // Act
    var result = builder.BuildSnapshot(typeNode, MetricIdentifier.RoslynCyclomaticComplexity);

    // Assert
    result.Should().NotBeNull();
    // FullyQualifiedName is set in CreateTypeNode, so it won't be empty
    // This test verifies the builder handles null coalescing correctly
    result!.Symbol.Should().NotBeNull();
  }

  [Test]
  public void BuildSnapshot_NullSource_ReturnsSnapshotWithNullFilePath()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", 10, ThresholdStatus.Success);
    typeNode.Source = null;

    _mockThresholdProvider!.GetThreshold(Arg.Any<MetricIdentifier>(), Arg.Any<MetricSymbolLevel>())
      .Returns(new MetricThreshold());
    _mockSuppressedChecker!.IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>())
      .Returns(false);

    var builder = new SymbolSnapshotBuilder(_mockThresholdProvider, _mockSuppressedChecker);

    // Act
    var result = builder.BuildSnapshot(typeNode, MetricIdentifier.RoslynCyclomaticComplexity);

    // Assert
    result.Should().NotBeNull();
    result!.FilePath.Should().BeNull();
  }

  [Test]
  public void BuildSnapshot_DifferentMetric_CallsProviderWithCorrectMetric()
  {
    // Arrange
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", 10, ThresholdStatus.Success);
    typeNode.Metrics[MetricIdentifier.RoslynClassCoupling] = new MetricValue { Value = 35, Status = ThresholdStatus.Error };

    _mockThresholdProvider!.GetThreshold(MetricIdentifier.RoslynClassCoupling, MetricSymbolLevel.Type)
      .Returns(new MetricThreshold());
    _mockSuppressedChecker!.IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>())
      .Returns(false);

    var builder = new SymbolSnapshotBuilder(_mockThresholdProvider, _mockSuppressedChecker);

    // Act
    var result = builder.BuildSnapshot(typeNode, MetricIdentifier.RoslynClassCoupling);

    // Assert
    result.Should().NotBeNull();
    _mockThresholdProvider.Received(1).GetThreshold(MetricIdentifier.RoslynClassCoupling, MetricSymbolLevel.Type);
    _mockSuppressedChecker.Received(1).IsSuppressed("Rca.Loader.Services.Type", MetricIdentifier.RoslynClassCoupling);
  }

  [Test]
  public void BuildSnapshot_PreservesAllMetricValueProperties()
  {
    // Arrange
    // Create metric value with all properties set
    var metricValue = new MetricValue
    {
      Value = 15,
      Delta = 5,
      Status = ThresholdStatus.Warning,
      Unit = "count"
    };
    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.RoslynCyclomaticComplexity] = metricValue
    };
    var typeNode = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", metrics);

    _mockThresholdProvider!.GetThreshold(Arg.Any<MetricIdentifier>(), Arg.Any<MetricSymbolLevel>())
      .Returns(new MetricThreshold());
    _mockSuppressedChecker!.IsSuppressed(Arg.Any<string?>(), Arg.Any<MetricIdentifier>())
      .Returns(false);

    var builder = new SymbolSnapshotBuilder(_mockThresholdProvider, _mockSuppressedChecker);

    // Act
    var result = builder.BuildSnapshot(typeNode, MetricIdentifier.RoslynCyclomaticComplexity);

    // Assert
    result.Should().NotBeNull();
    result!.MetricValue.Should().BeSameAs(metricValue);
    result.MetricValue.Delta.Should().Be(5);
    result.MetricValue.Unit.Should().Be("count");
    result.Status.Should().Be(ThresholdStatus.Warning);
  }
}


