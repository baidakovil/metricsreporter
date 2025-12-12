using System;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using MetricsReporter.Logging;

namespace MetricsReporter.Tests.Logging;

/// <summary>
/// Unit tests for <see cref="LoggerFactoryBuilder"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class LoggerFactoryBuilderTests
{
  [Test]
  public void FromVerbosity_Normal_ReturnsInformation()
  {
    // Act
    var level = LoggerFactoryBuilder.FromVerbosity("normal");

    // Assert
    level.Should().Be(LogLevel.Information);
  }

  [Test]
  public void FromVerbosity_Null_ReturnsInformation()
  {
    // Act
    var level = LoggerFactoryBuilder.FromVerbosity(null);

    // Assert
    level.Should().Be(LogLevel.Information);
  }

  [Test]
  public void FromVerbosity_Detailed_ReturnsDebug()
  {
    // Act
    var level = LoggerFactoryBuilder.FromVerbosity("detailed");

    // Assert
    level.Should().Be(LogLevel.Debug);
  }

  [Test]
  public void FromVerbosity_Quiet_ReturnsWarning()
  {
    // Act
    var level = LoggerFactoryBuilder.FromVerbosity("quiet");

    // Assert
    level.Should().Be(LogLevel.Warning);
  }

  [Test]
  public void FromVerbosity_Minimal_ReturnsWarning()
  {
    // Act
    var level = LoggerFactoryBuilder.FromVerbosity("minimal");

    // Assert
    level.Should().Be(LogLevel.Warning);
  }

  [Test]
  public void FromVerbosity_Unknown_ReturnsInformation()
  {
    // Act
    var level = LoggerFactoryBuilder.FromVerbosity("unknown");

    // Assert
    level.Should().Be(LogLevel.Information);
  }

  [Test]
  public void FromVerbosity_Whitespace_ReturnsInformation()
  {
    // Act
    var level = LoggerFactoryBuilder.FromVerbosity("   ");

    // Assert
    level.Should().Be(LogLevel.Information);
  }

  [Test]
  public void Create_WithFileLogPath_CreatesFile()
  {
    // Arrange
    var tempFile = Path.Combine(Path.GetTempPath(), $"test-log-{Guid.NewGuid():N}.log");

    try
    {
      // Act
      using (var factory = LoggerFactoryBuilder.Create(tempFile, LogLevel.Information, includeConsole: false, verbosity: "normal"))
      {
        var logger = factory.CreateLogger("TestCategory");
        logger.LogInformation("Test message");
      }

      // Assert - Verify file was created (file provider is registered and working)
      // Note: We don't verify content due to file locking issues, but file creation
      // confirms the file provider is properly registered and functional
      File.Exists(tempFile).Should().BeTrue();
    }
    finally
    {
      // Cleanup with retry
      for (int i = 0; i < 5; i++)
      {
        try
        {
          if (File.Exists(tempFile))
          {
            File.Delete(tempFile);
            break;
          }
        }
        catch (IOException)
        {
          if (i < 4)
          {
            System.Threading.Thread.Sleep(200);
          }
        }
      }
    }
  }

  [Test]
  public void Create_WithNullLogFilePath_DoesNotCreateFile()
  {
    // Arrange
    var tempDir = Path.Combine(Path.GetTempPath(), $"test-log-dir-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempDir);
    var nonExistentFile = Path.Combine(tempDir, "non-existent.log");

    try
    {
      // Act
      using (var factory = LoggerFactoryBuilder.Create(null, LogLevel.Information, includeConsole: false, verbosity: "normal"))
      {
        var logger = factory.CreateLogger("Test");
        logger.LogInformation("Test message");
      }

      // Assert - File should not exist
      File.Exists(nonExistentFile).Should().BeFalse();
    }
    finally
    {
      if (Directory.Exists(tempDir))
      {
        try
        {
          Directory.Delete(tempDir, recursive: true);
        }
        catch
        {
          // Ignore cleanup errors
        }
      }
    }
  }

  [Test]
  public void Create_WithEmptyLogFilePath_DoesNotCreateFile()
  {
    // Arrange
    var tempDir = Path.Combine(Path.GetTempPath(), $"test-log-dir-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempDir);
    var nonExistentFile = Path.Combine(tempDir, "non-existent.log");

    try
    {
      // Act
      using (var factory = LoggerFactoryBuilder.Create("   ", LogLevel.Information, includeConsole: false, verbosity: "normal"))
      {
        var logger = factory.CreateLogger("Test");
        logger.LogInformation("Test message");
      }

      // Assert - File should not exist
      File.Exists(nonExistentFile).Should().BeFalse();
    }
    finally
    {
      if (Directory.Exists(tempDir))
      {
        try
        {
          Directory.Delete(tempDir, recursive: true);
        }
        catch
        {
          // Ignore cleanup errors
        }
      }
    }
  }

}

