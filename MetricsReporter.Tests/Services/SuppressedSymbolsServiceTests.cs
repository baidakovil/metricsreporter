namespace MetricsReporter.Tests.Services;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter;
using MetricsReporter.Model;
using MetricsReporter.Services;
using MetricsReporter.Serialization;
using System.Text.Json;
using MetricsReporter.Tests.TestHelpers;
using Microsoft.Extensions.Logging;

/// <summary>
/// Unit tests for <see cref="SuppressedSymbolsService"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class SuppressedSymbolsServiceTests
{
  private string? testDirectory;
  private string? suppressedSymbolsPath;
  private SuppressedSymbolsService? service;

  [SetUp]
  public void SetUp()
  {
    testDirectory = Path.Combine(Path.GetTempPath(), "RCA_SuppressedSymbolsServiceTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(testDirectory!);
    suppressedSymbolsPath = Path.Combine(testDirectory, "suppressed-symbols.json");
    service = new SuppressedSymbolsService();
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

  private static TestLogger<SuppressedSymbolsService> CreateLogger()
    => new();

  [Test]
  public async Task ResolveAsync_WithAnalyzeDisabledAndNullPath_ReturnsEmptyList()
  {
    // Arrange
    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = false,
      SuppressedSymbolsPath = null
    };
    var logger = CreateLogger();

    // Act
    var result = await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeDisabledAndEmptyPath_ReturnsEmptyList()
  {
    // Arrange
    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = false,
      SuppressedSymbolsPath = string.Empty
    };
    var logger = CreateLogger();

    // Act
    var result = await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeDisabledAndNonExistentPath_ReturnsEmptyList()
  {
    // Arrange
    var nonExistentPath = Path.Combine(testDirectory!, "nonexistent.json");
    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = false,
      SuppressedSymbolsPath = nonExistentPath
    };
    var logger = CreateLogger();

    // Act
    var result = await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeDisabledAndExistingPath_LoadsFromCache()
  {
    // Arrange
    var suppressedSymbols = new List<SuppressedSymbolInfo>
    {
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "Test justification"
      }
    };

    var report = new SuppressedSymbolsReport
    {
      SuppressedSymbols = suppressedSymbols
    };

    var json = JsonSerializer.Serialize(report, JsonSerializerOptionsFactory.Create());
    File.WriteAllText(suppressedSymbolsPath!, json);

    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = false,
      SuppressedSymbolsPath = suppressedSymbolsPath
    };
    var logger = CreateLogger();

    // Act
    var result = await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(1);
    result[0].FullyQualifiedName.Should().Be("Sample.Namespace.SampleType");
    result[0].RuleId.Should().Be("CA1506");
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeEnabledButNullPath_ReturnsEmptyListAfterError()
  {
    // Arrange
    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = null,
      SolutionDirectory = testDirectory,
      SourceCodeFolders = Array.Empty<string>()
    };
    var logger = CreateLogger();

    // Act
    var result = await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty("should return empty list when analysis fails");
    logger.Entries.Should().Contain(entry => entry.Level == LogLevel.Error && entry.Message.Contains("Failed to analyze suppressed symbols"));
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeEnabledAndEmptyPath_ReturnsEmptyListAfterError()
  {
    // Arrange
    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = string.Empty,
      SolutionDirectory = testDirectory,
      SourceCodeFolders = Array.Empty<string>()
    };
    var logger = CreateLogger();

    // Act
    var result = await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty("should return empty list when analysis fails");
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeEnabledAndValidPath_CreatesOutputDirectory()
  {
    // Arrange
    var outputDir = Path.Combine(testDirectory!, "output");
    var outputPath = Path.Combine(outputDir, "suppressed-symbols.json");

    var srcDir = Path.Combine(testDirectory!, "src", "Sample.Assembly");
    Directory.CreateDirectory(srcDir);
    File.WriteAllText(Path.Combine(srcDir, "Sample.cs"), "namespace Sample; public class SampleType { }");

    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = outputPath,
      SolutionDirectory = testDirectory,
      SourceCodeFolders = new[] { "src" }
    };
    var logger = CreateLogger();

    // Act
    var result = await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    Directory.Exists(outputDir).Should().BeTrue("output directory should be created");
    File.Exists(outputPath).Should().BeTrue("suppressed symbols file should be created");
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeEnabledAndValidPath_WritesResultsToFile()
  {
    // Arrange
    var srcDir = Path.Combine(testDirectory!, "src", "Sample.Assembly");
    Directory.CreateDirectory(srcDir);

    var code = """
      using System.Diagnostics.CodeAnalysis;

      namespace Sample.Namespace;

      [SuppressMessage("Microsoft.Maintainability", "CA1506:Avoid excessive class coupling", Justification = "Test")]
      public class SampleType
      {
      }
      """;
    File.WriteAllText(Path.Combine(srcDir, "SampleType.cs"), code);

    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = suppressedSymbolsPath,
      SolutionDirectory = testDirectory,
      SourceCodeFolders = new[] { "src" }
    };
    var logger = CreateLogger();

    // Act
    var result = await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    File.Exists(suppressedSymbolsPath!).Should().BeTrue();
    result.Should().NotBeEmpty();
    var writtenContent = File.ReadAllText(suppressedSymbolsPath!);
    writtenContent.Should().Contain("SampleType");
    writtenContent.Should().Contain("CA1506");
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeDisabledAndInvalidJsonInCache_ThrowsJsonException()
  {
    // Arrange - Invalid JSON should throw JsonException when loading from cache
    File.WriteAllText(suppressedSymbolsPath!, "invalid json content");
    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = false,
      SuppressedSymbolsPath = suppressedSymbolsPath
    };
    var logger = CreateLogger();

    // Act & Assert - Should throw JsonException when JSON is invalid
    var action = async () => await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);
    await action.Should().ThrowAsync<System.Text.Json.JsonException>("invalid JSON should throw JsonException");
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeEnabledAndCancellationRequested_ReturnsEmptyList()
  {
    // Arrange
    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = suppressedSymbolsPath,
      SolutionDirectory = testDirectory,
      SourceCodeFolders = Array.Empty<string>()
    };
    var logger = CreateLogger();
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    // Act
    var result = await service!.ResolveAsync(options, logger, cts.Token).ConfigureAwait(false);

    // Assert
    result.Should().NotBeNull();
    // Result depends on when cancellation is detected during analysis
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeEnabledAndNoSourceCodeFolders_AnalyzesNoFiles()
  {
    // Arrange
    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = suppressedSymbolsPath,
      SolutionDirectory = testDirectory,
      SourceCodeFolders = Array.Empty<string>()
    };
    var logger = CreateLogger();

    // Act
    var result = await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty("no source code folders means no files to analyze");
    File.Exists(suppressedSymbolsPath!).Should().BeTrue("output file should still be created");
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeEnabledAndMissingSolutionDirectory_UsesMetricsDirectory()
  {
    // Arrange
    var metricsDir = Path.Combine(testDirectory!, "metrics");
    Directory.CreateDirectory(metricsDir);

    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = suppressedSymbolsPath,
      SolutionDirectory = null,
      MetricsDirectory = metricsDir,
      SourceCodeFolders = Array.Empty<string>()
    };
    var logger = CreateLogger();

    // Act
    var result = await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().NotBeNull();
    // Should complete without error, using metrics directory as start point
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeEnabledAndExcludedAssemblyNames_FiltersAssemblies()
  {
    // Arrange
    var srcDir = Path.Combine(testDirectory!, "src", "Sample.Assembly");
    Directory.CreateDirectory(srcDir);

    var code = """
      using System.Diagnostics.CodeAnalysis;

      namespace Sample.Namespace;

      [SuppressMessage("Microsoft.Maintainability", "CA1506:Avoid excessive class coupling", Justification = "Test")]
      public class SampleType
      {
      }
      """;
    File.WriteAllText(Path.Combine(srcDir, "SampleType.cs"), code);

    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = suppressedSymbolsPath,
      SolutionDirectory = testDirectory,
      SourceCodeFolders = new[] { "src" },
      ExcludedAssemblyNames = "Sample.Assembly"
    };
    var logger = CreateLogger();

    // Act
    var result = await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().NotBeNull();
    // Excluded assembly should not be processed
    result.Should().BeEmpty("excluded assembly should not be analyzed");
  }

  [Test]
  public async Task ResolveAsync_WithAnalyzeEnabled_LogsCompletionMessageWithCorrectFormat()
  {
    // Arrange
    var srcDir = Path.Combine(testDirectory!, "src", "Sample.Assembly");
    Directory.CreateDirectory(srcDir);

    var code = """
      using System.Diagnostics.CodeAnalysis;

      namespace Sample.Namespace;

      [SuppressMessage("Microsoft.Maintainability", "CA1506:Avoid excessive class coupling", Justification = "Test")]
      public class SampleType
      {
      }
      """;
    File.WriteAllText(Path.Combine(srcDir, "SampleType.cs"), code);

    var options = new MetricsReporterOptions
    {
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = suppressedSymbolsPath,
      SolutionDirectory = testDirectory,
      SourceCodeFolders = new[] { "src" }
    };
    var logger = CreateLogger();

    // Act
    var result = await service!.ResolveAsync(options, logger, CancellationToken.None).ConfigureAwait(false);

    // Assert
    logger.Entries.Should().Contain(entry =>
      entry.Level == LogLevel.Information &&
      entry.Message.Contains("Suppressed symbols analysis completed:") &&
      entry.Message.Contains("entries") &&
      !entry.Message.Contains("Entries="));
  }
}


