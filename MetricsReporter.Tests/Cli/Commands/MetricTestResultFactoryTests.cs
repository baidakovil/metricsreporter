using FluentAssertions;
using MetricsReporter.Cli.Commands;
using MetricsReporter.Model;
using MetricsReporter.MetricsReader.Services;
using NUnit.Framework;

namespace MetricsReporter.Tests.Cli.Commands;

[TestFixture]
[Category("Unit")]
internal sealed class MetricTestResultFactoryTests
{
  private MetricTestResultFactory _factory = null!;

  [SetUp]
  public void SetUp()
  {
    _factory = new MetricTestResultFactory();
  }

  [Test]
  public void Create_WhenSnapshotIsNull_ReturnsOkWithMessage()
  {
    // Act
    var result = _factory.Create(null, includeSuppressed: false);

    // Assert
    result.IsOk.Should().BeTrue();
    result.Details.Should().BeNull();
    result.Message.Should().NotBeNullOrWhiteSpace();
  }

  [Test]
  public void Create_WhenSnapshotSuppressedAndExcludingSuppressed_ReturnsOk()
  {
    // Arrange
    var snapshot = new SymbolMetricSnapshot(
      "Company.Type",
      CodeElementKind.Type,
      null,
      MetricIdentifier.RoslynClassCoupling,
      new MetricValue { Status = ThresholdStatus.Warning },
      null,
      IsSuppressed: true);

    // Act
    var result = _factory.Create(snapshot, includeSuppressed: false);

    // Assert
    result.IsOk.Should().BeTrue();
    result.Details.Should().NotBeNull();
  }

  [Test]
  public void Create_WhenSnapshotHasWarningAndIncludedSuppressed_ReturnsFailure()
  {
    // Arrange
    var snapshot = new SymbolMetricSnapshot(
      "Company.Type",
      CodeElementKind.Type,
      null,
      MetricIdentifier.RoslynClassCoupling,
      new MetricValue { Status = ThresholdStatus.Warning },
      null,
      IsSuppressed: false);

    // Act
    var result = _factory.Create(snapshot, includeSuppressed: true);

    // Assert
    result.IsOk.Should().BeFalse();
    result.Details.Should().NotBeNull();
    result.Message.Should().BeNull();
  }
}

