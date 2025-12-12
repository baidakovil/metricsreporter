namespace MetricsReporter.Tests.Services;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter;
using MetricsReporter.Services;
using MetricsReporter.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Unit tests for <see cref="BaselineLifecycleService"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class BaselineLifecycleServiceTests
{
  private string? testDirectory;
  private string? reportPath;
  private string? baselinePath;
  private BaselineLifecycleService? service;

  [SetUp]
  public void SetUp()
  {
    testDirectory = Path.Combine(Path.GetTempPath(), "RCA_BaselineLifecycleServiceTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(testDirectory!);
    reportPath = Path.Combine(testDirectory, "report.json");
    baselinePath = Path.Combine(testDirectory, "baseline.json");
    service = new BaselineLifecycleService();
  }

  [TearDown]
  public void TearDown()
  {
    if (testDirectory is not null && Directory.Exists(testDirectory))
    {
      try
      {
        Directory.Delete(testDirectory, recursive: true);
      }
      catch
      {
        // Ignore cleanup errors in tests.
      }
    }
  }

  [Test]
  public void CaptureContext_WithReportAndBaselineAtStart_ReturnsCorrectContext()
  {
    // Arrange
    File.WriteAllText(reportPath!, "{}");
    File.WriteAllText(baselinePath!, "{}");

    var options = new MetricsReporterOptions
    {
      OutputJsonPath = reportPath!,
      BaselinePath = baselinePath!,
      ReplaceMetricsBaseline = true
    };

    // Act
    var context = service!.CaptureContext(options);

    // Assert
    context.HadReportAtStart.Should().BeTrue();
    context.HadBaselineAtStart.Should().BeTrue();
    context.ReplaceBaselineEnabled.Should().BeTrue();
  }

  [Test]
  public void CaptureContext_WithoutReportAndBaseline_ReturnsContextIndicatingNoHistory()
  {
    // Arrange
    var options = new MetricsReporterOptions
    {
      OutputJsonPath = reportPath!,
      BaselinePath = baselinePath!,
      ReplaceMetricsBaseline = false
    };

    // Act
    var context = service!.CaptureContext(options);

    // Assert
    context.HadReportAtStart.Should().BeFalse();
    context.HadBaselineAtStart.Should().BeFalse();
    context.ReplaceBaselineEnabled.Should().BeFalse();
  }

  [Test]
  public void CaptureContext_WithNullOutputJsonPath_ReturnsFalseForHadReportAtStart()
  {
    // Arrange
    var options = new MetricsReporterOptions
    {
      OutputJsonPath = string.Empty,
      BaselinePath = baselinePath!,
      ReplaceMetricsBaseline = true
    };

    // Act
    var context = service!.CaptureContext(options);

    // Assert
    context.HadReportAtStart.Should().BeFalse();
  }

  [Test]
  public void CaptureContext_WithEmptyOutputJsonPath_ReturnsFalseForHadReportAtStart()
  {
    // Arrange
    var options = new MetricsReporterOptions
    {
      OutputJsonPath = string.Empty,
      BaselinePath = baselinePath!,
      ReplaceMetricsBaseline = true
    };

    // Act
    var context = service!.CaptureContext(options);

    // Assert
    context.HadReportAtStart.Should().BeFalse();
  }

  [Test]
  public void CaptureContext_WithWhitespaceOutputJsonPath_ReturnsFalseForHadReportAtStart()
  {
    // Arrange
    var options = new MetricsReporterOptions
    {
      OutputJsonPath = "   ",
      BaselinePath = baselinePath!,
      ReplaceMetricsBaseline = true
    };

    // Act
    var context = service!.CaptureContext(options);

    // Assert
    context.HadReportAtStart.Should().BeFalse();
  }

  [Test]
  public void CaptureContext_WithNonExistentReportPath_ReturnsFalseForHadReportAtStart()
  {
    // Arrange
    var nonExistentPath = Path.Combine(testDirectory!, "nonexistent.json");
    var options = new MetricsReporterOptions
    {
      OutputJsonPath = nonExistentPath,
      BaselinePath = baselinePath!,
      ReplaceMetricsBaseline = true
    };

    // Act
    var context = service!.CaptureContext(options);

    // Assert
    context.HadReportAtStart.Should().BeFalse();
  }

  [Test]
  public void CaptureContext_WithNullBaselinePath_ReturnsFalseForHadBaselineAtStart()
  {
    // Arrange
    var options = new MetricsReporterOptions
    {
      OutputJsonPath = reportPath!,
      BaselinePath = null,
      ReplaceMetricsBaseline = true
    };

    // Act
    var context = service!.CaptureContext(options);

    // Assert
    context.HadBaselineAtStart.Should().BeFalse();
  }

  [Test]
  public void LogContext_WithValidContext_LogsInformation()
  {
    // Arrange
    var context = new BaselineRunContext(true, true, true);
    var options = new MetricsReporterOptions
    {
      ReplaceMetricsBaseline = true,
      BaselinePath = baselinePath!,
      OutputJsonPath = reportPath!,
      MetricsReportStoragePath = testDirectory
    };
    var logger = new TestLogger<BaselineLifecycleService>();

    // Act
    service!.LogContext(context, options, logger);

    // Assert
    logger.Entries.Should().Contain(entry => entry.Message.Contains("Baseline debug"));
    logger.Entries.Should().Contain(entry => entry.Message.Contains("ReplaceMetricsBaseline=True"));
    logger.Entries.Should().Contain(entry => entry.Message.Contains("HadReportAtStart=True"));
    logger.Entries.Should().Contain(entry => entry.Message.Contains("HadBaselineAtStart=True"));
  }

  [Test]
  public void LogContext_WithNullBaselinePath_LogsNull()
  {
    // Arrange
    var context = new BaselineRunContext(false, false, false);
    var options = new MetricsReporterOptions
    {
      ReplaceMetricsBaseline = false,
      BaselinePath = null,
      OutputJsonPath = reportPath!,
      MetricsReportStoragePath = null
    };
    var logger = new TestLogger<BaselineLifecycleService>();

    // Act
    service!.LogContext(context, options, logger);

    // Assert
    logger.Entries.Should().Contain(entry => entry.Message.Contains("BaselinePath=(null)"));
    logger.Entries.Should().Contain(entry => entry.Message.Contains("MetricsReportStoragePath=(null)"));
  }

  [Test]
  public async Task InitializeBaselineAsync_WithReplaceBaselineDisabled_DoesNothing()
  {
    // Arrange
    var context = new BaselineRunContext(false, false, false);
    var options = new MetricsReporterOptions
    {
      BaselinePath = baselinePath!,
      OutputJsonPath = reportPath!,
      ReplaceMetricsBaseline = false
    };
    var logger = NullLogger<BaselineLifecycleService>.Instance;

    // Act
    await service!.InitializeBaselineAsync(context, options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    File.Exists(baselinePath!).Should().BeFalse();
  }

  [Test]
  public async Task InitializeBaselineAsync_WithNullBaselinePath_DoesNothing()
  {
    // Arrange
    var context = new BaselineRunContext(true, false, true);
    var options = new MetricsReporterOptions
    {
      BaselinePath = null,
      OutputJsonPath = reportPath!,
      ReplaceMetricsBaseline = true
    };
    var logger = NullLogger<BaselineLifecycleService>.Instance;

    // Act
    await service!.InitializeBaselineAsync(context, options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert - Should complete without throwing
  }

  [Test]
  public async Task InitializeBaselineAsync_WithBaselineAlreadyExisting_DoesNothing()
  {
    // Arrange
    File.WriteAllText(baselinePath!, "{}");
    var context = new BaselineRunContext(true, true, true);
    var options = new MetricsReporterOptions
    {
      BaselinePath = baselinePath!,
      OutputJsonPath = reportPath!,
      ReplaceMetricsBaseline = true
    };
    var logger = NullLogger<BaselineLifecycleService>.Instance;

    // Act
    await service!.InitializeBaselineAsync(context, options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert - Baseline should remain unchanged
    var baselineContent = File.ReadAllText(baselinePath!);
    baselineContent.Should().Be("{}");
  }

  [Test]
  public async Task InitializeBaselineAsync_WithPreviousReport_CreatesBaselineFromReport()
  {
    // Arrange
    var reportContent = "{\"Solution\":{\"Name\":\"Test\"}}";
    File.WriteAllText(reportPath!, reportContent);
    var context = new BaselineRunContext(true, false, true);
    var options = new MetricsReporterOptions
    {
      BaselinePath = baselinePath!,
      OutputJsonPath = reportPath!,
      ReplaceMetricsBaseline = true
    };
    var logger = NullLogger<BaselineLifecycleService>.Instance;

    // Act
    await service!.InitializeBaselineAsync(context, options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    File.Exists(baselinePath!).Should().BeTrue();
    var baselineContent = File.ReadAllText(baselinePath!);
    baselineContent.Should().Be(reportContent);
  }

  [Test]
  public async Task InitializeBaselineAsync_WithoutPreviousReport_LogsMessageAndDoesNothing()
  {
    // Arrange
    var context = new BaselineRunContext(false, false, true);
    var options = new MetricsReporterOptions
    {
      BaselinePath = baselinePath!,
      OutputJsonPath = reportPath!,
      ReplaceMetricsBaseline = true
    };
    var logger = new TestLogger<BaselineLifecycleService>();

    // Act
    await service!.InitializeBaselineAsync(context, options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    File.Exists(baselinePath!).Should().BeFalse();
    logger.Entries.Should().Contain(entry => entry.Message.Contains("Baseline does not exist and previous report not found"));
  }

  [Test]
  public async Task LoadBaselineAsync_WithNullPath_ReturnsNull()
  {
    // Act
    var result = await service!.LoadBaselineAsync(null, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public async Task LoadBaselineAsync_WithNonExistentPath_ReturnsNull()
  {
    // Arrange
    var nonExistentPath = Path.Combine(testDirectory!, "nonexistent.json");

    // Act
    var result = await service!.LoadBaselineAsync(nonExistentPath, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public async Task LoadBaselineAsync_WithExistingBaseline_ReturnsReport()
  {
    // Arrange
    var baselineContent = "{\"Solution\":{\"Name\":\"TestSolution\",\"Assemblies\":[]}}";
    File.WriteAllText(baselinePath!, baselineContent);

    // Act
    var result = await service!.LoadBaselineAsync(baselinePath!, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().NotBeNull();
    result!.Solution.Name.Should().Be("TestSolution");
  }

  [Test]
  public async Task ReplaceBaselineAsync_WithReplaceBaselineDisabled_DoesNothing()
  {
    // Arrange
    var context = new BaselineRunContext(false, false, false);
    var options = new MetricsReporterOptions
    {
      BaselinePath = baselinePath!,
      OutputJsonPath = reportPath!,
      ReplaceMetricsBaseline = false
    };
    var logger = NullLogger<BaselineLifecycleService>.Instance;

    // Act
    await service!.ReplaceBaselineAsync(context, options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert - Should complete without throwing
  }

  [Test]
  public async Task ReplaceBaselineAsync_WithNullBaselinePath_DoesNothing()
  {
    // Arrange
    var context = new BaselineRunContext(true, true, true);
    var options = new MetricsReporterOptions
    {
      BaselinePath = null,
      OutputJsonPath = reportPath!,
      ReplaceMetricsBaseline = true
    };
    var logger = NullLogger<BaselineLifecycleService>.Instance;

    // Act
    await service!.ReplaceBaselineAsync(context, options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert - Should complete without throwing
  }

  [Test]
  public async Task ReplaceBaselineAsync_WithoutReportOrBaselineAtStart_DoesNothing()
  {
    // Arrange
    var context = new BaselineRunContext(false, false, true);
    var options = new MetricsReporterOptions
    {
      BaselinePath = baselinePath!,
      OutputJsonPath = reportPath!,
      ReplaceMetricsBaseline = true
    };
    var logger = NullLogger<BaselineLifecycleService>.Instance;

    // Act
    await service!.ReplaceBaselineAsync(context, options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert - Should complete without throwing
  }

  [Test]
  public async Task ReplaceBaselineAsync_WithExistingReportAndBaseline_ReplacesBaseline()
  {
    // Arrange
    var reportContent = "{\"Solution\":{\"Name\":\"NewReport\",\"Assemblies\":[]}}";
    var baselineContent = "{\"Solution\":{\"Name\":\"OldBaseline\",\"Assemblies\":[]}}";
    File.WriteAllText(reportPath!, reportContent);
    File.WriteAllText(baselinePath!, baselineContent);

    var context = new BaselineRunContext(true, true, true);
    var options = new MetricsReporterOptions
    {
      BaselinePath = baselinePath!,
      OutputJsonPath = reportPath!,
      MetricsReportStoragePath = testDirectory,
      ReplaceMetricsBaseline = true
    };
    var logger = NullLogger<BaselineLifecycleService>.Instance;

    // Act
    await service!.ReplaceBaselineAsync(context, options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    File.Exists(baselinePath!).Should().BeTrue();
    var newBaselineContent = File.ReadAllText(baselinePath!);
    newBaselineContent.Should().Contain("NewReport");
  }
}


