namespace MetricsReporter.Tests.Rendering;

using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="RowAttributeBuilder"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class RowAttributeBuilderTests
{
  [Test]
  public void Constructor_WithNullStateCalculator_ThrowsArgumentNullException()
  {
    // Act & Assert
    FluentActions.Invoking(() => new RowAttributeBuilder(null!, null))
      .Should().Throw<ArgumentNullException>()
      .WithParameterName("stateCalculator");
  }

  [Test]
  public void BuildAllAttributes_WithCompleteNode_BuildsAllAttributes()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var descendantCountIndex = new Dictionary<MetricsNode, int>();
    var builder = new RowAttributeBuilder(stateCalculator, descendantCountIndex);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 50,
          Status = ThresholdStatus.Error
        }
      },
      Source = new SourceLocation
      {
        Path = "C:\\Source\\File.cs",
        StartLine = 10,
        EndLine = 20
      }
    };

    descendantCountIndex[node] = 5;

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().Contain("data-source-path");
    result.Should().Contain("data-source-line");
    result.Should().Contain("data-source-end-line");
    result.Should().Contain("data-has-error=\"true\"");
    result.Should().Contain("data-filter-key");
    result.Should().Contain("data-descendant-count=\"5\"");
    result.Should().Contain("data-hidden-by-detail=\"false\"");
    result.Should().Contain("data-expanded=\"true\"");
  }

  [Test]
  public void BuildAllAttributes_WithSourcePathOnly_BuildsSourceAttributes()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var builder = new RowAttributeBuilder(stateCalculator, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Source = new SourceLocation
      {
        Path = "C:\\Source\\File.cs",
        StartLine = 10,
        EndLine = null
      }
    };

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().Contain("data-source-path");
    result.Should().Contain("data-source-line=\"10\"");
    result.Should().NotContain("data-source-end-line");
  }

  [Test]
  public void BuildAllAttributes_WithNullSource_DoesNotBuildSourceAttributes()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var builder = new RowAttributeBuilder(stateCalculator, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Source = null
    };

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().NotContain("data-source-path");
    result.Should().NotContain("data-source-line");
  }

  [Test]
  public void BuildAllAttributes_WithNullStartLine_DoesNotBuildSourceAttributes()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var builder = new RowAttributeBuilder(stateCalculator, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Source = new SourceLocation
      {
        Path = "C:\\Source\\File.cs",
        StartLine = null,
        EndLine = 20
      }
    };

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().NotContain("data-source-path");
    result.Should().NotContain("data-source-line");
  }

  [Test]
  public void BuildAllAttributes_WithHtmlInPath_EscapesHtml()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var builder = new RowAttributeBuilder(stateCalculator, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Source = new SourceLocation
      {
        Path = "C:\\Source\\File<test>.cs",
        StartLine = 10,
        EndLine = null
      }
    };

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().Contain("File&lt;test&gt;.cs");
    result.Should().NotContain("File<test>.cs");
  }

  [Test]
  public void BuildAllAttributes_WithFullyQualifiedName_UsesFqnForFilter()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var builder = new RowAttributeBuilder(stateCalculator, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().Contain("data-filter-key=\"sample.namespace.sampletype\"");
  }

  [Test]
  public void BuildAllAttributes_WithNullFqn_UsesNameForFilter()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var builder = new RowAttributeBuilder(stateCalculator, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = null,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().Contain("data-filter-key=\"sampletype\"");
  }

  [Test]
  public void BuildAllAttributes_WithEmptyFqnAndName_ReturnsEmptyFilterKey()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var builder = new RowAttributeBuilder(stateCalculator, null);

    var node = new TypeMetricsNode
    {
      Name = string.Empty,
      FullyQualifiedName = string.Empty,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().Contain("data-filter-key=\"\"");
  }

  [Test]
  public void BuildAllAttributes_WithDescendantCount_BuildsDescendantAttribute()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var descendantCountIndex = new Dictionary<MetricsNode, int>();
    var builder = new RowAttributeBuilder(stateCalculator, descendantCountIndex);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    descendantCountIndex[node] = 10;

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().Contain("data-descendant-count=\"10\"");
  }

  [Test]
  public void BuildAllAttributes_WithZeroDescendantCount_DoesNotBuildDescendantAttribute()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var descendantCountIndex = new Dictionary<MetricsNode, int>();
    var builder = new RowAttributeBuilder(stateCalculator, descendantCountIndex);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    descendantCountIndex[node] = 0;

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().NotContain("data-descendant-count");
  }

  [Test]
  public void BuildAllAttributes_WithNegativeDescendantCount_DoesNotBuildDescendantAttribute()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var descendantCountIndex = new Dictionary<MetricsNode, int>();
    var builder = new RowAttributeBuilder(stateCalculator, descendantCountIndex);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    descendantCountIndex[node] = -1;

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().NotContain("data-descendant-count");
  }

  [Test]
  public void BuildAllAttributes_WithNullDescendantIndex_DoesNotBuildDescendantAttribute()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var builder = new RowAttributeBuilder(stateCalculator, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().NotContain("data-descendant-count");
  }

  [Test]
  public void BuildAllAttributes_WithAllStateFlags_BuildsStateAttributes()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.RoslynCyclomaticComplexity,
      MetricIdentifier.RoslynMaintainabilityIndex
    };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var builder = new RowAttributeBuilder(stateCalculator, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 50,
          Status = ThresholdStatus.Error
        },
        [MetricIdentifier.RoslynCyclomaticComplexity] = new MetricValue
        {
          Value = 30,
          Status = ThresholdStatus.Warning,
          Delta = 5
        }
      }
    };

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().Contain("data-has-error=\"true\"");
    result.Should().Contain("data-has-warning=\"true\"");
    result.Should().Contain("data-has-delta=\"true\"");
    result.Should().Contain("data-has-suppressed=\"false\"");
  }

  [Test]
  public void BuildAllAttributes_AlwaysIncludesVisibilityAttributes()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var builder = new RowAttributeBuilder(stateCalculator, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().Contain("data-hidden-by-detail=\"false\"");
    result.Should().Contain("data-hidden-by-filter=\"false\"");
    result.Should().Contain("data-hidden-by-awareness=\"false\"");
    result.Should().Contain("data-hidden-by-state=\"false\"");
    result.Should().Contain("data-expanded=\"true\"");
  }

  [Test]
  public void BuildAllAttributes_WithSpecialCharactersInFqn_EscapesHtml()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var stateCalculator = new RowStateCalculator(metricOrder, null);
    var builder = new RowAttributeBuilder(stateCalculator, null);

    var node = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace<Test>.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    // Act
    var result = builder.BuildAllAttributes(node);

    // Assert
    result.Should().Contain("data-filter-key=\"sample.namespace&lt;test&gt;.sampletype\"");
    result.Should().NotContain("Sample.Namespace<Test>.SampleType");
  }
}


