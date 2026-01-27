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
/// Unit tests for <see cref="MetricsAggregationService"/> focusing on rule descriptions filtering
/// based on rule IDs that are actually used in breakdown.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class MetricsAggregationServiceRuleDescriptionsFilteringTests
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
  public void BuildReport_RuleDescriptionInBreakdown_IncludesInMetadata()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string memberFqn = "Sample.Namespace.SampleType.DoWork(...)";
    const string filePath = @"C:\Repo\Sample.cs";

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
      },
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive complexity",
          FullDescription = "Methods should not have excessive cyclomatic complexity.",
          HelpUri = "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502",
          Category = "Maintainability"
        },
        ["CA1506"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive class coupling",
          FullDescription = "Types should not have excessive class coupling.",
          HelpUri = "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1506",
          Category = "Maintainability"
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
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
    report.Metadata.RuleDescriptions.Should().NotContainKey("CA1506", "CA1506 is not in breakdown");

    var description = report.Metadata.RuleDescriptions["CA1502"];
    description.ShortDescription.Should().Be("Avoid excessive complexity");
    description.FullDescription.Should().Be("Methods should not have excessive cyclomatic complexity.");
  }

  [Test]
  public void BuildReport_MultipleRulesInBreakdown_IncludesOnlyUsedRules()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string memberFqn1 = "Sample.Namespace.SampleType.DoWork(...)";
    const string memberFqn2 = "Sample.Namespace.SampleType.DoMore(...)";
    const string filePath = @"C:\Repo\Sample.cs";

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
          Source = new SourceLocation { Path = filePath, StartLine = 5, EndLine = 30 }
        },
        new(CodeElementKind.Member, "DoWork", memberFqn1)
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 18 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        },
        new(CodeElementKind.Member, "DoMore", memberFqn2)
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 20, EndLine = 28 },
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
        },
        new(CodeElementKind.Member, "CA1506", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 20, EndLine = 20 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1506")
            }
          }
        }
      },
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive complexity",
          FullDescription = "Methods should not have excessive cyclomatic complexity.",
          Category = "Maintainability"
        },
        ["CA1506"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive class coupling",
          FullDescription = "Types should not have excessive class coupling.",
          Category = "Maintainability"
        },
        ["CA1505"] = new RuleDescription
        {
          ShortDescription = "Avoid unmaintainable code",
          FullDescription = "Types should not have low maintainability index.",
          Category = "Maintainability"
        },
        ["IDE0051"] = new RuleDescription
        {
          ShortDescription = "Remove unused private members",
          FullDescription = "Private members that are never used should be removed.",
          Category = "Code Quality"
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
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1506");
    report.Metadata.RuleDescriptions.Should().NotContainKey("CA1505", "CA1505 is not in breakdown");
    report.Metadata.RuleDescriptions.Should().NotContainKey("IDE0051", "IDE0051 is not in breakdown");

    report.Metadata.RuleDescriptions.Count.Should().Be(2);
  }

  [Test]
  public void BuildReport_IDERulesInBreakdown_IncludesIDERuleDescriptions()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string memberFqn = "Sample.Namespace.SampleType.DoWork(...)";
    const string filePath = @"C:\Repo\Sample.cs";

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
        new(CodeElementKind.Member, "IDE0051", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 10 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifIdeRuleViolations] = new MetricValue
            {
              Value = 1,
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("IDE0051")
            }
          }
        }
      },
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["IDE0051"] = new RuleDescription
        {
          ShortDescription = "Remove unused private members",
          FullDescription = "Private members that are never used should be removed.",
          Category = "Code Quality"
        },
        ["IDE0028"] = new RuleDescription
        {
          ShortDescription = "Simplify collection initialization",
          FullDescription = "Collection initialization can be simplified.",
          Category = "Code Quality"
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
    report.Metadata.RuleDescriptions.Should().ContainKey("IDE0051");
    report.Metadata.RuleDescriptions.Should().NotContainKey("IDE0028", "IDE0028 is not in breakdown");

    var description = report.Metadata.RuleDescriptions["IDE0051"];
    description.ShortDescription.Should().Be("Remove unused private members");
  }

  [Test]
  public void BuildReport_MixedCARulesAndIDERulesInBreakdown_IncludesBoth()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string memberFqn1 = "Sample.Namespace.SampleType.DoWork(...)";
    const string memberFqn2 = "Sample.Namespace.SampleType.DoMore(...)";
    const string filePath = @"C:\Repo\Sample.cs";

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
          Source = new SourceLocation { Path = filePath, StartLine = 5, EndLine = 30 }
        },
        new(CodeElementKind.Member, "DoWork", memberFqn1)
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 18 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        },
        new(CodeElementKind.Member, "DoMore", memberFqn2)
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 20, EndLine = 28 },
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
        },
        new(CodeElementKind.Member, "IDE0051", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 20, EndLine = 20 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifIdeRuleViolations] = new MetricValue
            {
              Value = 1,
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("IDE0051")
            }
          }
        }
      },
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive complexity",
          FullDescription = "Methods should not have excessive cyclomatic complexity.",
          Category = "Maintainability"
        },
        ["CA1506"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive class coupling",
          FullDescription = "Types should not have excessive class coupling.",
          Category = "Maintainability"
        },
        ["IDE0051"] = new RuleDescription
        {
          ShortDescription = "Remove unused private members",
          FullDescription = "Private members that are never used should be removed.",
          Category = "Code Quality"
        },
        ["IDE0028"] = new RuleDescription
        {
          ShortDescription = "Simplify collection initialization",
          FullDescription = "Collection initialization can be simplified.",
          Category = "Code Quality"
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
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
    report.Metadata.RuleDescriptions.Should().ContainKey("IDE0051");
    report.Metadata.RuleDescriptions.Should().NotContainKey("CA1506", "CA1506 is not in breakdown");
    report.Metadata.RuleDescriptions.Should().NotContainKey("IDE0028", "IDE0028 is not in breakdown");

    report.Metadata.RuleDescriptions.Count.Should().Be(2);
  }

  [Test]
  public void BuildReport_MultipleRulesInSameType_CollectsAllRuleIds()
  {
    // Arrange - Two different rules in breakdown, but only those should be in metadata
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string filePath = @"C:\Repo\Sample.cs";

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
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1506")
            }
          }
        }
      },
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive complexity",
          FullDescription = "Methods should not have excessive cyclomatic complexity.",
          Category = "Maintainability"
        },
        ["CA1506"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive class coupling",
          FullDescription = "Types should not have excessive class coupling.",
          Category = "Maintainability"
        },
        ["CA1505"] = new RuleDescription
        {
          ShortDescription = "Avoid unmaintainable code",
          FullDescription = "Types should not have low maintainability index.",
          Category = "Maintainability"
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

    // Assert - Rule descriptions should include both rules that are in breakdown
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1506");
    report.Metadata.RuleDescriptions.Should().NotContainKey("CA1505", "CA1505 is not in breakdown");

    report.Metadata.RuleDescriptions.Count.Should().Be(2);
  }

  [Test]
  public void BuildReport_NoBreakdown_IncludesAllRuleDescriptions()
  {
    // Arrange
    var sarifDocument = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive complexity",
          FullDescription = "Methods should not have excessive cyclomatic complexity.",
          Category = "Maintainability"
        },
        ["CA1506"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive class coupling",
          FullDescription = "Types should not have excessive class coupling.",
          Category = "Maintainability"
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert - When there's no breakdown, all rule descriptions should be included (backward compatibility)
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1506");
    report.Metadata.RuleDescriptions.Count.Should().Be(2);
  }

  [Test]
  public void BuildReport_EmptyBreakdown_IncludesAllRuleDescriptions()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string memberFqn = "Sample.Namespace.SampleType.DoWork(...)";
    const string filePath = @"C:\Repo\Sample.cs";

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
              Value = 0,
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Empty() // Empty breakdown
            }
          }
        }
      },
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive complexity",
          FullDescription = "Methods should not have excessive cyclomatic complexity.",
          Category = "Maintainability"
        },
        ["CA1506"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive class coupling",
          FullDescription = "Types should not have excessive class coupling.",
          Category = "Maintainability"
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

    // Assert - When breakdown is empty, all rule descriptions should be included (backward compatibility)
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1506");
    report.Metadata.RuleDescriptions.Count.Should().Be(2);
  }

  [Test]
  public void BuildReport_RuleIdInBreakdownButNotInDescriptions_DoesNotThrow()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string memberFqn = "Sample.Namespace.SampleType.DoWork(...)";
    const string filePath = @"C:\Repo\Sample.cs";

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
              Breakdown = SarifBreakdownTestHelper.Create(("CA1502", 1), ("CA9999", 1)) // CA9999 not in descriptions
            }
          }
        }
      },
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive complexity",
          FullDescription = "Methods should not have excessive cyclomatic complexity.",
          Category = "Maintainability"
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

    // Act & Assert - Should not throw even if rule ID in breakdown doesn't have a description
    var act = () => service.BuildReport(input);
    act.Should().NotThrow();

    var report = service.BuildReport(input);
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
    report.Metadata.RuleDescriptions.Should().NotContainKey("CA9999", "CA9999 has no description");
    report.Metadata.RuleDescriptions.Count.Should().Be(1);
  }

  [Test]
  public void BuildReport_MultipleAssembliesWithDifferentRules_IncludesAllUsedRules()
  {
    // Arrange
    const string assemblyName1 = "Sample.Assembly1";
    const string assemblyName2 = "Sample.Assembly2";
    const string namespaceFqn1 = "Sample.Namespace1";
    const string namespaceFqn2 = "Sample.Namespace2";
    const string typeFqn1 = "Sample.Namespace1.SampleType1";
    const string typeFqn2 = "Sample.Namespace2.SampleType2";
    const string memberFqn1 = "Sample.Namespace1.SampleType1.DoWork(...)";
    const string memberFqn2 = "Sample.Namespace2.SampleType2.DoMore(...)";
    const string filePath1 = @"C:\Repo\Sample1.cs";
    const string filePath2 = @"C:\Repo\Sample2.cs";

    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName1, assemblyName1),
        new(CodeElementKind.Namespace, namespaceFqn1, namespaceFqn1)
        {
          ParentFullyQualifiedName = assemblyName1
        },
        new(CodeElementKind.Type, "SampleType1", typeFqn1)
        {
          ParentFullyQualifiedName = namespaceFqn1,
          Source = new SourceLocation { Path = filePath1, StartLine = 5, EndLine = 20 }
        },
        new(CodeElementKind.Member, "DoWork", memberFqn1)
        {
          ParentFullyQualifiedName = typeFqn1,
          Source = new SourceLocation { Path = filePath1, StartLine = 10, EndLine = 18 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        },
        new(CodeElementKind.Assembly, assemblyName2, assemblyName2),
        new(CodeElementKind.Namespace, namespaceFqn2, namespaceFqn2)
        {
          ParentFullyQualifiedName = assemblyName2
        },
        new(CodeElementKind.Type, "SampleType2", typeFqn2)
        {
          ParentFullyQualifiedName = namespaceFqn2,
          Source = new SourceLocation { Path = filePath2, StartLine = 5, EndLine = 20 }
        },
        new(CodeElementKind.Member, "DoMore", memberFqn2)
        {
          ParentFullyQualifiedName = typeFqn2,
          Source = new SourceLocation { Path = filePath2, StartLine = 10, EndLine = 18 },
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
          Source = new SourceLocation { Path = filePath1, StartLine = 10, EndLine = 10 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1502")
            }
          }
        },
        new(CodeElementKind.Member, "CA1506", null)
        {
          Source = new SourceLocation { Path = filePath2, StartLine = 10, EndLine = 10 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1506")
            }
          }
        }
      },
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive complexity",
          FullDescription = "Methods should not have excessive cyclomatic complexity.",
          Category = "Maintainability"
        },
        ["CA1506"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive class coupling",
          FullDescription = "Types should not have excessive class coupling.",
          Category = "Maintainability"
        },
        ["CA1505"] = new RuleDescription
        {
          ShortDescription = "Avoid unmaintainable code",
          FullDescription = "Types should not have low maintainability index.",
          Category = "Maintainability"
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
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1506");
    report.Metadata.RuleDescriptions.Should().NotContainKey("CA1505", "CA1505 is not in breakdown");

    report.Metadata.RuleDescriptions.Count.Should().Be(2);
  }
}


