namespace MetricsReporter.Tests.Rendering;

using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="CoverageLinkBuilder"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class CoverageLinkBuilderTests
{
  private string _tempDirectory = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(_tempDirectory);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDirectory))
    {
      Directory.Delete(_tempDirectory, recursive: true);
    }
  }

  [Test]
  public void BuildLink_TypeNodeWithExistingFile_ReturnsFileUrl()
  {
    // Arrange
    var typeNode = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType"
    };
    var assemblyName = "Sample.Assembly";
    var htmlFileName = $"{assemblyName}_{typeNode.Name}.html";
    var htmlFilePath = Path.Combine(_tempDirectory, htmlFileName);
    File.WriteAllText(htmlFilePath, "<html></html>");

    var builder = new CoverageLinkBuilder(_tempDirectory);

    // Act
    var result = builder.BuildLink(typeNode, assemblyName);

    // Assert
    result.Should().NotBeNull();
    result.Should().Contain("file://");
    result.Should().Contain(htmlFileName);
  }

  [Test]
  public void BuildLink_TypeNodeWithNonExistingFile_ReturnsNull()
  {
    // Arrange
    var typeNode = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType"
    };
    var assemblyName = "Sample.Assembly";
    // File does not exist

    var builder = new CoverageLinkBuilder(_tempDirectory);

    // Act
    var result = builder.BuildLink(typeNode, assemblyName);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void BuildLink_MemberNode_ReturnsNull()
  {
    // Arrange
    var memberNode = new MemberMetricsNode
    {
      Name = "DoWork",
      FullyQualifiedName = "Sample.Namespace.SampleType.DoWork()"
    };
    var assemblyName = "Sample.Assembly";

    var builder = new CoverageLinkBuilder(_tempDirectory);

    // Act
    var result = builder.BuildLink(memberNode, assemblyName);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void BuildLink_AssemblyNode_ReturnsNull()
  {
    // Arrange
    var assemblyNode = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly"
    };

    var builder = new CoverageLinkBuilder(_tempDirectory);

    // Act
    var result = builder.BuildLink(assemblyNode, assemblyNode.Name);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void BuildLink_NamespaceNode_ReturnsNull()
  {
    // Arrange
    var namespaceNode = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace"
    };
    var assemblyName = "Sample.Assembly";

    var builder = new CoverageLinkBuilder(_tempDirectory);

    // Act
    var result = builder.BuildLink(namespaceNode, assemblyName);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void BuildLink_NullCoverageHtmlDir_ReturnsNull()
  {
    // Arrange
    var typeNode = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType"
    };
    var assemblyName = "Sample.Assembly";

    var builder = new CoverageLinkBuilder(null);

    // Act
    var result = builder.BuildLink(typeNode, assemblyName);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void BuildLink_EmptyCoverageHtmlDir_ReturnsNull()
  {
    // Arrange
    var typeNode = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType"
    };
    var assemblyName = "Sample.Assembly";

    var builder = new CoverageLinkBuilder(string.Empty);

    // Act
    var result = builder.BuildLink(typeNode, assemblyName);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void BuildLink_NonExistentDirectory_ReturnsNull()
  {
    // Arrange
    var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var typeNode = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType"
    };
    var assemblyName = "Sample.Assembly";

    var builder = new CoverageLinkBuilder(nonExistentDir);

    // Act
    var result = builder.BuildLink(typeNode, assemblyName);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void BuildLink_NullAssemblyName_ReturnsNull()
  {
    // Arrange
    var typeNode = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType"
    };

    var builder = new CoverageLinkBuilder(_tempDirectory);

    // Act
    var result = builder.BuildLink(typeNode, null);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void BuildLink_EmptyAssemblyName_ReturnsNull()
  {
    // Arrange
    var typeNode = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType"
    };

    var builder = new CoverageLinkBuilder(_tempDirectory);

    // Act
    var result = builder.BuildLink(typeNode, string.Empty);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void BuildLink_EmptyTypeName_ReturnsNull()
  {
    // Arrange
    var typeNode = new TypeMetricsNode
    {
      Name = string.Empty,
      FullyQualifiedName = "Sample.Namespace"
    };
    var assemblyName = "Sample.Assembly";

    var builder = new CoverageLinkBuilder(_tempDirectory);

    // Act
    var result = builder.BuildLink(typeNode, assemblyName);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void BuildLink_UrlIsHtmlEncoded()
  {
    // Arrange
    var typeNode = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType"
    };
    var assemblyName = "Sample.Assembly";
    var htmlFileName = $"{assemblyName}_{typeNode.Name}.html";
    var htmlFilePath = Path.Combine(_tempDirectory, htmlFileName);
    File.WriteAllText(htmlFilePath, "<html></html>");

    var builder = new CoverageLinkBuilder(_tempDirectory);

    // Act
    var result = builder.BuildLink(typeNode, assemblyName);

    // Assert
    result.Should().NotBeNull();
    // File:// URLs should be properly encoded
    result.Should().NotContain(" ");
  }

  [Test]
  public void BuildLink_WithSpecialCharactersInNames_BuildsCorrectFilename()
  {
    // Arrange
    var typeNode = new TypeMetricsNode
    {
      Name = "GenericType`1",
      FullyQualifiedName = "Sample.Namespace.GenericType`1"
    };
    var assemblyName = "Sample.Assembly";
    var htmlFileName = $"{assemblyName}_{typeNode.Name}.html";
    var htmlFilePath = Path.Combine(_tempDirectory, htmlFileName);
    File.WriteAllText(htmlFilePath, "<html></html>");

    var builder = new CoverageLinkBuilder(_tempDirectory);

    // Act
    var result = builder.BuildLink(typeNode, assemblyName);

    // Assert
    result.Should().NotBeNull();
    // File:// URLs are HTML encoded, so we check for the encoded version
    result.Should().Contain(htmlFileName.Replace("`", "%60"));
  }

  [Test]
  public void BuildLink_VerifyCorrectNamingConvention()
  {
    // Arrange
    var typeNode = new TypeMetricsNode
    {
      Name = "PipeResponseFactory",
      FullyQualifiedName = "Rca.Loader.Infrastructure.PipeResponseFactory"
    };
    var assemblyName = "Rca.Loader";
    var expectedFileName = "Rca.Loader_PipeResponseFactory.html";
    var htmlFilePath = Path.Combine(_tempDirectory, expectedFileName);
    File.WriteAllText(htmlFilePath, "<html></html>");

    var builder = new CoverageLinkBuilder(_tempDirectory);

    // Act
    var result = builder.BuildLink(typeNode, assemblyName);

    // Assert
    result.Should().NotBeNull();
    result.Should().Contain(expectedFileName);
  }
}


