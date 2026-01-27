namespace MetricsReporter.Tests.Aggregation;

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using MetricsReporter.Tests.TestHelpers;

/// <summary>
/// Unit tests for breakdown aggregation functionality in metrics merging.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class BreakdownAggregationTests
{
  private MetricsAggregationService service = null!;
  private Dictionary<MetricIdentifier, MetricThresholdDefinition> thresholds = null!;

  [SetUp]
  public void SetUp()
  {
    service = new MetricsAggregationService();
    thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>
    {
      [MetricIdentifier.SarifCaRuleViolations] = ThresholdTestFactory.CreateDefinition(1, 2, false),
      [MetricIdentifier.SarifIdeRuleViolations] = ThresholdTestFactory.CreateDefinition(1, 2, false)
    };
  }

  [Test]
  public void BuildReport_SingleCARuleBreakdown_PreservesBreakdown()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string memberFqn = "Sample.Namespace.SampleType.DoWork(...)";
    const string filePath = @"C:\Repo\Sample.cs";

    // Roslyn document creates structural elements
    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "SampleType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 5, EndLine = 20 }
        },
        new(CodeElementKind.Member, "DoWork", memberFqn)
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 18 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        }
      }
    };

    var sarifDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Member, "CA1502", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 10 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1502")
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var member = report.Solution.Assemblies
        .Single()
        .Namespaces.Single()
        .Types.Single()
        .Members.Single();

    var metric = member.Metrics[MetricIdentifier.SarifCaRuleViolations];
    metric.Value.Should().Be(1);
    metric.Breakdown.Should().NotBeNull();
    metric.Breakdown.Should().NotBeNull().And.ContainKey("CA1502");
    metric.Breakdown!["CA1502"].Count.Should().Be(1);
  }

  [Test]
  public void BuildReport_MultipleDifferentCARules_MergesBreakdown()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string filePath = @"C:\Repo\Sample.cs";

    // Roslyn document creates structural elements
    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "SampleType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 5, EndLine = 25 }
        }
      }
    };

    var sarifDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Member, "CA1502", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 10 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1502")
            }
          }
        },
        new(CodeElementKind.Member, "CA1506", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 15, EndLine = 15 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1506")
            }
          }
        },
        new(CodeElementKind.Member, "CA1502", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 20, EndLine = 20 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1502")
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var type = report.Solution.Assemblies
        .Single()
        .Namespaces.Single()
        .Types.Single();

    var metric = type.Metrics[MetricIdentifier.SarifCaRuleViolations];
    metric.Value.Should().Be(3, "Total violations should be 3");
    metric.Breakdown.Should().NotBeNull().And.ContainKey("CA1502");
    metric.Breakdown!["CA1502"].Count.Should().Be(2, "CA1502 should appear twice");
    metric.Breakdown["CA1502"].Violations.Should().HaveCount(2, "Each CA1502 violation should surface in tooltip data.");
    metric.Breakdown.Should().ContainKey("CA1506");
    metric.Breakdown["CA1506"].Count.Should().Be(1, "CA1506 should appear once");
    metric.Breakdown["CA1506"].Violations.Should().HaveCount(1);
  }

  [Test]
  public void BuildReport_MultipleIDERules_MergesBreakdown()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string filePath = @"C:\Repo\Sample.cs";

    // Roslyn document creates structural elements (assembly with type to register file in LineIndex)
    // Use line numbers outside type range to ensure metrics are applied to assembly, not to types
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "SampleType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 1, EndLine = 50 }
        }
      }
    };

    var sarifDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Member, "IDE0051", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 100, EndLine = 100 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifIdeRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("IDE0051")
            }
          }
        },
        new(CodeElementKind.Member, "IDE0028", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 101, EndLine = 101 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifIdeRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("IDE0028")
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Single();
    var metric = assembly.Metrics[MetricIdentifier.SarifIdeRuleViolations];
    metric.Value.Should().Be(2);
    metric.Breakdown.Should().NotBeNull().And.ContainKey("IDE0051");
    metric.Breakdown!["IDE0051"].Count.Should().Be(1);
    metric.Breakdown["IDE0051"].Violations.Should().HaveCount(1);
    metric.Breakdown.Should().ContainKey("IDE0028");
    metric.Breakdown["IDE0028"].Count.Should().Be(1);
    metric.Breakdown["IDE0028"].Violations.Should().HaveCount(1);
  }

  [Test]
  public void BuildReport_MixedCARulesAndIDERules_SeparateBreakdowns()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string filePath = @"C:\Repo\Sample.cs";

    // Roslyn document creates structural elements (assembly with type to register file in LineIndex)
    // Use line numbers outside type range to ensure metrics are applied to assembly, not to types
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "SampleType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 1, EndLine = 50 }
        }
      }
    };

    var sarifDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Member, "CA1502", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 100, EndLine = 100 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1502")
            }
          }
        },
        new(CodeElementKind.Member, "IDE0051", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 101, EndLine = 101 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifIdeRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("IDE0051")
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Single();

    // CA rules breakdown
    var caMetric = assembly.Metrics[MetricIdentifier.SarifCaRuleViolations];
    caMetric.Value.Should().Be(1);
    caMetric.Breakdown.Should().NotBeNull().And.ContainKey("CA1502");
    caMetric.Breakdown!["CA1502"].Count.Should().Be(1);

    // IDE rules breakdown
    var ideMetric = assembly.Metrics[MetricIdentifier.SarifIdeRuleViolations];
    ideMetric.Value.Should().Be(1);
    ideMetric.Breakdown.Should().NotBeNull().And.ContainKey("IDE0051");
    ideMetric.Breakdown!["IDE0051"].Count.Should().Be(1);
  }

  [Test]
  public void BuildReport_NullBreakdown_HandlesGracefully()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string filePath = @"C:\Repo\Sample.cs";

    // Roslyn document creates structural elements (assembly with type to register file in LineIndex)
    // Use different line numbers for SARIF to ensure metrics are applied to assembly, not to types
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "SampleType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 1, EndLine = 50 }
        }
      }
    };

    var sarifDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Member, "CA1502", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 100, EndLine = 100 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = null // Null breakdown
            }
          }
        },
        new(CodeElementKind.Member, "CA1506", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 101, EndLine = 101 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1506")
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Single();
    var metric = assembly.Metrics[MetricIdentifier.SarifCaRuleViolations];
    metric.Value.Should().Be(2);
    // When one breakdown is null, the other should be preserved
    metric.Breakdown.Should().NotBeNull().And.ContainKey("CA1506");
    metric.Breakdown!["CA1506"].Count.Should().Be(1);
  }

  [Test]
  public void BuildReport_EmptyBreakdown_HandlesGracefully()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string filePath = @"C:\Repo\Sample.cs";

    // Roslyn document creates structural elements (assembly with type to register file in LineIndex)
    // Use different line numbers for SARIF to ensure metrics are applied to assembly, not to types
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "SampleType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 1, EndLine = 50 }
        }
      }
    };

    var sarifDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Member, "CA1502", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 100, EndLine = 100 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Empty() // Empty breakdown
            }
          }
        },
        new(CodeElementKind.Member, "CA1506", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 101, EndLine = 101 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1506")
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Single();
    var metric = assembly.Metrics[MetricIdentifier.SarifCaRuleViolations];
    metric.Value.Should().Be(2);
    // Empty breakdown should be ignored, non-empty should be preserved
    metric.Breakdown.Should().NotBeNull().And.ContainKey("CA1506");
    metric.Breakdown!["CA1506"].Count.Should().Be(1);
  }

  [Test]
  public void BuildReport_BreakdownAggregatesAcrossHierarchy()
  {
    // Arrange - Multiple violations at different levels
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string filePath = @"C:\Repo\Sample.cs";

    // Roslyn document creates structural elements
    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "SampleType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 5, EndLine = 25 }
        }
      }
    };

    var sarifDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Member, "CA1502", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 10 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1502")
            }
          }
        },
        new(CodeElementKind.Member, "CA1506", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 15, EndLine = 15 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1506")
            }
          }
        },
        new(CodeElementKind.Member, "CA1502", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 20, EndLine = 20 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1502")
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert - Breakdown should aggregate at type level
    var type = report.Solution.Assemblies
        .Single()
        .Namespaces.Single()
        .Types.Single();

    var typeMetric = type.Metrics[MetricIdentifier.SarifCaRuleViolations];
    typeMetric.Value.Should().Be(3);
    typeMetric.Breakdown.Should().NotBeNull().And.ContainKey("CA1502");
    typeMetric.Breakdown!["CA1502"].Count.Should().Be(2);
    typeMetric.Breakdown["CA1502"].Violations.Should().HaveCount(2);
    typeMetric.Breakdown.Should().ContainKey("CA1506");
    typeMetric.Breakdown["CA1506"].Count.Should().Be(1);
    typeMetric.Breakdown["CA1506"].Violations.Should().HaveCount(1);
  }

  [Test]
  public void BuildReport_RealWorldScenario_MultipleRulesAndLocations()
  {
    // Arrange - Simulating real-world scenario with various rules
    const string assemblyName = "Sample.Assembly";
    const string filePath = @"C:\Repo\Sample.cs";

    // Roslyn document creates structural elements (assembly with type to register file in LineIndex)
    // Use line numbers outside type range to ensure metrics are applied to assembly, not to types
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "SampleType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 1, EndLine = 50 }
        }
      }
    };

    var sarifDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        // CA1502 violations
        new(CodeElementKind.Member, "CA1502", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 100, EndLine = 100 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1502")
            }
          }
        },
        new(CodeElementKind.Member, "CA1502", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 101, EndLine = 101 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1502")
            }
          }
        },
        // CA1506 violations
        new(CodeElementKind.Member, "CA1506", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 102, EndLine = 102 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1506")
            }
          }
        },
        // IDE0051 violations
        new(CodeElementKind.Member, "IDE0051", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 103, EndLine = 103 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifIdeRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("IDE0051")
            }
          }
        },
        new(CodeElementKind.Member, "IDE0051", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 104, EndLine = 104 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifIdeRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("IDE0051")
            }
          }
        },
        new(CodeElementKind.Member, "IDE0028", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 105, EndLine = 105 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifIdeRuleViolations] = new MetricValue
            {
              Value = 1,
              Unit = "count",
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("IDE0028")
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Single();

    // CA rules breakdown
    var caMetric = assembly.Metrics[MetricIdentifier.SarifCaRuleViolations];
    caMetric.Value.Should().Be(3);
    caMetric.Breakdown.Should().NotBeNull().And.ContainKey("CA1502");
    caMetric.Breakdown!["CA1502"].Count.Should().Be(2);
    caMetric.Breakdown["CA1502"].Violations.Should().HaveCount(2);
    caMetric.Breakdown.Should().ContainKey("CA1506");
    caMetric.Breakdown["CA1506"].Count.Should().Be(1);
    caMetric.Breakdown["CA1506"].Violations.Should().HaveCount(1);

    // IDE rules breakdown
    var ideMetric = assembly.Metrics[MetricIdentifier.SarifIdeRuleViolations];
    ideMetric.Value.Should().Be(3);
    ideMetric.Breakdown.Should().NotBeNull().And.ContainKey("IDE0051");
    ideMetric.Breakdown!["IDE0051"].Count.Should().Be(2);
    ideMetric.Breakdown["IDE0051"].Violations.Should().HaveCount(2);
    ideMetric.Breakdown.Should().ContainKey("IDE0028");
    ideMetric.Breakdown["IDE0028"].Count.Should().Be(1);
    ideMetric.Breakdown["IDE0028"].Violations.Should().HaveCount(1);
  }
}


