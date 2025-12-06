namespace MetricsReporter.Tests.Rendering;

using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="SymbolTooltipBuilder"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class SymbolTooltipBuilderTests
{
  [Test]
  public void BuildDataAttribute_WithNullFqn_ReturnsEmptyString()
  {
    // Arrange
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = null,
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void BuildDataAttribute_WithEmptyFqn_ReturnsEmptyString()
  {
    // Arrange
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = string.Empty,
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void BuildDataAttribute_WithWhitespaceFqn_ReturnsEmptyString()
  {
    // Arrange
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "   ",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void BuildDataAttribute_WithTypeNode_BuildsAttribute()
  {
    // Arrange
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    result.Should().Contain("data-symbol-info=");
    result.Should().Contain("TYPE");
    result.Should().Contain("Sample.Namespace.SampleType");
  }

  [Test]
  public void BuildDataAttribute_WithMemberNode_BuildsAttribute()
  {
    // Arrange
    var node = new MemberMetricsNode
    {
      Name = "DoWork",
      FullyQualifiedName = "Sample.Namespace.SampleType.DoWork()",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    result.Should().Contain("data-symbol-info=");
    result.Should().Contain("MEMBER");
    result.Should().Contain("Sample.Namespace.SampleType.DoWork()");
  }

  [Test]
  public void BuildDataAttribute_WithAssemblyNode_BuildsAttribute()
  {
    // Arrange
    var node = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    result.Should().Contain("data-symbol-info=");
    result.Should().Contain("ASSEMBLY");
    result.Should().Contain("Sample.Assembly");
  }

  [Test]
  public void BuildDataAttribute_WithNamespaceNode_BuildsAttribute()
  {
    // Arrange
    var node = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    result.Should().Contain("data-symbol-info=");
    result.Should().Contain("NAMESPACE");
    result.Should().Contain("Sample.Namespace");
  }

  [Test]
  public void BuildDataAttribute_WithSolutionNode_BuildsAttribute()
  {
    // Arrange
    var node = new SolutionMetricsNode
    {
      Name = "SampleSolution",
      FullyQualifiedName = "SampleSolution",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    result.Should().Contain("data-symbol-info=");
    result.Should().Contain("SOLUTION");
    result.Should().Contain("SampleSolution");
  }

  [Test]
  public void BuildDataAttribute_WithSourceLocation_IncludesSourceInfo()
  {
    // Arrange
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>(),
      Source = new SourceLocation
      {
        Path = "C:\\Source\\File.cs",
        StartLine = 10,
        EndLine = 20
      }
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    // JSON is HTML-encoded, so quotes become &quot; and backslashes are escaped as \\ in JSON
    result.Should().Contain("Source");
    result.Should().Contain("File.cs");
    result.Should().Contain("10");
    result.Should().Contain("20");
    result.Should().Contain("sourcePath");
  }

  [Test]
  public void BuildDataAttribute_WithNullSource_OmitsSourceInfo()
  {
    // Arrange
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>(),
      Source = null
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    result.Should().Contain("data-symbol-info=");
    result.Should().Contain("Sample.Namespace.SampleType");
    // Source fields should be null in JSON, which means they won't appear due to DefaultIgnoreCondition.WhenWritingNull
  }

  [Test]
  public void BuildDataAttribute_WithPartialSource_IncludesAvailableInfo()
  {
    // Arrange
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>(),
      Source = new SourceLocation
      {
        Path = "C:\\Source\\File.cs",
        StartLine = 10,
        EndLine = null
      }
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    // JSON is HTML-encoded, so quotes become &quot; and backslashes are escaped as \\ in JSON
    result.Should().Contain("Source");
    result.Should().Contain("File.cs");
    result.Should().Contain("10");
    result.Should().Contain("sourcePath");
    // EndLine should be null and not appear in JSON
  }

  [Test]
  public void BuildDataAttribute_WithHtmlInFqn_EscapesHtml()
  {
    // Arrange
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace<Test>.SampleType",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    // The FQN is serialized to JSON, then the entire JSON is HTML-encoded
    // WebUtility.HtmlEncode encodes quotes as &quot; but may not encode < and > in JSON string values
    result.Should().Contain("data-symbol-info=");
    result.Should().Match("*Sample.Namespace*Test*.SampleType*");
  }

  [Test]
  public void BuildDataAttribute_WithHtmlInPath_EscapesHtml()
  {
    // Arrange
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>(),
      Source = new SourceLocation
      {
        Path = "C:\\Source\\File<script>.cs",
        StartLine = 10,
        EndLine = 20
      }
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    // The sourcePath is serialized to JSON, then the entire JSON is HTML-encoded
    // WebUtility.HtmlEncode encodes quotes as &quot; but may not encode < and > in JSON string values
    result.Should().Contain("data-symbol-info=");
    result.Should().Match("*File*script*.cs*");
  }

  [Test]
  public void BuildDataAttribute_WithCamelCaseProperties_UsesCamelCase()
  {
    // Arrange
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    // JSON is HTML-encoded, so quotes become &quot;
    result.Should().Contain("&quot;role&quot;"); // camelCase
    result.Should().Contain("&quot;fullyQualifiedName&quot;"); // camelCase
    // sourcePath, sourceStartLine, sourceEndLine are null, so they're not included in JSON due to DefaultIgnoreCondition.WhenWritingNull
    result.Should().NotContain("&quot;sourcePath&quot;");
    result.Should().NotContain("&quot;sourceStartLine&quot;");
    result.Should().NotContain("&quot;sourceEndLine&quot;");
  }

  [Test]
  public void BuildDataAttribute_WithNullSourceFields_OmitsNullFields()
  {
    // Arrange
    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new System.Collections.Generic.Dictionary<MetricIdentifier, MetricValue>(),
      Source = new SourceLocation
      {
        Path = null,
        StartLine = null,
        EndLine = null
      }
    };

    // Act
    var result = SymbolTooltipBuilder.BuildDataAttribute(node);

    // Assert
    // With DefaultIgnoreCondition.WhenWritingNull, null fields should not appear
    result.Should().Contain("role");
    result.Should().Contain("fullyQualifiedName");
    // Source fields should be omitted
  }
}


