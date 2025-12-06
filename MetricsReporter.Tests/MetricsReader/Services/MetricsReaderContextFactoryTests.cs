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

  [SetUp]
  public void SetUp()
  {
    _testDirectory = Path.Combine(Path.GetTempPath(), "RCA_MetricsReaderContextFactoryTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(_testDirectory!);
    _mockReportLoader = Substitute.For<IJsonReportLoader>();
    _mockThresholdsFileLoader = Substitute.For<IThresholdsFileLoader>();
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
    var act = () => new MetricsReaderContextFactory(
      null!,
      _mockThresholdsFileLoader!);

    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("reportLoader");
  }

  [Test]
  public void Constructor_NullThresholdsFileLoader_ThrowsArgumentNullException()
  {
    var act = () => new MetricsReaderContextFactory(
      _mockReportLoader!,
      null!);

    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("thresholdsFileLoader");
  }

  [Test]
  public async Task CreateAsync_NullSettings_ThrowsArgumentNullException()
  {
    var factory = new MetricsReaderContextFactory(
      _mockReportLoader!,
      _mockThresholdsFileLoader!);

    var act = async () => await factory.CreateAsync(null!, CancellationToken.None).ConfigureAwait(false);

    await act.Should().ThrowAsync<ArgumentNullException>()
      .WithParameterName("settings");
  }

  [Test]
  public async Task CreateAsync_LoadsReportAndThresholds()
  {
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());
    File.WriteAllText(reportPath, "{}");

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services"
    };

    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns(report);
    _mockThresholdsFileLoader!.LoadAsync(null, Arg.Any<CancellationToken>())
      .Returns((IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?)null);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader);

    var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    context.Should().NotBeNull();
    context.Report.Should().BeSameAs(report);
    _ = _mockReportLoader.Received(1).LoadAsync(reportPath, Arg.Any<CancellationToken>());
  }

  [Test]
  public async Task CreateAsync_NonExistentReport_ThrowsFileNotFoundException()
  {
    var nonExistentPath = Path.Combine(_testDirectory!, "nonexistent.json");
    var settings = new NamespaceMetricSettings
    {
      ReportPath = nonExistentPath,
      Namespace = "Rca.Loader.Services"
    };

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader!,
      _mockThresholdsFileLoader!);

    var act = async () => await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    await act.Should().ThrowAsync<FileNotFoundException>()
      .WithMessage($"Metrics report not found: *{Path.GetFileName(nonExistentPath)}*");
  }

  [Test]
  public async Task CreateAsync_NullReportFromLoader_ThrowsInvalidOperationException()
  {
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    File.WriteAllText(reportPath, "{}");

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services"
    };

    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns((MetricsReport?)null);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader!);

    var act = async () => await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage($"Failed to load metrics report: *{reportPath}*");
  }

  [Test]
  public async Task CreateAsync_WithThresholdsFile_LoadsOverrideThresholds()
  {
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
      ThresholdsFile = thresholdsPath
    };

    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns(report);
    _mockThresholdsFileLoader!.LoadAsync(thresholdsPath, Arg.Any<CancellationToken>())
      .Returns(overrideThresholds);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader);

    var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    context.Should().NotBeNull();
    _ = _mockThresholdsFileLoader.Received(1).LoadAsync(thresholdsPath, Arg.Any<CancellationToken>());
  }

  [Test]
  public async Task CreateAsync_EmptyThresholdsFile_LoadsNullThresholds()
  {
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());
    File.WriteAllText(reportPath, "{}");

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services",
      ThresholdsFile = null
    };

    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns(report);
    _mockThresholdsFileLoader!.LoadAsync(null, Arg.Any<CancellationToken>())
      .Returns((IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?)null);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader);

    var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    context.Should().NotBeNull();
    _ = _mockThresholdsFileLoader.Received(1).LoadAsync(null, Arg.Any<CancellationToken>());
  }

  [Test]
  public async Task CreateAsync_CancellationRequested_ThrowsOperationCanceledException()
  {
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    File.WriteAllText(reportPath, "{}");

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services"
    };

    using var cts = new CancellationTokenSource();
    cts.Cancel();

    _mockReportLoader!.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns(call =>
      {
        var token = call.Arg<CancellationToken>();
        token.ThrowIfCancellationRequested();
        return Task.FromResult<MetricsReport?>(MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>()));
      });

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader!);

    var act = async () => await factory.CreateAsync(settings, cts.Token).ConfigureAwait(false);

    await act.Should().ThrowAsync<OperationCanceledException>();
  }

  [Test]
  public async Task CreateAsync_WithSuppressedSymbols_CreatesContextWithSuppressedIndex()
  {
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
      Namespace = "Rca.Loader.Services"
    };

    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns(report);
    _mockThresholdsFileLoader!.LoadAsync(null, Arg.Any<CancellationToken>())
      .Returns((IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?)null);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader);

    var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    context.Should().NotBeNull();
    context.SuppressedSymbolIndex.Should().NotBeNull();
    context.SuppressedSymbolIndex.IsSuppressed("Rca.Loader.Services.Type", MetricIdentifier.RoslynClassCoupling, "CA1506")
      .Should().BeTrue();
  }

  [Test]
  public async Task CreateAsync_WithIncludeSuppressed_CreatesContextWithFlag()
  {
    var reportPath = Path.Combine(_testDirectory!, "report.json");
    var report = MetricsReaderCommandTestData.CreateReport(Enumerable.Empty<TypeMetricsNode>());
    File.WriteAllText(reportPath, "{}");

    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = "Rca.Loader.Services",
      IncludeSuppressed = true
    };

    _mockReportLoader!.LoadAsync(reportPath, Arg.Any<CancellationToken>())
      .Returns(report);
    _mockThresholdsFileLoader!.LoadAsync(null, Arg.Any<CancellationToken>())
      .Returns((IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?)null);

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader,
      _mockThresholdsFileLoader);

    var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    context.Should().NotBeNull();
    context.IncludeSuppressed.Should().BeTrue();
  }

  [Test]
  public async Task CreateAsync_RelativeReportPath_ResolvesToAbsolutePath()
  {
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
        Namespace = "Rca.Loader.Services"
      };

      _mockReportLoader!.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns(report);
      _mockThresholdsFileLoader!.LoadAsync(null, Arg.Any<CancellationToken>())
        .Returns((IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?)null);

      var factory = new MetricsReaderContextFactory(
        _mockReportLoader,
        _mockThresholdsFileLoader);

      var context = await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

      context.Should().NotBeNull();
      _ = _mockReportLoader!.Received(1).LoadAsync(Arg.Is<string>(p => Path.IsPathRooted(p)), Arg.Any<CancellationToken>());
    }
    finally
    {
      Directory.SetCurrentDirectory(originalDirectory);
    }
  }

  [Test]
  public async Task CreateAsync_EmptyReportPath_ThrowsArgumentException()
  {
    var settings = new NamespaceMetricSettings
    {
      ReportPath = string.Empty,
      Namespace = "Rca.Loader.Services"
    };

    var factory = new MetricsReaderContextFactory(
      _mockReportLoader!,
      _mockThresholdsFileLoader!);

    var act = async () => await factory.CreateAsync(settings, CancellationToken.None).ConfigureAwait(false);

    await act.Should().ThrowAsync<ArgumentException>()
      .WithMessage("*Report path must be provided*");
  }
}

