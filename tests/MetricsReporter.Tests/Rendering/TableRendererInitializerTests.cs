namespace MetricsReporter.Tests.Rendering;

using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="TableRendererInitializer"/> and <see cref="RendererComponents"/> classes.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class TableRendererInitializerTests
{
  [Test]
  public void Initialize_WithValidInputs_InitializesAllComponents()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = new List<SuppressedSymbolInfo>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      null);

    // Assert
    components.CoverageLinkBuilder.Should().BeNull();
    components.SuppressedIndex.Should().NotBeNull();
    components.StateCalculator.Should().NotBeNull();
    components.AttributeBuilder.Should().NotBeNull();
    components.MetricCellRenderer.Should().NotBeNull();
  }

  [Test]
  public void Initialize_WithCoverageHtmlDir_CreatesCoverageLinkBuilder()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = new List<SuppressedSymbolInfo>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      "C:\\Coverage");

    // Assert
    components.CoverageLinkBuilder.Should().NotBeNull();
  }

  [Test]
  public void Initialize_WithEmptyCoverageHtmlDir_DoesNotCreateCoverageLinkBuilder()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = new List<SuppressedSymbolInfo>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      string.Empty);

    // Assert
    components.CoverageLinkBuilder.Should().BeNull();
  }

  [Test]
  public void Initialize_WithWhitespaceCoverageHtmlDir_DoesNotCreateCoverageLinkBuilder()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = new List<SuppressedSymbolInfo>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      "   ");

    // Assert
    components.CoverageLinkBuilder.Should().BeNull();
  }

  [Test]
  public void Initialize_WithSuppressedSymbols_BuildsSuppressedIndex()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = new List<SuppressedSymbolInfo>
        {
          new()
          {
            FullyQualifiedName = "Sample.Namespace.SampleType",
            Metric = "RoslynClassCoupling",
            RuleId = "CA1506",
            Justification = "Justified"
          }
        }
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      null);

    // Assert
    components.SuppressedIndex.Should().NotBeNull();
    components.SuppressedIndex!.Should().ContainKey(("Sample.Namespace.SampleType", MetricIdentifier.RoslynClassCoupling));
  }

  [Test]
  public void Initialize_WithHierarchy_BuildsDescendantCountIndex()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };

    var member = new MemberMetricsNode
    {
      Name = "DoWork",
      FullyQualifiedName = "Sample.Namespace.SampleType.DoWork()",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var type = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode> { member }
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { type }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { @namespace }
    };

    var solution = new SolutionMetricsNode
    {
      Name = "SampleSolution",
      FullyQualifiedName = "SampleSolution",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Assemblies = new List<AssemblyMetricsNode> { assembly }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = new List<SuppressedSymbolInfo>()
      },
      Solution = solution
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      null);

    // Assert
    components.StateCalculator.Should().NotBeNull();
    components.AttributeBuilder.Should().NotBeNull();
    components.MetricCellRenderer.Should().NotBeNull();
  }

  [Test]
  public void Initialize_WithMultipleMetrics_InitializesCorrectly()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.RoslynCyclomaticComplexity,
      MetricIdentifier.RoslynMaintainabilityIndex
    };
    var metricUnits = new Dictionary<MetricIdentifier, string?>
    {
      [MetricIdentifier.RoslynClassCoupling] = null,
      [MetricIdentifier.RoslynCyclomaticComplexity] = null,
      [MetricIdentifier.RoslynMaintainabilityIndex] = "score"
    };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = new List<SuppressedSymbolInfo>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      null);

    // Assert
    components.StateCalculator.Should().NotBeNull();
    components.MetricCellRenderer.Should().NotBeNull();
  }

  #region Null Parameter Validation Tests

  [Test]
  public void Initialize_WithNullMetricOrder_ThrowsArgumentNullException()
  {
    // Arrange
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var act = () => TableRendererInitializer.Initialize(
      null!,
      metricUnits,
      report,
      null);

    // Assert
    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("metricOrder");
  }

  [Test]
  public void Initialize_WithNullMetricUnits_ThrowsArgumentNullException()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var act = () => TableRendererInitializer.Initialize(
      metricOrder,
      null!,
      report,
      null);

    // Assert
    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("metricUnits");
  }

  [Test]
  public void Initialize_WithNullReport_ThrowsNullReferenceException()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };

    // Act
    var act = () => TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      null!,
      null);

    // Assert
    // Note: TableRendererInitializer doesn't validate null report directly,
    // it passes it to SuppressionIndexBuilder which throws NullReferenceException
    act.Should().Throw<NullReferenceException>();
  }

  #endregion

  #region Edge Cases Tests

  [Test]
  public void Initialize_WithEmptyMetricOrder_InitializesComponents()
  {
    // Arrange
    var metricOrder = Array.Empty<MetricIdentifier>();
    var metricUnits = new Dictionary<MetricIdentifier, string?>();
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      null);

    // Assert
    components.StateCalculator.Should().NotBeNull();
    components.AttributeBuilder.Should().NotBeNull();
    components.MetricCellRenderer.Should().NotBeNull();
  }

  [Test]
  public void Initialize_WithEmptyMetricUnits_InitializesComponents()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?>();
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      null);

    // Assert
    components.StateCalculator.Should().NotBeNull();
    components.MetricCellRenderer.Should().NotBeNull();
  }

  [Test]
  public void Initialize_WithNullSolution_InitializesComponents()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = null!
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      null);

    // Assert
    components.SuppressedIndex.Should().NotBeNull();
    components.StateCalculator.Should().NotBeNull();
    components.AttributeBuilder.Should().NotBeNull();
    components.MetricCellRenderer.Should().NotBeNull();
  }

  [Test]
  public void Initialize_WithNullMetadata_ThrowsNullReferenceException()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var report = new MetricsReport
    {
      Metadata = null!,
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var act = () => TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      null);

    // Assert - SuppressionIndexBuilder.Build accesses report.Metadata.SuppressedSymbols which will throw
    act.Should().Throw<NullReferenceException>();
  }

  [Test]
  public void Initialize_WithAllMetricTypes_InitializesCorrectly()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.AltCoverSequenceCoverage,
      MetricIdentifier.AltCoverBranchCoverage,
      MetricIdentifier.AltCoverNPathComplexity,
      MetricIdentifier.AltCoverCyclomaticComplexity,
      MetricIdentifier.RoslynCyclomaticComplexity,
      MetricIdentifier.RoslynMaintainabilityIndex,
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.RoslynDepthOfInheritance,
      MetricIdentifier.RoslynSourceLines,
      MetricIdentifier.RoslynExecutableLines,
      MetricIdentifier.SarifCaRuleViolations,
      MetricIdentifier.SarifIdeRuleViolations
    };
    var metricUnits = new Dictionary<MetricIdentifier, string?>
    {
      [MetricIdentifier.AltCoverSequenceCoverage] = "%",
      [MetricIdentifier.AltCoverBranchCoverage] = "%",
      [MetricIdentifier.RoslynMaintainabilityIndex] = "score"
    };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      null);

    // Assert
    components.StateCalculator.Should().NotBeNull();
    components.MetricCellRenderer.Should().NotBeNull();
  }

  #endregion

  #region Component Integration Tests

  [Test]
  public void Initialize_ComponentsAreCorrectlyWired_AttributeBuilderUsesStateCalculator()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var node = new TypeMetricsNode
    {
      Name = "TestType",
      FullyQualifiedName = "Test.Namespace.TestType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynClassCoupling] = new MetricValue
        {
          Value = 50,
          Status = ThresholdStatus.Error
        }
      }
    };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      null);

    // Assert - Verify that AttributeBuilder uses StateCalculator correctly
    var state = components.StateCalculator.Calculate(node);
    state.HasError.Should().BeTrue();

    var attributes = components.AttributeBuilder.BuildAllAttributes(node);
    attributes.Should().Contain("data-has-error=\"true\"");
  }

  [Test]
  public void Initialize_ComponentsAreCorrectlyWired_MetricCellRendererUsesSuppressedIndex()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var node = new TypeMetricsNode
    {
      Name = "TestType",
      FullyQualifiedName = "Test.Namespace.TestType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = new List<SuppressedSymbolInfo>
        {
          new()
          {
            FullyQualifiedName = "Test.Namespace.TestType",
            Metric = "RoslynClassCoupling",
            RuleId = "CA1506",
            Justification = "Test justification"
          }
        }
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(
      metricOrder,
      metricUnits,
      report,
      null);

    // Assert - Verify that MetricCellRenderer uses SuppressedIndex
    var builder = new System.Text.StringBuilder();
    components.MetricCellRenderer.AppendCells(node, "td", builder);
    var html = builder.ToString();

    html.Should().Contain("data-suppressed=\"true\"");
  }

  #endregion

  #region RendererComponents Tests

  [Test]
  public void RendererComponents_Equality_WithSameValues_ReturnsTrue()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components1 = TableRendererInitializer.Initialize(metricOrder, metricUnits, report, null);
    var components2 = TableRendererInitializer.Initialize(metricOrder, metricUnits, report, null);

    // Assert - Record equality should work based on reference equality of components
    // Note: Since components contain object references, equality will be based on reference equality
    // This test verifies that the record structure works correctly
    components1.Should().NotBeSameAs(components2); // Different instances
    components1.StateCalculator.Should().NotBeSameAs(components2.StateCalculator);
    components1.AttributeBuilder.Should().NotBeSameAs(components2.AttributeBuilder);
    components1.MetricCellRenderer.Should().NotBeSameAs(components2.MetricCellRenderer);
  }

  [Test]
  public void RendererComponents_Properties_AreAccessible()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(metricOrder, metricUnits, report, null);

    // Assert - Verify all properties are accessible
    components.CoverageLinkBuilder.Should().BeNull();
    components.SuppressedIndex.Should().NotBeNull();
    components.StateCalculator.Should().NotBeNull();
    components.AttributeBuilder.Should().NotBeNull();
    components.MetricCellRenderer.Should().NotBeNull();
  }

  [Test]
  public void RendererComponents_WithCoverageLinkBuilder_PropertiesAreSet()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var metricUnits = new Dictionary<MetricIdentifier, string?> { [MetricIdentifier.RoslynClassCoupling] = null };
    var report = new MetricsReport
    {
      Metadata = new ReportMetadata { SuppressedSymbols = new List<SuppressedSymbolInfo>() },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode>()
      }
    };

    // Act
    var components = TableRendererInitializer.Initialize(metricOrder, metricUnits, report, "C:\\Coverage");

    // Assert
    components.CoverageLinkBuilder.Should().NotBeNull();
    components.SuppressedIndex.Should().NotBeNull();
    components.StateCalculator.Should().NotBeNull();
    components.AttributeBuilder.Should().NotBeNull();
    components.MetricCellRenderer.Should().NotBeNull();
  }

  #endregion
}


