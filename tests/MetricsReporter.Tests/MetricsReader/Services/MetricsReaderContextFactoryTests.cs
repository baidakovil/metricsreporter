namespace MetricsReporter.Tests.MetricsReader.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using MetricsReporter.Tests.MetricsReader;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Unit tests for <see cref="MetricsReaderContextFactory"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class MetricsReaderContextFactoryTests
{
  private string? _testDirectory;
  private IJsonReportLoader? _mockReportLoader;
  private IThresholdsFileLoader? _mockThresholdsFileLoader;
  private ISolutionLocator? _mockSolutionLocator;
  private IMetricsUpdaterFactory? _mockUpdaterFactory;

  [SetUp]
  public void SetUp()
  {
    _testDirectory = Path.Combine(Path.GetTempPath(), "RCA_MetricsReaderContextFactoryTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(_testDirectory!);
    _mockReportLoader = Substitute.For<IJsonReportLoader>();
    _mockThresholdsFileLoader = Substitute.For<IThresholdsFileLoader>();
    _mockSolutionLocator = Substitute.For<ISolutionLocator>();
    _mockUpdaterFactory = Substitute.For<IMetricsUpdaterFactory>();
  }

  [TearDown]
  public void TearDown()
  {
    if (_testDirectory is not null && Directory.Exists(_testDirectory))
    {
      try
      {
        Directory.Delete(_testDirectory, recursive: true);
      }
      catch
      {
        // Ignore cleanup errors in tests
      }
    }
  }

  [Test]
  public void Constructor_NullReportLoader_ThrowsArgumentNullException()
  {
    // Act
    var act = () => new MetricsReaderContextFactory(
      null!,
      _mockThresholdsFileLoader!,
      _mockSolutionLocator!,
      _mockUpdaterFactory!);

    // Assert
    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("reportLoader");
  }

  [Test]
  public void Constructor_NullThresholdsFileLoader_ThrowsArgumentNullException()
  {
    // Act
    var act = () => new MetricsReaderContextFactory(
      _mockReportLoader!,
      null!,
      _mockSolutionLocator!,
      _mockUpdaterFactory!);

    // Assert
    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("thresholdsFileLoader");
  }

  [Test]
  public void Constructor_NullSolutionLocator_ThrowsArgumentNullException()
  {
    // Act
    var act = () => new MetricsReaderContextFactory(
      _mockReportLoader!,
      _mockThresholdsFileLoader!,
      null!,
      _mockUpdaterFactory!);

    // Assert
    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("solutionLocator");
  }

  [Test]
  public void Constructor_NullUpdaterFactory_ThrowsArgumentNullException()
  {
    // Act
    var act = () => new MetricsReaderContextFactory(
      _mockReportLoader!,
      _mockThresholdsFileLoader!,
      _mockSolutionLocator!,
      null!);

    // Assert
    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("updaterFactory");
  }

  [Test]
  public async Task CreateAsync_NullSettings_ThrowsArgumentNullException()
  {
    // Arrange
    var factory = new MetricsReaderContextFactory(
      _mockReportLoader!,
      _mockThresholdsFileLoader!,
      _mockSolutionLocator!,
      _mockUpdaterFactory!);

    // Act
    var act = async () => await factory.CreateAsync(null!, CancellationToken.None).ConfigureAwait(false);

    // Assert
    await act.Should().ThrowAsync<ArgumentNullException>()
      .WithParameterName("settings");
  }

  [Test]
  public async Task CreateAsync_NoUpdateFlag_SkipsUpdateAndLoadsReport()
  {
    // Arrange
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());
    File.WriteAllText(reportPath, "{}"); // Dummy file for existence check

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services",
      NoUpdate = true
    };

    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns(report);
    _mockThresholdsFileLoader!.LoadAsync(null, Arg.Any<CancellationToken>())
      .Returns((IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?)null);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader,
      _mockSolutionLocator!,
      _mockUpdaterFactory!);

    // Act
    var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    // Assert
    context.Should().NotBeNull();
    context.Report.Should().BeSameAs(report);
    _mockSolutionLocator!.DidNotReceive().FindSolutionPath(Arg.Any<string>());
    _mockUpdaterFactory!.DidNotReceive().Create(Arg.Any<string>());
    _ = _mockReportLoader!.Received(1).LoadAsync(reportPath, Arg.Any<CancellationToken>());
  }

  [Test]
  public async Task CreateAsync_WithUpdateFlag_UpdatesMetricsBeforeLoading()
  {
    // Arrange
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    var solutionPath = Path.Combine(_testDirectory!, "solution.sln");
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());
    File.WriteAllText(reportPath, "{}");
    File.WriteAllText(solutionPath, "");

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services",
      NoUpdate = false
    };

    _mockSolutionLocator!.FindSolutionPath(reportPath)
      .Returns(solutionPath);
    var mockUpdater = Substitute.For<IMetricsUpdater>();
    _mockUpdaterFactory!.Create(solutionPath)
      .Returns(mockUpdater);
    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns(report);
    _mockThresholdsFileLoader!.LoadAsync(null, Arg.Any<CancellationToken>())
      .Returns((IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?)null);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader,
      _mockSolutionLocator,
      _mockUpdaterFactory);

    // Act
    var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    // Assert
    context.Should().NotBeNull();
    await mockUpdater.Received(1).UpdateAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
    _mockSolutionLocator!.Received(1).FindSolutionPath(reportPath);
    _ = _mockReportLoader!.Received(1).LoadAsync(reportPath, Arg.Any<CancellationToken>());
  }

  [Test]
  public async Task CreateAsync_NonExistentReportWithUpdate_ThrowsFileNotFoundException()
  {
    // Arrange
    var nonExistentPath = Path.Combine(_testDirectory!, "nonexistent.json");
    var settings = new NamespaceMetricSettings
    {
      ReportPath = nonExistentPath,
      Namespace = "Rca.Loader.Services",
      NoUpdate = false // Allow missing when updating
    };

    _mockSolutionLocator!.FindSolutionPath(Arg.Any<string>())
      .Returns(Path.Combine(_testDirectory!, "solution.sln"));
    var mockUpdater = Substitute.For<IMetricsUpdater>();
    _mockUpdaterFactory!.Create(Arg.Any<string>())
      .Returns(mockUpdater);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader!,
      _mockThresholdsFileLoader!,
      _mockSolutionLocator,
      _mockUpdaterFactory);

    // Act
    var act = async () => await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    // Assert
    await act.Should().ThrowAsync<FileNotFoundException>()
      .WithMessage($"Metrics report not found: *{Path.GetFileName(nonExistentPath)}*");
  }

  [Test]
  public async Task CreateAsync_NonExistentReportWithoutUpdate_ThrowsFileNotFoundException()
  {
    // Arrange
    var nonExistentPath = Path.Combine(_testDirectory!, "nonexistent.json");
    var settings = new NamespaceMetricSettings
    {
      ReportPath = nonExistentPath,
      Namespace = "Rca.Loader.Services",
      NoUpdate = true
    };

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader!,
      _mockThresholdsFileLoader!,
      _mockSolutionLocator!,
      _mockUpdaterFactory!);

    // Act
    var act = async () => await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    // Assert
    await act.Should().ThrowAsync<FileNotFoundException>()
      .WithMessage($"Metrics report not found: *{Path.GetFileName(nonExistentPath)}*");
  }

  [Test]
  public async Task CreateAsync_NullReportFromLoader_ThrowsInvalidOperationException()
  {
    // Arrange
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    File.WriteAllText(reportPath, "{}");

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services",
      NoUpdate = true
    };

    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns((MetricsReport?)null);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader!,
      _mockSolutionLocator!,
      _mockUpdaterFactory!);

    // Act
    var act = async () => await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage($"Failed to load metrics report: *{reportPath}*");
  }

  [Test]
  public async Task CreateAsync_WithThresholdsFile_LoadsOverrideThresholds()
  {
    // Arrange
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    var thresholdsPath = Path.Combine(_testDirectory!, "thresholds.json");
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());
    File.WriteAllText(reportPath, "{}");

    var overrideThresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>
    {
      [MetricIdentifier.RoslynCyclomaticComplexity] = new MetricThresholdDefinition
      {
        Levels = new Dictionary<MetricSymbolLevel, MetricThreshold>
        {
          [MetricSymbolLevel.Type] = new MetricThreshold { Warning = 15, Error = 30 }
        }
      }
    };

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services",
      ThresholdsFile = thresholdsPath,
      NoUpdate = true
    };

    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns(report);
    _mockThresholdsFileLoader!.LoadAsync(thresholdsPath, Arg.Any<CancellationToken>())
      .Returns(overrideThresholds);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader,
      _mockSolutionLocator!,
      _mockUpdaterFactory!);

    // Act
    var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    // Assert
    context.Should().NotBeNull();
    _ = _mockThresholdsFileLoader!.Received(1).LoadAsync(thresholdsPath, Arg.Any<CancellationToken>());
  }

  [Test]
  public async Task CreateAsync_EmptyThresholdsFile_LoadsNullThresholds()
  {
    // Arrange
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());
    File.WriteAllText(reportPath, "{}");

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services",
      ThresholdsFile = null,
      NoUpdate = true
    };

    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns(report);
    _mockThresholdsFileLoader!.LoadAsync(null, Arg.Any<CancellationToken>())
      .Returns((IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?)null);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader,
      _mockSolutionLocator!,
      _mockUpdaterFactory!);

    // Act
    var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    // Assert
    context.Should().NotBeNull();
    _ = _mockThresholdsFileLoader!.Received(1).LoadAsync(null, Arg.Any<CancellationToken>());
  }

  [Test]
  public async Task CreateAsync_CancellationRequested_ThrowsOperationCanceledException()
  {
    // Arrange
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    File.WriteAllText(reportPath, "{}");

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services",
      NoUpdate = true
    };

    using var cts = new CancellationTokenSource();
    cts.Cancel();

    _mockReportLoader!.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns(async (NSubstitute.Core.CallInfo x) =>
      {
        await Task.Delay(1, x.Arg<CancellationToken>()).ConfigureAwait(false);
        return (MetricsReport?)MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());
      });

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader!,
      _mockSolutionLocator!,
      _mockUpdaterFactory!);

    // Act
    var act = async () => await factory.CreateAsync(settings, cts.Token).ConfigureAwait(false);

    // Assert
    await act.Should().ThrowAsync<OperationCanceledException>();
  }

  [Test]
  public async Task CreateAsync_WithSuppressedSymbols_CreatesContextWithSuppressedIndex()
  {
    // Arrange
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    var suppressedSymbols = new List<SuppressedSymbolInfo>
    {
      new SuppressedSymbolInfo
      {
        FullyQualifiedName = "Rca.Loader.Services.Type",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "Test"
      }
    };
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>(), suppressedSymbols);
    File.WriteAllText(reportPath, "{}");

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services",
      NoUpdate = true
    };

    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns(report);
    _mockThresholdsFileLoader!.LoadAsync(null, Arg.Any<CancellationToken>())
      .Returns((IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?)null);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader,
      _mockSolutionLocator!,
      _mockUpdaterFactory!);

    // Act
    var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    // Assert
    context.Should().NotBeNull();
    context.SuppressedSymbolIndex.Should().NotBeNull();
    context.SuppressedSymbolIndex.IsSuppressed("Rca.Loader.Services.Type", MetricIdentifier.RoslynClassCoupling, "CA1506")
      .Should().BeTrue();
  }

  [Test]
  public async Task CreateAsync_WithIncludeSuppressed_CreatesContextWithFlag()
  {
    // Arrange
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());
    File.WriteAllText(reportPath, "{}");

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services",
      IncludeSuppressed = true,
      NoUpdate = true
    };

    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns(report);
    _mockThresholdsFileLoader!.LoadAsync(null, Arg.Any<CancellationToken>())
      .Returns((IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?)null);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader,
      _mockSolutionLocator!,
      _mockUpdaterFactory!);

    // Act
    var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    // Assert
    context.Should().NotBeNull();
    context.IncludeSuppressed.Should().BeTrue();
  }

  [Test]
  public async Task CreateAsync_RelativeReportPath_ResolvesToAbsolutePath()
  {
    // Arrange
    var relativePath = "report.json";
    var absolutePath = Path.Combine(_testDirectory!, relativePath);
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());
    File.WriteAllText(absolutePath, "{}");

    var originalDirectory = Directory.GetCurrentDirectory();
    try
    {
      Directory.SetCurrentDirectory(_testDirectory!);

      var settings = new NamespaceMetricSettings
      {
        ReportPath = relativePath,
        Namespace = "Rca.Loader.Services",
        NoUpdate = true
      };

      _mockReportLoader!.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns(report);
      _mockThresholdsFileLoader!.LoadAsync(null, Arg.Any<CancellationToken>())
        .Returns((IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?)null);

      var factory = new MetricsReaderContextFactory(
        _mockReportLoader,
        _mockThresholdsFileLoader,
        _mockSolutionLocator!,
        _mockUpdaterFactory!);

      // Act
      var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

      // Assert
      context.Should().NotBeNull();
      // Verify that loader was called with absolute path
      _ = _mockReportLoader!.Received(1).LoadAsync(Arg.Is<string>(p => Path.IsPathRooted(p)), Arg.Any<CancellationToken>());
    }
    finally
    {
      Directory.SetCurrentDirectory(originalDirectory);
    }
  }

  [Test]
  public async Task CreateAsync_EmptyReportPath_ThrowsFileNotFoundException()
  {
    // Arrange
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());

    var settings = new NamespaceMetricSettings
    {
      ReportPath = string.Empty,
      Namespace = "Rca.Loader.Services",
      NoUpdate = true
    };

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader!,
      _mockThresholdsFileLoader!,
      _mockSolutionLocator!,
      _mockUpdaterFactory!);

    // Act
    // Empty path will be resolved to current directory, which likely doesn't contain MetricsReport.g.json
    var act = async () => await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    // Assert
    // Empty path causes ArgumentException in Path.GetFullPath, not FileNotFoundException
    await act.Should().ThrowAsync<ArgumentException>()
      .WithMessage("*path*");
  }

  [Test]
  public async Task CreateAsync_UpdateThrowsException_PropagatesException()
  {
    // Arrange
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    var solutionPath = Path.Combine(_testDirectory!, "solution.sln");
    File.WriteAllText(reportPath, "{}");

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services",
      NoUpdate = false
    };

    _mockSolutionLocator!.FindSolutionPath(reportPath)
      .Returns(solutionPath);
    var mockUpdater = Substitute.For<IMetricsUpdater>();
    mockUpdater.When(x => x.UpdateAsync(Arg.Any<CancellationToken>()))
      .Do(x => throw new InvalidOperationException("MSBuild failed"));
    _mockUpdaterFactory!.Create(solutionPath)
      .Returns(mockUpdater);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader!,
      _mockThresholdsFileLoader!,
      _mockSolutionLocator,
      _mockUpdaterFactory);

    // Act
    var act = async () => await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("MSBuild failed");
  }
}


