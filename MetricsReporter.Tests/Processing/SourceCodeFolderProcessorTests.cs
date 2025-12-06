namespace MetricsReporter.Tests.Processing;

using System.IO;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Processing;

/// <summary>
/// Unit tests for <see cref="SourceCodeFolderProcessor"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class SourceCodeFolderProcessorTests
{
  private string _tempDirectory = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDirectory = Path.Combine(Path.GetTempPath(), "RcaMetricsReporter_SourceCode_" + System.Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(_tempDirectory);
  }

  [TearDown]
  public void TearDown()
  {
    try
    {
      if (Directory.Exists(_tempDirectory))
      {
        Directory.Delete(_tempDirectory, recursive: true);
      }
    }
    catch
    {
      // Best effort cleanup
    }
  }

  [Test]
  public void NormalizeAndSortFolders_EmptyInput_ReturnsSingleEmptyString()
  {
    // Arrange
    var folders = Array.Empty<string>();

    // Act
    var result = SourceCodeFolderProcessor.NormalizeAndSortFolders(folders);

    // Assert
    result.Should().HaveCount(1);
    result[0].Should().BeEmpty();
  }

  [Test]
  public void NormalizeAndSortFolders_SingleFolder_ReturnsNormalized()
  {
    // Arrange
    var folders = new[] { "src" };

    // Act
    var result = SourceCodeFolderProcessor.NormalizeAndSortFolders(folders);

    // Assert
    result.Should().HaveCount(1);
    result[0].Should().Be("src");
  }

  [Test]
  public void NormalizeAndSortFolders_MultipleFolders_SortsByLengthDescending()
  {
    // Arrange
    var folders = new[] { "src", "src/Tools", "tests" };

    // Act
    var result = SourceCodeFolderProcessor.NormalizeAndSortFolders(folders);

    // Assert
    result.Should().HaveCount(3);
    // NormalizePath converts separators, so on Windows it will be "src\Tools"
    result[0].Length.Should().BeGreaterThan(result[1].Length);
    result[0].Length.Should().BeGreaterThan(result[2].Length);
    result.Should().Contain("src");
    result.Should().Contain("tests");
  }

  [Test]
  public void NormalizeAndSortFolders_WithWhitespace_FiltersEmpty()
  {
    // Arrange
    var folders = new[] { "src", "  ", "tests" };

    // Act
    var result = SourceCodeFolderProcessor.NormalizeAndSortFolders(folders);

    // Assert
    // Empty/whitespace-only strings are filtered by Where(!string.IsNullOrWhiteSpace) clause
    // NormalizePath only normalizes separators, it doesn't trim whitespace from paths
    result.Should().HaveCount(2);
    result.Should().Contain("tests");
    // Check that non-empty strings are present (whitespace-only "  " is filtered out)
    result.All(r => !string.IsNullOrWhiteSpace(r)).Should().BeTrue();
  }

  [Test]
  public void EnumerateCSharpFiles_SingleFolder_ReturnsAllCSharpFiles()
  {
    // Arrange
    var srcDir = Path.Combine(_tempDirectory, "src", "Sample.Assembly");
    Directory.CreateDirectory(srcDir);
    File.WriteAllText(Path.Combine(srcDir, "File1.cs"), "// Test");
    File.WriteAllText(Path.Combine(srcDir, "File2.cs"), "// Test");
    File.WriteAllText(Path.Combine(srcDir, "File.txt"), "// Test");

    var folders = new[] { "src" };

    // Act
    var result = SourceCodeFolderProcessor.EnumerateCSharpFiles(_tempDirectory, folders);

    // Assert
    result.Should().HaveCount(2);
    result.Should().OnlyContain(f => f.EndsWith(".cs"));
  }

  [Test]
  public void EnumerateCSharpFiles_MultipleFolders_ReturnsFilesFromAllFolders()
  {
    // Arrange
    var srcDir = Path.Combine(_tempDirectory, "src");
    var otherDir = Path.Combine(_tempDirectory, "other");
    Directory.CreateDirectory(srcDir);
    Directory.CreateDirectory(otherDir);
    File.WriteAllText(Path.Combine(srcDir, "File1.cs"), "// Test");
    File.WriteAllText(Path.Combine(otherDir, "File2.cs"), "// Test");

    var folders = new[] { "src", "other" };

    // Act
    var result = SourceCodeFolderProcessor.EnumerateCSharpFiles(_tempDirectory, folders);

    // Assert
    result.Should().HaveCount(2);
  }

  [Test]
  public void EnumerateCSharpFiles_NonExistentDirectory_ReturnsEmpty()
  {
    // Arrange
    var nonExistentDir = Path.Combine(Path.GetTempPath(), "NonExistent_" + System.Guid.NewGuid().ToString("N"));
    var folders = new[] { "src" };

    // Act
    var result = SourceCodeFolderProcessor.EnumerateCSharpFiles(nonExistentDir, folders);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void TryResolveAssemblyName_FileInSrcFolder_ReturnsAssemblyName()
  {
    // Arrange
    var filePath = Path.Combine(_tempDirectory, "src", "Sample.Assembly", "File.cs");
    var folders = new[] { "src" };

    // Act
    var result = SourceCodeFolderProcessor.TryResolveAssemblyName(_tempDirectory, filePath, folders);

    // Assert
    result.Should().Be("Sample.Assembly");
  }

  [Test]
  public void TryResolveAssemblyName_FileInNestedFolder_UsesLongestMatch()
  {
    // Arrange
    var filePath = Path.Combine(_tempDirectory, "src", "Tools", "Sample.Assembly", "File.cs");
    var folders = new[] { "src", "src/Tools" };

    // Act
    var result = SourceCodeFolderProcessor.TryResolveAssemblyName(_tempDirectory, filePath, folders);

    // Assert
    result.Should().Be("Sample.Assembly");
  }

  [Test]
  public void TryResolveAssemblyName_FileNotInSourceFolders_ReturnsFirstSegment()
  {
    // Arrange
    var filePath = Path.Combine(_tempDirectory, "other", "Sample.Assembly", "File.cs");
    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    File.WriteAllText(filePath, "// Test");
    var folders = new[] { "src" };

    // Act
    var result = SourceCodeFolderProcessor.TryResolveAssemblyName(_tempDirectory, filePath, folders);

    // Assert
    // When file is not in any source folder, it returns the first segment of the relative path
    result.Should().Be("other");
  }

  [Test]
  public void TryResolveAssemblyName_FileInRoot_ReturnsFileName()
  {
    // Arrange
    var filePath = Path.Combine(_tempDirectory, "File.cs");
    File.WriteAllText(filePath, "// Test");
    var folders = new[] { "src" };

    // Act
    var result = SourceCodeFolderProcessor.TryResolveAssemblyName(_tempDirectory, filePath, folders);

    // Assert
    // When file is at root, Path.GetRelativePath returns just the filename
    // After splitting by separators, we get one segment which is returned
    // This is the actual behavior - the method doesn't distinguish between
    // a file at root vs a file in a single-segment path
    result.Should().Be("File.cs");
  }

  [Test]
  public void TryResolveAssemblyName_EmptySourceFolders_ReturnsFirstSegment()
  {
    // Arrange
    var filePath = Path.Combine(_tempDirectory, "src", "Sample.Assembly", "File.cs");
    var folders = new[] { string.Empty };

    // Act
    var result = SourceCodeFolderProcessor.TryResolveAssemblyName(_tempDirectory, filePath, folders);

    // Assert
    result.Should().Be("src");
  }
}


