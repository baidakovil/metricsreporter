namespace MetricsReporter.Tests.Rendering;

using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="SuppressionIndexBuilder"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class SuppressionIndexBuilderTests
{
  [Test]
  public void Build_WithValidSuppressions_BuildsIndex()
  {
    // Arrange
    var suppressedSymbols = new List<SuppressedSymbolInfo>
    {
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "Justified suppression"
      },
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType.DoWork()",
        Metric = "RoslynCyclomaticComplexity",
        RuleId = "CA1502",
        Justification = "Complex but necessary"
      }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = suppressedSymbols
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
    var result = SuppressionIndexBuilder.Build(report);

    // Assert
    result.Should().HaveCount(2);
    result.Should().ContainKey(("Sample.Namespace.SampleType", MetricIdentifier.RoslynClassCoupling));
    result.Should().ContainKey(("Sample.Namespace.SampleType.DoWork()", MetricIdentifier.RoslynCyclomaticComplexity));
    result[("Sample.Namespace.SampleType", MetricIdentifier.RoslynClassCoupling)].RuleId.Should().Be("CA1506");
    result[("Sample.Namespace.SampleType.DoWork()", MetricIdentifier.RoslynCyclomaticComplexity)].RuleId.Should().Be("CA1502");
  }

  [Test]
  public void Build_WithEmptySuppressions_ReturnsEmptyDictionary()
  {
    // Arrange
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
    var result = SuppressionIndexBuilder.Build(report);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void Build_WithDuplicateEntries_LastInWins()
  {
    // Arrange
    var suppressedSymbols = new List<SuppressedSymbolInfo>
    {
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "First"
      },
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "Second"
      }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = suppressedSymbols
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
    var result = SuppressionIndexBuilder.Build(report);

    // Assert
    result.Should().HaveCount(1);
    result[("Sample.Namespace.SampleType", MetricIdentifier.RoslynClassCoupling)].Justification.Should().Be("Second");
  }

  [Test]
  public void Build_WithInvalidMetricName_SkipsEntry()
  {
    // Arrange
    var suppressedSymbols = new List<SuppressedSymbolInfo>
    {
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "InvalidMetricName",
        RuleId = "CA1506",
        Justification = "Test"
      },
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType.DoWork()",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "Valid"
      }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = suppressedSymbols
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
    var result = SuppressionIndexBuilder.Build(report);

    // Assert
    result.Should().HaveCount(1);
    result.Should().ContainKey(("Sample.Namespace.SampleType.DoWork()", MetricIdentifier.RoslynClassCoupling));
    result.Should().NotContainKey(("Sample.Namespace.SampleType", MetricIdentifier.RoslynClassCoupling));
  }

  [Test]
  public void Build_WithNullOrEmptyFqn_SkipsEntry()
  {
    // Arrange
    var suppressedSymbols = new List<SuppressedSymbolInfo>
    {
      new()
      {
        FullyQualifiedName = string.Empty,
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "Test"
      },
      new()
      {
        FullyQualifiedName = string.Empty,
        Metric = "RoslynCyclomaticComplexity",
        RuleId = "CA1502",
        Justification = "Test"
      },
      new()
      {
        FullyQualifiedName = "   ",
        Metric = "RoslynMaintainabilityIndex",
        RuleId = "CA1501",
        Justification = "Test"
      },
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "Valid"
      }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = suppressedSymbols
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
    var result = SuppressionIndexBuilder.Build(report);

    // Assert
    result.Should().HaveCount(1);
    result.Should().ContainKey(("Sample.Namespace.SampleType", MetricIdentifier.RoslynClassCoupling));
  }

  [Test]
  public void Build_WithNullOrEmptyMetric_SkipsEntry()
  {
    // Arrange
    var suppressedSymbols = new List<SuppressedSymbolInfo>
    {
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = string.Empty,
        RuleId = "CA1506",
        Justification = "Test"
      },
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType.DoWork()",
        Metric = string.Empty,
        RuleId = "CA1502",
        Justification = "Test"
      },
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType.Process()",
        Metric = "   ",
        RuleId = "CA1501",
        Justification = "Test"
      },
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType.Valid()",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "Valid"
      }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = suppressedSymbols
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
    var result = SuppressionIndexBuilder.Build(report);

    // Assert
    result.Should().HaveCount(1);
    result.Should().ContainKey(("Sample.Namespace.SampleType.Valid()", MetricIdentifier.RoslynClassCoupling));
  }

  [Test]
  public void Build_WithDuplicateKeys_LastInWins()
  {
    // Arrange
    var suppressedSymbols = new List<SuppressedSymbolInfo>
    {
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "First justification"
      },
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "Second justification (wins)"
      }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = suppressedSymbols
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
    var result = SuppressionIndexBuilder.Build(report);

    // Assert
    result.Should().HaveCount(1);
    result[("Sample.Namespace.SampleType", MetricIdentifier.RoslynClassCoupling)].Justification.Should().Be("Second justification (wins)");
  }

  [Test]
  public void Build_WithAllMetricTypes_HandlesAllMetrics()
  {
    // Arrange
    var suppressedSymbols = new List<SuppressedSymbolInfo>
    {
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metric = "OpenCoverSequenceCoverage",
        RuleId = "CA0001",
        Justification = "OpenCover"
      },
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType.DoWork()",
        Metric = "RoslynClassCoupling",
        RuleId = "CA1506",
        Justification = "Roslyn"
      },
      new()
      {
        FullyQualifiedName = "Sample.Namespace.SampleType.Process()",
        Metric = "SarifCaRuleViolations",
        RuleId = "CA0002",
        Justification = "SARIF"
      }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        SuppressedSymbols = suppressedSymbols
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
    var result = SuppressionIndexBuilder.Build(report);

    // Assert
    result.Should().HaveCount(3);
    result.Should().ContainKey(("Sample.Namespace.SampleType", MetricIdentifier.OpenCoverSequenceCoverage));
    result.Should().ContainKey(("Sample.Namespace.SampleType.DoWork()", MetricIdentifier.RoslynClassCoupling));
    result.Should().ContainKey(("Sample.Namespace.SampleType.Process()", MetricIdentifier.SarifCaRuleViolations));
  }
}


