namespace MetricsReporter.Tests.Processing;

using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Processing;
using MetricsReporter.Model;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Integration-style tests for <see cref="SuppressedSymbolsAnalyzer"/> that verify
/// discovery of <c>SuppressMessage</c> attributes in real C# source files.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class SuppressedSymbolsAnalyzerTests
{
  private string _rootDirectory = null!;

  [SetUp]
  public void SetUp()
  {
    _rootDirectory = Path.Combine(Path.GetTempPath(), "MetricsReporter_Suppressed_" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(_rootDirectory);
  }

  [TearDown]
  public void TearDown()
  {
    try
    {
      if (Directory.Exists(_rootDirectory))
      {
        Directory.Delete(_rootDirectory, recursive: true);
      }
    }
    catch
    {
      // Best effort cleanup; tests must not fail on IO issues during teardown.
    }
  }

  [Test]
  public void Analyze_ClassLevelSuppression_IsDiscovered()
  {
    // Arrange
    var srcDir = Path.Combine(_rootDirectory, "src", "Sample.Assembly");
    Directory.CreateDirectory(srcDir);

    var code = """
      using System.Diagnostics.CodeAnalysis;

      namespace Sample.Namespace;

      [SuppressMessage(
          "Microsoft.Maintainability",
          "CA1506:Avoid excessive class coupling",
          Justification = "Test justification.")]
      public class SampleType
      {
      }
      """;

    var filePath = Path.Combine(srcDir, "SampleType.cs");
    File.WriteAllText(filePath, code);

    var cancellationToken = CancellationToken.None;

    // Act
    var sourceCodeFolders = new[] { "src" };
    var report = SuppressedSymbolsAnalyzer.Analyze(_rootDirectory, sourceCodeFolders, excludedAssemblyNames: null, cancellationToken);

    // Assert
    report.SuppressedSymbols.Should().NotBeEmpty("class-level SuppressMessage should be discovered");
    var entry = report.SuppressedSymbols[0];
    entry.RuleId.Should().Be("CA1506");
    entry.Metric.Should().Be("RoslynClassCoupling");
    entry.FullyQualifiedName.Should().Be("Sample.Namespace.SampleType");
    entry.Justification.Should().Be("Test justification.");
  }

    [Test]
  public void Analyze_RealSolutionRoot_FindsPipeTestExecutionTransportSuppression()
  {
    // Arrange: simulate a realistic source tree with a suppressed member
    var srcDir = Path.Combine(_rootDirectory, "src", "Tools", "Sample.TestAdapter");
    Directory.CreateDirectory(srcDir);

    var code = """
      using System.Diagnostics.CodeAnalysis;

      namespace Sample.TestAdapter;

      public class PipeTestExecutionTransport
      {
        [SuppressMessage(
            "Microsoft.Maintainability",
            "CA1506:Avoid excessive class coupling",
            Justification = "Sample suppression for test coverage.")]
        public void Execute()
        {
        }
      }
      """;

    var filePath = Path.Combine(srcDir, "PipeTestExecutionTransport.cs");
    File.WriteAllText(filePath, code);

    var excludedAssemblyNames = "Tests,Contracts,MetricsReporter";
    var sourceCodeFolders = new[] { "src", "src/Tools", "tests" };

    // Act
    var report = SuppressedSymbolsAnalyzer.Analyze(_rootDirectory, sourceCodeFolders, excludedAssemblyNames, CancellationToken.None);

    // Assert
    report.SuppressedSymbols.Should().NotBeEmpty("realistic SuppressMessage usages should be discovered");
    report.SuppressedSymbols.Should().Contain(
      s =>
        s.RuleId == "CA1506" &&
        s.Metric == nameof(MetricIdentifier.RoslynClassCoupling) &&
        s.FullyQualifiedName == "Sample.TestAdapter.PipeTestExecutionTransport.Execute(...)",
      "CA1506 suppression should be mapped to RoslynClassCoupling at member level");
  }

  [Test]
  public void Analyze_FullyQualifiedAttributeName_IsDiscovered()
  {
    // Arrange: Test that System.Diagnostics.CodeAnalysis.SuppressMessage works
    var srcDir = Path.Combine(_rootDirectory, "src", "Sample.Assembly");
    Directory.CreateDirectory(srcDir);

    var code = """
      namespace Sample.Namespace;

      [System.Diagnostics.CodeAnalysis.SuppressMessage(
          "Microsoft.Maintainability",
          "CA1506:Avoid excessive class coupling",
          Justification = "Test with fully qualified attribute name.")]
      public class SampleType
      {
      }
      """;

    var filePath = Path.Combine(srcDir, "SampleType.cs");
    File.WriteAllText(filePath, code);

    // Act
    var sourceCodeFolders = new[] { "src" };
    var report = SuppressedSymbolsAnalyzer.Analyze(_rootDirectory, sourceCodeFolders, excludedAssemblyNames: null, CancellationToken.None);

    // Assert
    report.SuppressedSymbols.Should().NotBeEmpty("fully qualified SuppressMessage should be discovered");
    var entry = report.SuppressedSymbols[0];
    entry.RuleId.Should().Be("CA1506");
    entry.Metric.Should().Be("RoslynClassCoupling");
    entry.FullyQualifiedName.Should().Be("Sample.Namespace.SampleType");
    entry.Justification.Should().Be("Test with fully qualified attribute name.");
  }

  [Test]
  public void Analyze_ConcatenatedJustification_IsExtracted()
  {
    // Arrange: Test that string concatenation in justification works
    var srcDir = Path.Combine(_rootDirectory, "src", "Sample.Assembly");
    Directory.CreateDirectory(srcDir);

    var code = """
      using System.Diagnostics.CodeAnalysis;

      namespace Sample.Namespace;

      [SuppressMessage(
          "Microsoft.Maintainability",
          "CA1506:Avoid excessive class coupling",
          Justification = "First part of justification. " +
                          "Second part of justification. " +
                          "Third part of justification.")]
      public class SampleType
      {
      }
      """;

    var filePath = Path.Combine(srcDir, "SampleType.cs");
    File.WriteAllText(filePath, code);

    // Act
    var sourceCodeFolders = new[] { "src" };
    var report = SuppressedSymbolsAnalyzer.Analyze(_rootDirectory, sourceCodeFolders, excludedAssemblyNames: null, CancellationToken.None);

    // Assert
    report.SuppressedSymbols.Should().NotBeEmpty("suppression with concatenated justification should be discovered");
    var entry = report.SuppressedSymbols[0];
    entry.RuleId.Should().Be("CA1506");
    entry.Metric.Should().Be("RoslynClassCoupling");
    entry.FullyQualifiedName.Should().Be("Sample.Namespace.SampleType");
    entry.Justification.Should().Be("First part of justification. Second part of justification. Third part of justification.");
  }

  [Test]
  public void Analyze_FullyQualifiedWithConcatenatedJustification_Works()
  {
    // Arrange: Test both fully qualified name and concatenated justification together
    var srcDir = Path.Combine(_rootDirectory, "src", "Sample.Assembly");
    Directory.CreateDirectory(srcDir);

    var code = """
      namespace Sample.Namespace;

      [System.Diagnostics.CodeAnalysis.SuppressMessage(
          "Microsoft.Maintainability",
          "CA1506:Avoid excessive class coupling",
          Justification = "Part one. " + "Part two.")]
      public class SampleType
      {
      }
      """;

    var filePath = Path.Combine(srcDir, "SampleType.cs");
    File.WriteAllText(filePath, code);

    // Act
    var sourceCodeFolders = new[] { "src" };
    var report = SuppressedSymbolsAnalyzer.Analyze(_rootDirectory, sourceCodeFolders, excludedAssemblyNames: null, CancellationToken.None);

    // Assert
    report.SuppressedSymbols.Should().NotBeEmpty("fully qualified attribute with concatenated justification should work");
    var entry = report.SuppressedSymbols[0];
    entry.RuleId.Should().Be("CA1506");
    entry.Metric.Should().Be("RoslynClassCoupling");
    entry.FullyQualifiedName.Should().Be("Sample.Namespace.SampleType");
    entry.Justification.Should().Be("Part one. Part two.");
  }

  [Test]
  public void Analyze_PropertySuppression_IsDiscovered()
  {
    var srcDir = Path.Combine(_rootDirectory, "src", "Sample.Assembly");
    Directory.CreateDirectory(srcDir);

    var code = """
      using System.Diagnostics.CodeAnalysis;

      namespace Sample.Namespace;

      public class SampleType
      {
        [SuppressMessage(
            "Microsoft.Maintainability",
            "CA1506:Avoid excessive class coupling",
            Justification = "Property suppression test.")]
        public string SuppressedProperty { get; set; }
      }
      """;

    var filePath = Path.Combine(srcDir, "SampleType.cs");
    File.WriteAllText(filePath, code);

    var sourceCodeFolders = new[] { "src" };
    var report = SuppressedSymbolsAnalyzer.Analyze(_rootDirectory, sourceCodeFolders, excludedAssemblyNames: null, CancellationToken.None);

    report.SuppressedSymbols.Should().NotBeEmpty("property-level SuppressMessage should be discovered");
    var entry = report.SuppressedSymbols[0];
    entry.RuleId.Should().Be("CA1506");
    entry.Metric.Should().Be("RoslynClassCoupling");
    entry.FullyQualifiedName.Should().Be("Sample.Namespace.SampleType.SuppressedProperty");
    entry.Justification.Should().Be("Property suppression test.");
  }
}





