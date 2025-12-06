namespace MetricsReporter.Tests.MetricsReader;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.MetricsReader.Commands;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Integration-style tests for <see cref="TestMetricCommand"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
[Parallelizable(ParallelScope.None)]
internal sealed class TestMetricCommandTests : MetricsReaderCommandTestsBase
{
  [Test]
  public async Task ExecuteAsync_WhenSymbolViolatesThreshold_ReturnsFailure()
  {
    // Arrange
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.FailingType", 35, ThresholdStatus.Error)
    });
    var reportPath = WriteReport(report);
    var settings = CreateTestSettings(reportPath, "Rca.Loader.Services.FailingType");

    // Act
    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunTestCommandAsync<TestMetricCommand>(settings)
      .ConfigureAwait(false);

    // Assert
    exitCode.Should().Be(0);
    var json = JsonDocument.Parse(output).RootElement;
    json.GetProperty("isOk").GetBoolean().Should().BeFalse();
    var details = json.GetProperty("details");
    details.GetProperty("symbolFqn").GetString().Should().Be("Rca.Loader.Services.FailingType");
    details.GetProperty("status").GetString().Should().Be("Error");
  }

  [Test]
  public async Task ExecuteAsync_WhenSymbolWithinThreshold_ReturnsSuccess()
  {
    // Arrange
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.CleanType", 5, ThresholdStatus.Success)
    });
    var reportPath = WriteReport(report);
    var settings = CreateTestSettings(reportPath, "Rca.Loader.Services.CleanType");

    // Act
    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunTestCommandAsync<TestMetricCommand>(settings)
      .ConfigureAwait(false);

    // Assert
    exitCode.Should().Be(0);
    var json = JsonDocument.Parse(output).RootElement;
    json.GetProperty("isOk").GetBoolean().Should().BeTrue();
    json.GetProperty("details").GetProperty("status").GetString().Should().Be("Success");
  }

  [Test]
  public async Task ExecuteAsync_WhenSymbolSuppressedAndNotIncluded_ReturnsSuccess()
  {
    // Arrange
    const string suppressedFqn = "Rca.Loader.Services.SuppressedType";
    var suppressedInfo = new SuppressedSymbolInfo
    {
      FullyQualifiedName = suppressedFqn,
      Metric = MetricIdentifier.RoslynCyclomaticComplexity.ToString(),
      RuleId = "CA1502",
      FilePath = "src/Rca.Loader/SuppressedType.cs"
    };

    var report = MetricsReaderCommandTestData.CreateReport(
      new[] { MetricsReaderCommandTestData.CreateTypeNode(suppressedFqn, 50, ThresholdStatus.Error) },
      new[] { suppressedInfo });
    var reportPath = WriteReport(report);
    var settings = CreateTestSettings(reportPath, suppressedFqn);

    // Act
    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunTestCommandAsync<TestMetricCommand>(settings)
      .ConfigureAwait(false);

    // Assert
    exitCode.Should().Be(0);
    var json = JsonDocument.Parse(output).RootElement;
    json.GetProperty("isOk").GetBoolean().Should().BeTrue("suppressed entries are ignored by default");
  }

  [Test]
  public async Task ExecuteAsync_WhenSymbolSuppressedAndIncluded_ReturnsFailure()
  {
    // Arrange
    const string suppressedFqn = "Rca.Loader.Services.SuppressedType";
    var suppressedInfo = new SuppressedSymbolInfo
    {
      FullyQualifiedName = suppressedFqn,
      Metric = MetricIdentifier.RoslynCyclomaticComplexity.ToString(),
      RuleId = "CA1502",
      FilePath = "src/Rca.Loader/SuppressedType.cs"
    };

    var report = MetricsReaderCommandTestData.CreateReport(
      new[] { MetricsReaderCommandTestData.CreateTypeNode(suppressedFqn, 50, ThresholdStatus.Error) },
      new[] { suppressedInfo });
    var reportPath = WriteReport(report);
    var settings = CreateTestSettings(reportPath, suppressedFqn, includeSuppressed: true);

    // Act
    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunTestCommandAsync<TestMetricCommand>(settings)
      .ConfigureAwait(false);

    // Assert
    exitCode.Should().Be(0);
    JsonDocument.Parse(output).RootElement.GetProperty("isOk").GetBoolean().Should().BeFalse();
  }

  [Test]
  public async Task ExecuteAsync_WhenSymbolMissing_ReturnsSuccessWithMessage()
  {
    // Arrange
    var report = MetricsReaderCommandTestData.CreateReport(Array.Empty<TypeMetricsNode>());
    var reportPath = WriteReport(report);
    var settings = CreateTestSettings(reportPath, "Rca.Loader.Services.NotPresent");

    // Act
    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunTestCommandAsync<TestMetricCommand>(settings)
      .ConfigureAwait(false);

    // Assert
    exitCode.Should().Be(0);
    var json = JsonDocument.Parse(output).RootElement;
    json.GetProperty("isOk").GetBoolean().Should().BeTrue();
    var message = json.GetProperty("message").GetString();
    message.Should().NotBeNull();
    message!.Contains("not present", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    json.GetProperty("details").ValueKind.Should().Be(JsonValueKind.Null);
  }

  [Test]
  public async Task ExecuteAsync_WhenMemberViolatesThreshold_ReturnsMemberDetails()
  {
    // Arrange
    var member = MetricsReaderCommandTestData.CreateMemberNode(
      "Rca.Loader.Services.MemberType.Process(...)", 30, ThresholdStatus.Error);
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.MemberType",
      5,
      ThresholdStatus.Success,
      new[] { member });
    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateTestSettings(reportPath, "Rca.Loader.Services.MemberType.Process(...)");

    // Act
    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunTestCommandAsync<TestMetricCommand>(settings)
      .ConfigureAwait(false);

    // Assert
    exitCode.Should().Be(0);
    var details = JsonDocument.Parse(output).RootElement.GetProperty("details");
    details.GetProperty("symbolType").GetString().Should().Be("Member");
    details.GetProperty("symbolFqn").GetString().Should().Contain("Process(...)");
  }

  [Test]
  public async Task ExecuteAsync_WithThresholdOverride_UsesOverrideThreshold()
  {
    // Arrange
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.OverrideTarget", 12, ThresholdStatus.Warning)
    });
    var reportPath = WriteReport(report);
    var overridePath = WriteThresholdOverride(5, 6);
    var settings = CreateTestSettings(reportPath, "Rca.Loader.Services.OverrideTarget", thresholdsFile: overridePath);

    // Act
    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunTestCommandAsync<TestMetricCommand>(settings)
      .ConfigureAwait(false);

    // Assert
    exitCode.Should().Be(0);
    var details = JsonDocument.Parse(output).RootElement.GetProperty("details");
    details.GetProperty("threshold").GetDecimal().Should().Be(5);
  }
}



