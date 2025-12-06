namespace MetricsReporter.Tests.Services;

using System.IO;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter;

/// <summary>
/// Verifies that command-line arguments are correctly mapped to <see cref="MetricsReporter.Services.MetricsReporterOptions"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class MetricsReporterConsoleHostArgumentTests
{
  /// <summary>
  /// Ensures that the presence of <c>--replace-baseline</c> on the command line
  /// sets <see cref="MetricsReporter.Services.MetricsReporterOptions.ReplaceMetricsBaseline"/> to <see langword="true"/>.
  /// </summary>
  [Test]
  public void ParseArguments_WithReplaceBaselineFlag_SetsReplaceMetricsBaselineTrue()
  {
    // Arrange
    var args = new[]
    {
      "--metrics-dir", "c:\\temp\\metrics",
      "--output-json", "c:\\temp\\metrics\\report.json",
      "--baseline", "c:\\temp\\metrics\\baseline.json",
      "--replace-baseline"
    };

    // Act
    var options = MetricsReporterConsoleHost.ParseArguments(args);

    // Assert
    options.ReplaceMetricsBaseline.Should().BeTrue();
  }

  /// <summary>
  /// Ensures that when <c>--replace-baseline</c> is absent,
  /// <see cref="MetricsReporter.Services.MetricsReporterOptions.ReplaceMetricsBaseline"/> stays <see langword="false"/>.
  /// </summary>
  [Test]
  public void ParseArguments_WithoutReplaceBaselineFlag_SetsReplaceMetricsBaselineFalse()
  {
    // Arrange
    var args = new[]
    {
      "--metrics-dir", "c:\\temp\\metrics",
      "--output-json", "c:\\temp\\metrics\\report.json",
      "--baseline", "c:\\temp\\metrics\\baseline.json"
    };

    // Act
    var options = MetricsReporterConsoleHost.ParseArguments(args);

    // Assert
    options.ReplaceMetricsBaseline.Should().BeFalse();
  }

  [Test]
  public void ParseArguments_WithMultipleAltCoverArguments_PreservesAllPaths()
  {
    // Arrange
    var args = new[]
    {
      "--metrics-dir", "c:\\temp\\metrics",
      "--output-json", "c:\\temp\\metrics\\report.json",
      "--altcover", "coverage-one.xml",
      "--altcover", "coverage-two.xml"
    };

    // Act
    var options = MetricsReporterConsoleHost.ParseArguments(args);

    // Assert
    options.AltCoverPaths.Should().HaveCount(2);
    options.AltCoverPaths.Should().Contain(Path.GetFullPath("coverage-one.xml"));
    options.AltCoverPaths.Should().Contain(Path.GetFullPath("coverage-two.xml"));
  }
}



