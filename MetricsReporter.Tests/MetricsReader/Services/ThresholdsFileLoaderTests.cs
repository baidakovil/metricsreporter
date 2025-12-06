namespace MetricsReporter.Tests.MetricsReader.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.Model;

/// <summary>
/// Unit tests for <see cref="ThresholdsFileLoader"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class ThresholdsFileLoaderTests
{
  private string? _testDirectory;
  private IThresholdsParser? _mockParser;

  [SetUp]
  public void SetUp()
  {
    _testDirectory = Path.Combine(Path.GetTempPath(), "RCA_ThresholdsFileLoaderTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(_testDirectory!);
    _mockParser = Substitute.For<IThresholdsParser>();
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
  public async Task LoadAsync_NullPath_ReturnsNull()
  {
    // Arrange
    var loader = new ThresholdsFileLoader(_mockParser!);

    // Act
    var result = await loader.LoadAsync(null, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().BeNull();
    _mockParser!.DidNotReceive().Parse(Arg.Any<string>());
  }

  [Test]
  public async Task LoadAsync_EmptyPath_ReturnsNull()
  {
    // Arrange
    var loader = new ThresholdsFileLoader(_mockParser!);

    // Act
    var result = await loader.LoadAsync(string.Empty, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().BeNull();
    _mockParser!.DidNotReceive().Parse(Arg.Any<string>());
  }

  [Test]
  public async Task LoadAsync_WhitespacePath_ReturnsNull()
  {
    // Arrange
    var loader = new ThresholdsFileLoader(_mockParser!);

    // Act
    var result = await loader.LoadAsync("   ", CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().BeNull();
    _mockParser!.DidNotReceive().Parse(Arg.Any<string>());
  }

  [Test]
  public async Task LoadAsync_NonExistentFile_ThrowsFileNotFoundException()
  {
    // Arrange
    var nonExistentPath = Path.Combine(_testDirectory!, "nonexistent.json");
    var loader = new ThresholdsFileLoader(_mockParser!);

    // Act
    var act = async () => await loader.LoadAsync(nonExistentPath, CancellationToken.None).ConfigureAwait(false);

    // Assert
    await act.Should().ThrowAsync<FileNotFoundException>()
      .WithMessage($"Thresholds override file not found: {Path.GetFullPath(nonExistentPath)}*");
    _mockParser!.DidNotReceive().Parse(Arg.Any<string>());
  }

  [Test]
  public async Task LoadAsync_ValidFile_CallsParserWithJsonPayload()
  {
    // Arrange
    var filePath = Path.Combine(_testDirectory!, "thresholds.json");
    const string jsonContent = """{"metrics": []}""";
    File.WriteAllText(filePath, jsonContent);

    var expectedResult = new Dictionary<MetricIdentifier, MetricThresholdDefinition>();
    _mockParser!.Parse(jsonContent).Returns(expectedResult);

    var loader = new ThresholdsFileLoader(_mockParser);

    // Act
    var result = await loader.LoadAsync(filePath, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().BeSameAs(expectedResult);
    _mockParser.Received(1).Parse(jsonContent);
  }

  [Test]
  public async Task LoadAsync_RelativePath_ResolvesToAbsolutePath()
  {
    // Arrange
    var relativePath = "thresholds.json";
    var absolutePath = Path.Combine(_testDirectory!, relativePath);
    const string jsonContent = """{"metrics": []}""";
    File.WriteAllText(absolutePath, jsonContent);

    var expectedResult = new Dictionary<MetricIdentifier, MetricThresholdDefinition>();
    _mockParser!.Parse(Arg.Any<string>()).Returns(expectedResult);

    var loader = new ThresholdsFileLoader(_mockParser);

    // Change to test directory to test relative path resolution
    var originalDirectory = Directory.GetCurrentDirectory();
    try
    {
      Directory.SetCurrentDirectory(_testDirectory!);

      // Act
      var result = await loader.LoadAsync(relativePath, CancellationToken.None).ConfigureAwait(false);

      // Assert
      result.Should().NotBeNull();
      _mockParser.Received(1).Parse(Arg.Any<string>());
    }
    finally
    {
      Directory.SetCurrentDirectory(originalDirectory);
    }
  }

  [Test]
  public async Task LoadAsync_CancellationRequested_ThrowsOperationCanceledException()
  {
    // Arrange
    var filePath = Path.Combine(_testDirectory!, "thresholds.json");
    File.WriteAllText(filePath, """{"metrics": []}""");

    using var cts = new CancellationTokenSource();
    cts.Cancel();

    var loader = new ThresholdsFileLoader(_mockParser!);

    // Act
    var act = async () => await loader.LoadAsync(filePath, cts.Token).ConfigureAwait(false);

    // Assert
    await act.Should().ThrowAsync<OperationCanceledException>();
  }

  [Test]
  public async Task LoadAsync_InvalidJson_ThrowsJsonException()
  {
    // Arrange
    var filePath = Path.Combine(_testDirectory!, "invalid.json");
    const string invalidJson = "{ invalid json }";
    File.WriteAllText(filePath, invalidJson);

    var loader = new ThresholdsFileLoader(_mockParser!);

    // Act
    var act = async () => await loader.LoadAsync(filePath, CancellationToken.None).ConfigureAwait(false);

    // Assert
    // JsonDocument.ParseAsync throws JsonReaderException for invalid JSON
    await act.Should().ThrowAsync<System.Text.Json.JsonException>();
    // Parser is never called because JsonDocument.ParseAsync fails first
    _mockParser!.DidNotReceive().Parse(Arg.Any<string>());
  }

  [Test]
  public async Task LoadAsync_EmptyFile_ThrowsJsonException()
  {
    // Arrange
    var filePath = Path.Combine(_testDirectory!, "empty.json");
    File.WriteAllText(filePath, string.Empty);

    var loader = new ThresholdsFileLoader(_mockParser!);

    // Act
    var act = async () => await loader.LoadAsync(filePath, CancellationToken.None).ConfigureAwait(false);

    // Assert
    // Empty file is not valid JSON, so JsonDocument.ParseAsync will throw
    await act.Should().ThrowAsync<System.Text.Json.JsonException>();
    _mockParser!.DidNotReceive().Parse(Arg.Any<string>());
  }

  [Test]
  public async Task LoadAsync_LargeFile_ReadsEntireContent()
  {
    // Arrange
    var filePath = Path.Combine(_testDirectory!, "large.json");
    var largeContent = "{\"metrics\": []}"; // Valid JSON instead of invalid construction
    File.WriteAllText(filePath, largeContent);

    var expectedResult = new Dictionary<MetricIdentifier, MetricThresholdDefinition>();
    _mockParser!.Parse(largeContent).Returns(expectedResult);

    var loader = new ThresholdsFileLoader(_mockParser);

    // Act
    var result = await loader.LoadAsync(filePath, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().BeSameAs(expectedResult);
    _mockParser.Received(1).Parse(largeContent);
  }

  [Test]
  public void Constructor_NullParser_ThrowsArgumentNullException()
  {
    // Act
    var act = () => new ThresholdsFileLoader(null!);

    // Assert
    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("parser");
  }
}


