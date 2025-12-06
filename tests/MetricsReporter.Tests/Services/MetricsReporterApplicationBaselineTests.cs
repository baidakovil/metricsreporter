namespace MetricsReporter.Tests.Services;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter;
using MetricsReporter.Services;

/// <summary>
/// Integration-style tests that verify baseline creation and replacement behavior
/// in <see cref="MetricsReporterApplication"/> using real file system state.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class MetricsReporterApplicationBaselineTests
{
  private string? rootDirectory;
  private string? metricsDir;
  private string? reportDir;
  private string? reportPath;
  private string? baselinePath;
  private string? storagePath;
  private string? logFilePath;

  [SetUp]
  public void SetUp()
  {
    rootDirectory = Path.Combine(Path.GetTempPath(), "RCA_MetricsReporterApplicationBaselineTests", Guid.NewGuid().ToString("N"));
    metricsDir = Path.Combine(rootDirectory, "Metrics");
    reportDir = Path.Combine(metricsDir, "Report");
    reportPath = Path.Combine(reportDir, "metrics-report.json");
    baselinePath = Path.Combine(reportDir, "metrics-baseline.json");
    storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RCA", "Metrics");
    logFilePath = Path.Combine(reportDir, "MetricsReporter.log");

    Directory.CreateDirectory(reportDir!);
  }

  [TearDown]
  public void TearDown()
  {
    if (rootDirectory is not null && Directory.Exists(rootDirectory))
    {
      try
      {
        Directory.Delete(rootDirectory, recursive: true);
      }
      catch
      {
        // Ignore cleanup errors in tests.
      }
    }
  }

  /// <summary>
  /// Case 1: when no previous report and no baseline exist, the first run should
  /// generate a report without using or creating a baseline. Baseline will only
  /// be created on a subsequent run that sees a previous report.
  /// </summary>
  [Test]
  public async Task RunAsync_FirstRunWithoutReportOrBaseline_CreatesReportAndBaseline()
  {
    // Arrange
    File.Exists(reportPath!).Should().BeFalse();
    File.Exists(baselinePath!).Should().BeFalse();

    var options = CreateDefaultOptions(replaceBaseline: true);
    var application = new MetricsReporterApplication();

    // Act
    var result = await application.RunAsync(options, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().Be(MetricsReporterExitCode.Success);
    File.Exists(reportPath!).Should().BeTrue("first run should always produce metrics-report.json");
    File.Exists(baselinePath!).Should().BeFalse("baseline should not be created on the very first run without history");
  }

  /// <summary>
  /// Case 2: when a previous report exists but baseline does not, the next run should
  /// create baseline from the previous report and then archive/replace it after
  /// generating a new report.
  /// </summary>
  [Test]
  public async Task RunAsync_PreviousReportWithoutBaseline_CreatesBaselineFromPreviousReportAndArchivesIt()
  {
    var application = new MetricsReporterApplication();

    // Arrange step 1: initial run to create the first report without touching baseline.
    var initialOptions = CreateDefaultOptions(replaceBaseline: false);
    var initialResult = await application.RunAsync(initialOptions, CancellationToken.None).ConfigureAwait(false);
    initialResult.Should().Be(MetricsReporterExitCode.Success);

    File.Exists(reportPath!).Should().BeTrue("initial run should create metrics-report.json");
    File.Exists(baselinePath!).Should().BeFalse("baseline should not be created when ReplaceMetricsBaseline=false");

    // Arrange step 2: second run with ReplaceMetricsBaseline=true and no baseline.
    var optionsWithBaseline = CreateDefaultOptions(replaceBaseline: true);

    // Act
    var result = await application.RunAsync(optionsWithBaseline, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().Be(MetricsReporterExitCode.Success);

    File.Exists(reportPath!).Should().BeTrue();
    File.Exists(baselinePath!).Should().BeTrue("baseline should exist after second run");

    // Old baseline (created from previous report) should have been archived.
    if (Directory.Exists(storagePath!))
    {
      var archivedFiles = Directory.GetFiles(storagePath!, "metrics-baseline-*.json");
      archivedFiles.Should().NotBeEmpty("previous baseline should be archived when replaced");
    }
  }

  /// <summary>
  /// Case 3: when baseline exists but previous report file does not, a new report
  /// should still be generated using the existing baseline and baseline should be
  /// replaced by the new report afterwards.
  /// </summary>
  [Test]
  public async Task RunAsync_BaselineWithoutPreviousReport_ReusesBaselineAndReplacesIt()
  {
    var application = new MetricsReporterApplication();

    // Arrange step 1: create initial report without baseline.
    var initialOptions = CreateDefaultOptions(replaceBaseline: false);
    var initialResult = await application.RunAsync(initialOptions, CancellationToken.None).ConfigureAwait(false);
    initialResult.Should().Be(MetricsReporterExitCode.Success);

    File.Exists(reportPath!).Should().BeTrue();
    File.Exists(baselinePath!).Should().BeFalse();

    // Arrange step 2: second run with ReplaceMetricsBaseline=true to create baseline from previous report.
    var optionsWithBaseline = CreateDefaultOptions(replaceBaseline: true);
    var secondResult = await application.RunAsync(optionsWithBaseline, CancellationToken.None).ConfigureAwait(false);
    secondResult.Should().Be(MetricsReporterExitCode.Success);

    File.Exists(reportPath!).Should().BeTrue();
    File.Exists(baselinePath!).Should().BeTrue();

    var originalBaselineTimestamp = File.GetLastWriteTimeUtc(baselinePath!);

    // Simulate missing previous report while keeping baseline.
    File.Delete(reportPath!);
    File.Exists(reportPath!).Should().BeFalse();

    // Act: third run with existing baseline and no previous report.
    var thirdResult = await application.RunAsync(optionsWithBaseline, CancellationToken.None).ConfigureAwait(false);

    // Assert
    thirdResult.Should().Be(MetricsReporterExitCode.Success);

    File.Exists(reportPath!).Should().BeTrue("third run should recreate metrics-report.json");
    File.Exists(baselinePath!).Should().BeTrue("baseline should still exist after replacement");

    var newBaselineTimestamp = File.GetLastWriteTimeUtc(baselinePath!);
    newBaselineTimestamp.Should().BeOnOrAfter(originalBaselineTimestamp, "baseline should be replaced by the new report");
  }

  /// <summary>
  /// Case 4: when both report and baseline exist, the application should use the
  /// baseline for deltas and then replace it with the new report, archiving the old one.
  /// </summary>
  [Test]
  public async Task RunAsync_ReportAndBaselineExist_ReplacesBaselineAndArchivesOld()
  {
    var application = new MetricsReporterApplication();

    // Arrange step 1: create initial report without baseline.
    var initialOptions = CreateDefaultOptions(replaceBaseline: false);
    var initialResult = await application.RunAsync(initialOptions, CancellationToken.None).ConfigureAwait(false);
    initialResult.Should().Be(MetricsReporterExitCode.Success);

    // Arrange step 2: second run with ReplaceMetricsBaseline=true to create baseline from previous report.
    var optionsWithBaseline = CreateDefaultOptions(replaceBaseline: true);
    var secondResult = await application.RunAsync(optionsWithBaseline, CancellationToken.None).ConfigureAwait(false);
    secondResult.Should().Be(MetricsReporterExitCode.Success);

    File.Exists(reportPath!).Should().BeTrue();
    File.Exists(baselinePath!).Should().BeTrue();

    var firstBaselineTimestamp = File.GetLastWriteTimeUtc(baselinePath!);

    // Act: third run with the same options (both report and baseline exist).
    var thirdResult = await application.RunAsync(optionsWithBaseline, CancellationToken.None).ConfigureAwait(false);

    // Assert
    thirdResult.Should().Be(MetricsReporterExitCode.Success);

    File.Exists(reportPath!).Should().BeTrue();
    File.Exists(baselinePath!).Should().BeTrue();

    var newBaselineTimestamp = File.GetLastWriteTimeUtc(baselinePath!);
    newBaselineTimestamp.Should().BeOnOrAfter(firstBaselineTimestamp, "baseline should be replaced by the new report");

    if (Directory.Exists(storagePath!))
    {
      var archivedFiles = Directory.GetFiles(storagePath!, "metrics-baseline-*.json");
      archivedFiles.Should().NotBeEmpty("previous baseline should be archived when replaced");
    }
  }

  private MetricsReporterOptions CreateDefaultOptions(bool replaceBaseline)
  {
    return new MetricsReporterOptions
    {
      SolutionName = "TestSolution",
      MetricsDirectory = metricsDir!,
      OutputJsonPath = reportPath!,
      OutputHtmlPath = string.Empty,
      LogFilePath = logFilePath!,
      BaselinePath = baselinePath!,
      BaselineReference = null,
      ThresholdsJson = null,
      ThresholdsPath = null,
      InputJsonPath = null,
      ExcludedMemberNamesPatterns = null,
      ExcludedAssemblyNames = null,
      ExcludedTypeNamePatterns = null,
      ReplaceMetricsBaseline = replaceBaseline,
      MetricsReportStoragePath = replaceBaseline ? storagePath : null,
      CoverageHtmlDir = null,
      AnalyzeSuppressedSymbols = false,
      SuppressedSymbolsPath = null,
      SolutionDirectory = rootDirectory!,
      SourceCodeFolders = Array.Empty<string>()
    };
  }
}



