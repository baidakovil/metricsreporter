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

[TestFixture]
[Category("Unit")]
public sealed class MetricsAggregationServiceTests
{
  private MetricsAggregationService service = null!;
  private Dictionary<MetricIdentifier, MetricThresholdDefinition> thresholds = null!;

  [SetUp]
  public void SetUp()
  {
    service = new MetricsAggregationService();
    thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>
    {
      [MetricIdentifier.RoslynMaintainabilityIndex] = ThresholdTestFactory.CreateDefinition(65, 40, true),
      [MetricIdentifier.AltCoverSequenceCoverage] = ThresholdTestFactory.CreateDefinition(70, 50, true),
      [MetricIdentifier.SarifCaRuleViolations] = ThresholdTestFactory.CreateDefinition(1, 2, false)
    };
  }

  [Test]
  public void BuildReport_MergesSourcesAndCalculatesDeltas()
  {
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    // Use normalized FQN format (with ...) to match what the normalization produces
    const string memberFqn = "Sample.Namespace.SampleType.DoWork(...)";
    const string filePath = @"C:\Repo\Sample.cs";

    var roslynDocument = new ParsedMetricsDocument
    {
      SolutionName = "SampleSolution",
      Elements = new List<ParsedCodeElement>
            {
                new(CodeElementKind.Assembly, assemblyName, assemblyName)
                {
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(80, "score")
                    }
                },
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
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(80, "score")
                    }
                },
                new(CodeElementKind.Member, "NewWork", $"{typeFqn}.NewWork(...)")
                {
                    ParentFullyQualifiedName = typeFqn,
                    Source = new SourceLocation { Path = filePath, StartLine = 30, EndLine = 35 },
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(55, "score")
                    }
                }
            }
    };

    var altCoverDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
            {
                new(CodeElementKind.Assembly, assemblyName, assemblyName),
                new(CodeElementKind.Type, "Sample.Namespace.SampleType", typeFqn)
                {
                    ParentFullyQualifiedName = assemblyName,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>()
                },
                new(CodeElementKind.Member, "Sample.Namespace.SampleType::DoWork()", memberFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 18 },
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(95, "percent")
                    }
                }
            }
    };

    var sarifDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
            {
                new(CodeElementKind.Member, "CA1000", null)
                {
                    Source = new SourceLocation { Path = filePath, StartLine = 12, EndLine = 12 },
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.SarifCaRuleViolations] = Metric(1, "count")
                    }
                }
            }
    };

    var baselineReport = CreateBaselineReport(assemblyName, namespaceFqn, typeFqn, memberFqn, 75);

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      Baseline = baselineReport,
      Thresholds = thresholds,
      Paths = new ReportPaths
      {
        MetricsDirectory = @"C:\Repo\build\Metrics",
        Baseline = @"C:\Repo\build\Metrics\Report\metrics-baseline.json",
        Report = @"C:\Repo\build\Metrics\Report\metrics-report.json",
        Html = @"C:\Repo\build\Metrics\Report\metrics-report.html"
      }
    };

    var report = service.BuildReport(input);

    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    assembly.IsNew.Should().BeFalse();

    var type = assembly.Namespaces.Should().ContainSingle().Subject.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn).Subject;
    var existingMember = type.Members.Should().ContainSingle(m => m.FullyQualifiedName == memberFqn).Subject;
    var newMember = type.Members.Should().ContainSingle(m => m.FullyQualifiedName!.EndsWith("NewWork(...)")).Subject;

    existingMember.IsNew.Should().BeFalse();
    existingMember.Metrics[MetricIdentifier.RoslynMaintainabilityIndex].Value.Should().Be(80);
    existingMember.Metrics[MetricIdentifier.RoslynMaintainabilityIndex].Delta.Should().Be(5);
    existingMember.Metrics[MetricIdentifier.RoslynMaintainabilityIndex].Status.Should().Be(ThresholdStatus.Success);
    existingMember.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(95);
    existingMember.Metrics[MetricIdentifier.SarifCaRuleViolations].Value.Should().Be(1);

    newMember.IsNew.Should().BeTrue();
    newMember.Metrics[MetricIdentifier.RoslynMaintainabilityIndex].Value.Should().Be(55);
    newMember.Metrics[MetricIdentifier.RoslynMaintainabilityIndex].Delta.Should().BeNull();
    newMember.Metrics[MetricIdentifier.RoslynMaintainabilityIndex].Status.Should().Be(ThresholdStatus.Warning);
  }

  [Test]
  public void BuildReport_NoDocuments_ReturnsEmptySolution()
  {
    // Verifies the workflow handles empty inputs without failing and produces an empty solution tree.
    // Ensures ProcessDocuments branches where no documents exist are covered.
    var input = new MetricsAggregationInput
    {
      SolutionName = "EmptySolution",
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    var report = service.BuildReport(input);

    report.Solution.Assemblies.Should().BeEmpty();
    report.Metadata.Should().NotBeNull();
  }

  [Test]
  public void BuildReport_PopulatesTypeSourceFromMemberMetadata()
  {
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string memberFqn = "Sample.Namespace.SampleType.DoWork(...)";
    const string filePath = @"C:\Repo\SampleType.cs";

    var document = new ParsedMetricsDocument
    {
      SolutionName = "SampleSolution",
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "SampleType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn
        },
        new(CodeElementKind.Member, "DoWork", memberFqn)
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation
          {
            Path = filePath,
            StartLine = 40,
            EndLine = 45
          }
        },
        new(CodeElementKind.Member, "DoMore", $"{typeFqn}.DoMore(...)")
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation
          {
            Path = filePath,
            StartLine = 50,
            EndLine = 55
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      RoslynDocuments = new List<ParsedMetricsDocument> { document },
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    var report = service.BuildReport(input);

    var typeNode = report.Solution.Assemblies.Single().Namespaces.Single().Types.Single();
    typeNode.Source.Should().NotBeNull();
    typeNode.Source!.Path.Should().Be(filePath);
    typeNode.Source.StartLine.Should().Be(40);
    typeNode.Source.EndLine.Should().Be(55);
  }

  [Test]
  public void BuildReport_TypeMembersAcrossFiles_UsesDominantFileForSource()
  {
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string dominantFile = @"C:\Repo\SampleType.Part1.cs";
    const string minorityFile = @"C:\Repo\SampleType.Part2.cs";

    var document = new ParsedMetricsDocument
    {
      SolutionName = "SampleSolution",
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "SampleType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn
        },
        new(CodeElementKind.Member, "DoWork", $"{typeFqn}.DoWork(...)")
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation
          {
            Path = dominantFile,
            StartLine = 10,
            EndLine = 20
          }
        },
        new(CodeElementKind.Member, "DoStuff", $"{typeFqn}.DoStuff(...)")
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation
          {
            Path = dominantFile,
            StartLine = 30,
            EndLine = 40
          }
        },
        new(CodeElementKind.Member, "DoMinor", $"{typeFqn}.DoMinor(...)")
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation
          {
            Path = minorityFile,
            StartLine = 5,
            EndLine = 6
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      RoslynDocuments = new List<ParsedMetricsDocument> { document },
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    var report = service.BuildReport(input);

    var typeNode = report.Solution.Assemblies.Single().Namespaces.Single().Types.Single();
    typeNode.Source.Should().NotBeNull();
    typeNode.Source!.Path.Should().Be(dominantFile);
    typeNode.Source.StartLine.Should().Be(10);
    typeNode.Source.EndLine.Should().Be(40);
  }

  [Test]
  public void BuildReport_TypeWithExistingPath_PreservesPathAndAddsLineNumbers()
  {
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string preservedPath = @"C:\Repo\Existing.cs";

    var document = new ParsedMetricsDocument
    {
      SolutionName = "SampleSolution",
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
          Source = new SourceLocation
          {
            Path = preservedPath
          }
        },
        new(CodeElementKind.Member, "DoWork", $"{typeFqn}.DoWork(...)")
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation
          {
            Path = preservedPath,
            StartLine = 15,
            EndLine = 20
          }
        },
        new(CodeElementKind.Member, "DoMore", $"{typeFqn}.DoMore(...)")
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation
          {
            Path = preservedPath,
            StartLine = 22,
            EndLine = 25
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      RoslynDocuments = new List<ParsedMetricsDocument> { document },
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    var report = service.BuildReport(input);

    var typeNode = report.Solution.Assemblies.Single().Namespaces.Single().Types.Single();
    typeNode.Source.Should().NotBeNull();
    typeNode.Source!.Path.Should().Be(preservedPath);
    typeNode.Source.StartLine.Should().Be(15);
    typeNode.Source.EndLine.Should().Be(25);
  }

  [Test]
  public void BuildReport_SarifAssemblyValueAtWarning_RemainsSuccess()
  {
    // Arrange
    const string assemblyName = "Inclusive.Assembly";

    var serviceUnderTest = new MetricsAggregationService();
    var thresholds = ThresholdTestFactory.CreateUniformThresholds(
        (MetricIdentifier.SarifCaRuleViolations, 1m, 2m, false));

    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
            {
                new(CodeElementKind.Assembly, assemblyName, assemblyName)
                {
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.SarifCaRuleViolations] = Metric(1, "count")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "InclusiveSolution",
      SarifDocuments = new List<ParsedMetricsDocument>(),
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = serviceUnderTest.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    assembly.Metrics[MetricIdentifier.SarifCaRuleViolations].Status.Should().Be(ThresholdStatus.Success);
  }

  [Test]
  public void BuildReport_SarifAssemblyValueAboveWarning_ProducesWarning()
  {
    // Arrange
    const string assemblyName = "Inclusive.Assembly";

    var serviceUnderTest = new MetricsAggregationService();
    var thresholds = ThresholdTestFactory.CreateUniformThresholds(
        (MetricIdentifier.SarifCaRuleViolations, 1m, 2m, false));

    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
            {
                new(CodeElementKind.Assembly, assemblyName, assemblyName)
                {
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.SarifCaRuleViolations] = Metric(1.5m, "count")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "InclusiveSolution",
      SarifDocuments = new List<ParsedMetricsDocument>(),
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = serviceUnderTest.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    assembly.Metrics[MetricIdentifier.SarifCaRuleViolations].Status.Should().Be(ThresholdStatus.Warning);
  }

  [Test]
  public void BuildReport_MaintainabilityValueAtWarning_RemainsSuccess()
  {
    // Arrange
    const string assemblyName = "Inclusive.Assembly";
    const string namespaceName = "Inclusive.Namespace";
    const string typeFqn = "Inclusive.Namespace.SampleType";

    var serviceUnderTest = new MetricsAggregationService();
    var thresholds = ThresholdTestFactory.CreateUniformThresholds(
        (MetricIdentifier.RoslynMaintainabilityIndex, 65m, 40m, true));

    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
            {
                new(CodeElementKind.Assembly, assemblyName, assemblyName),
                new(CodeElementKind.Namespace, namespaceName, namespaceName)
                {
                    ParentFullyQualifiedName = assemblyName
                },
                new(CodeElementKind.Type, "SampleType", typeFqn)
                {
                    ParentFullyQualifiedName = namespaceName,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(65, "score")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "InclusiveSolution",
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = serviceUnderTest.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var @namespace = assembly.Namespaces.Should().ContainSingle(n => n.Name == namespaceName).Subject;
    var type = @namespace.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn).Subject;
    type.Metrics[MetricIdentifier.RoslynMaintainabilityIndex].Status.Should().Be(ThresholdStatus.Success);
  }

  [Test]
  public void BuildReport_MaintainabilityValueBelowWarning_ProducesWarning()
  {
    // Arrange
    const string assemblyName = "Inclusive.Assembly";
    const string namespaceName = "Inclusive.Namespace";
    const string typeFqn = "Inclusive.Namespace.SampleType";

    var serviceUnderTest = new MetricsAggregationService();
    var thresholds = ThresholdTestFactory.CreateUniformThresholds(
        (MetricIdentifier.RoslynMaintainabilityIndex, 65m, 40m, true));

    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
            {
                new(CodeElementKind.Assembly, assemblyName, assemblyName),
                new(CodeElementKind.Namespace, namespaceName, namespaceName)
                {
                    ParentFullyQualifiedName = assemblyName
                },
                new(CodeElementKind.Type, "SampleType", typeFqn)
                {
                    ParentFullyQualifiedName = namespaceName,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(60, "score")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "InclusiveSolution",
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = serviceUnderTest.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var @namespace = assembly.Namespaces.Should().ContainSingle(n => n.Name == namespaceName).Subject;
    var type = @namespace.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn).Subject;
    type.Metrics[MetricIdentifier.RoslynMaintainabilityIndex].Status.Should().Be(ThresholdStatus.Warning);
  }

  [Test]
  public void BuildReport_ExcludesConstructorMethods_FromAltCover()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string constructorFqn = "Sample.Namespace.SampleType..ctor(...)";
    const string staticConstructorFqn = "Sample.Namespace.SampleType..cctor(...)";
    const string normalMethodFqn = "Sample.Namespace.SampleType.DoWork(...)";

    var altCoverDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
            {
                new(CodeElementKind.Assembly, assemblyName, assemblyName),
                new(CodeElementKind.Type, "Sample.Namespace.SampleType", typeFqn)
                {
                    ParentFullyQualifiedName = assemblyName
                },
                new(CodeElementKind.Member, ".ctor", constructorFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(100, "percent")
                    }
                },
                new(CodeElementKind.Member, ".cctor", staticConstructorFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(100, "percent")
                    }
                },
                new(CodeElementKind.Member, "DoWork", normalMethodFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(95, "percent")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var type = assembly.Namespaces.Should().ContainSingle().Subject.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn).Subject;

    // Constructors should be excluded
    type.Members.Should().NotContain(m => m.FullyQualifiedName == constructorFqn);
    type.Members.Should().NotContain(m => m.FullyQualifiedName == staticConstructorFqn);

    // Normal method should be included
    type.Members.Should().ContainSingle(m => m.FullyQualifiedName == normalMethodFqn);
  }

  [Test]
  public void BuildReport_ExcludesConstructorMethods_FromRoslyn()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    // Roslyn format: constructor name matches type name
    const string constructorFqn = "Sample.Namespace.SampleType.SampleType(...)";
    const string normalMethodFqn = "Sample.Namespace.SampleType.DoWork(...)";

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
                    ParentFullyQualifiedName = namespaceFqn
                },
                new(CodeElementKind.Member, "SampleType", constructorFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(80, "score")
                    }
                },
                new(CodeElementKind.Member, "DoWork", normalMethodFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(80, "score")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var type = assembly.Namespaces.Should().ContainSingle().Subject.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn).Subject;

    // Constructor should be excluded
    type.Members.Should().NotContain(m => m.FullyQualifiedName == constructorFqn);

    // Normal method should be included
    type.Members.Should().ContainSingle(m => m.FullyQualifiedName == normalMethodFqn);
  }

  [Test]
  public void BuildReport_ExcludesCompilerGeneratedMethods()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string moveNextFqn = "Sample.Namespace.SampleType.MoveNext(...)";
    const string setStateMachineFqn = "Sample.Namespace.SampleType.SetStateMachine(...)";
    const string moveNextAsyncFqn = "Sample.Namespace.SampleType.MoveNextAsync(...)";
    const string disposeAsyncFqn = "Sample.Namespace.SampleType.DisposeAsync(...)";
    const string normalMethodFqn = "Sample.Namespace.SampleType.DoWork(...)";

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
                    ParentFullyQualifiedName = namespaceFqn
                },
                new(CodeElementKind.Member, "MoveNext", moveNextFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(80, "score")
                    }
                },
                new(CodeElementKind.Member, "SetStateMachine", setStateMachineFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(80, "score")
                    }
                },
                new(CodeElementKind.Member, "MoveNextAsync", moveNextAsyncFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(80, "score")
                    }
                },
                new(CodeElementKind.Member, "DisposeAsync", disposeAsyncFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(80, "score")
                    }
                },
                new(CodeElementKind.Member, "DoWork", normalMethodFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(80, "score")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var type = assembly.Namespaces.Should().ContainSingle().Subject.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn).Subject;

    // Compiler-generated methods should be excluded
    type.Members.Should().NotContain(m => m.FullyQualifiedName == moveNextFqn);
    type.Members.Should().NotContain(m => m.FullyQualifiedName == setStateMachineFqn);
    type.Members.Should().NotContain(m => m.FullyQualifiedName == moveNextAsyncFqn);
    type.Members.Should().NotContain(m => m.FullyQualifiedName == disposeAsyncFqn);

    // Normal method should be included
    type.Members.Should().ContainSingle(m => m.FullyQualifiedName == normalMethodFqn);
  }

  [Test]
  public void BuildReport_ExcludedMethods_NotInJsonOutput()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string constructorFqn = "Sample.Namespace.SampleType..ctor(...)";
    const string moveNextFqn = "Sample.Namespace.SampleType.MoveNext(...)";
    const string normalMethodFqn = "Sample.Namespace.SampleType.DoWork(...)";

    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
            {
                new(CodeElementKind.Assembly, assemblyName, assemblyName),
                new(CodeElementKind.Namespace, "Sample.Namespace", "Sample.Namespace")
                {
                    ParentFullyQualifiedName = assemblyName
                },
                new(CodeElementKind.Type, "SampleType", typeFqn)
                {
                    ParentFullyQualifiedName = "Sample.Namespace"
                },
                new(CodeElementKind.Member, ".ctor", constructorFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(80, "score")
                    }
                },
                new(CodeElementKind.Member, "MoveNext", moveNextFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(80, "score")
                    }
                },
                new(CodeElementKind.Member, "DoWork", normalMethodFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.RoslynMaintainabilityIndex] = Metric(80, "score")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert - Verify that excluded methods are not in the report structure
    // (which means they won't be in JSON either)
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var type = assembly.Namespaces.Should().ContainSingle().Subject.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn).Subject;

    // Excluded methods should not be in members list
    type.Members.Should().NotContain(m => m.FullyQualifiedName == constructorFqn);
    type.Members.Should().NotContain(m => m.FullyQualifiedName == moveNextFqn);

    // Normal method should be included
    type.Members.Should().ContainSingle(m => m.FullyQualifiedName == normalMethodFqn);

    // Verify that only the normal method is present
    type.Members.Should().HaveCount(1);
    type.Members[0].FullyQualifiedName.Should().Be(normalMethodFqn);
  }

  [Test]
  public void BuildReport_ExcludedAssemblies_AreNotAddedToSolution()
  {
    // Arrange
    const string includedAssembly = "Rca.Network";
    const string excludedNamespace = "Rca.UI.Tests";
    const string excludedTypeFqn = "Rca.UI.Tests.RcaDockablePanelViewModelTests";

    var assemblyFilter = AssemblyFilter.FromString("Tests");
    var serviceWithFilter = new MetricsAggregationService(new MemberFilter(), assemblyFilter, new TypeFilter());

    var roslynDocument = new ParsedMetricsDocument
    {
      SolutionName = "SampleSolution",
      Elements = new List<ParsedCodeElement>
            {
                new(CodeElementKind.Assembly, includedAssembly, includedAssembly),
                new(CodeElementKind.Namespace, excludedNamespace, excludedNamespace),
                new(CodeElementKind.Type, "RcaDockablePanelViewModelTests", excludedTypeFqn)
                {
                    ParentFullyQualifiedName = excludedNamespace
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = serviceWithFilter.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == includedAssembly).Subject;
    assembly.Namespaces.Should().BeEmpty("namespaces from excluded assemblies must be removed");
  }

  [Test]
  public void BuildReport_SarifViolationsFromExcludedAssembly_DoNotLeakIntoIncludedAssembly()
  {
    // Arrange
    const string includedAssembly = "Rca.Network";
    const string excludedAssembly = "Rca.Integration.Revit.Tests";
    const string namespaceFqn = "<global namespace>";
    const string includedTypeFqn = "<global namespace>.AllowedComponent";
    const string excludedTypeFqn = "<global namespace>.TestLogger";
    const string includedFilePath = @"C:\Repo\src\Rca.Network\AllowedComponent.cs";
    const string excludedFilePath = @"C:\Repo\tests\Rca.Integration.Revit.Tests\TestLogger.cs";

    var assemblyFilter = AssemblyFilter.FromString("Tests");
    var serviceWithFilter = new MetricsAggregationService(new MemberFilter(), assemblyFilter, new TypeFilter());

    var roslynDocument = new ParsedMetricsDocument
    {
      SolutionName = "SampleSolution",
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, includedAssembly, includedAssembly),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = includedAssembly
        },
        new(CodeElementKind.Type, "AllowedComponent", includedTypeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          ContainingAssemblyName = includedAssembly,
          Source = new SourceLocation
          {
            Path = includedFilePath,
            StartLine = 10,
            EndLine = 10
          }
        },
        new(CodeElementKind.Assembly, excludedAssembly, excludedAssembly),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = excludedAssembly
        },
        new(CodeElementKind.Type, "TestLogger", excludedTypeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          ContainingAssemblyName = excludedAssembly,
          Source = new SourceLocation
          {
            Path = excludedFilePath,
            StartLine = 14,
            EndLine = 20
          }
        },
        new(CodeElementKind.Member, "AllowedComponent.Ctor", $"{includedTypeFqn}.Ctor(...)")
        {
          ParentFullyQualifiedName = includedTypeFqn,
          ContainingAssemblyName = includedAssembly,
          Source = new SourceLocation
          {
            Path = includedFilePath,
            StartLine = 10,
            EndLine = 10
          },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        },
        new(CodeElementKind.Member, "TestLogger.Log", $"{excludedTypeFqn}.Log(...)")
        {
          ParentFullyQualifiedName = excludedTypeFqn,
          ContainingAssemblyName = excludedAssembly,
          Source = new SourceLocation
          {
            Path = excludedFilePath,
            StartLine = 18,
            EndLine = 18
          },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        }
      }
    };

    var sarifDocument = new ParsedMetricsDocument
    {
      SolutionName = "SampleSolution",
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Member, "CA1050", null)
        {
          Source = new SourceLocation
          {
            Path = excludedFilePath,
            StartLine = 14,
            EndLine = 14
          },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 1,
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Single("CA1050")
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = serviceWithFilter.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == includedAssembly).Subject;
    assembly.Namespaces.SelectMany(n => n.Types)
        .Should().NotContain(t => t.FullyQualifiedName == excludedTypeFqn, "types from excluded assemblies must not appear under included assemblies");
    if (assembly.Metrics.TryGetValue(MetricIdentifier.SarifCaRuleViolations, out var assemblySarifMetric))
    {
      assemblySarifMetric.Value.Should().BeNull("excluded assemblies must not contribute violation counts to included assemblies");
      assemblySarifMetric.Breakdown.Should().BeNull("excluded assemblies must not contribute rule breakdowns to included assemblies");
    }
  }

  [Test]
  public void BuildReport_IteratorTypeCoverage_IsTransferredToMethodAndTypeIsHidden()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string iteratorTypeFqn = "Sample.Namespace.SampleType+<DoWork>d__1";
    const string memberFqn = typeFqn + ".DoWork(...)";

    var filePath = @"C:\Repo\Sample.cs";

    var altCoverDocument = new ParsedMetricsDocument
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
                    ParentFullyQualifiedName = namespaceFqn
                },
                // Iterator state-machine type with real coverage
                new(CodeElementKind.Type, "Sample.Namespace.SampleType+<DoWork>d__1", iteratorTypeFqn)
                {
                    ParentFullyQualifiedName = assemblyName,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(80, "percent"),
                        [MetricIdentifier.AltCoverBranchCoverage] = Metric(60, "percent")
                    }
                },
                // User method with zero AltCover coverage
                new(CodeElementKind.Member, "DoWork", memberFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 20 },
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(0, "percent"),
                        [MetricIdentifier.AltCoverBranchCoverage] = Metric(0, "percent")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var @namespace = assembly.Namespaces.Should().ContainSingle(n => n.Name == namespaceFqn).Subject;
    var type = @namespace.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn).Subject;

    // Iterator type should be hidden from the type list
    @namespace.Types.Should().NotContain(t => t.FullyQualifiedName == iteratorTypeFqn);

    // Method should receive iterator coverage and be marked accordingly
    var method = type.Members.Should().ContainSingle(m => m.FullyQualifiedName == memberFqn).Subject;
    method.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(80);
    method.Metrics[MetricIdentifier.AltCoverBranchCoverage].Value.Should().Be(60);
    method.IncludesIteratorStateMachineCoverage.Should().BeTrue();
  }

  [Test]
  public void BuildReport_IteratorTypeCoverage_DoesNotTransferBranchCoverage_WhenMethodHasNoBranchMetric()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string iteratorTypeFqn = "Sample.Namespace.SampleType+<DoWork>d__1";
    const string memberFqn = typeFqn + ".DoWork(...)";

    var filePath = @"C:\Repo\Sample.cs";

    var altCoverDocument = new ParsedMetricsDocument
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
                    ParentFullyQualifiedName = namespaceFqn
                },
                // Iterator state-machine type with real coverage
                new(CodeElementKind.Type, "Sample.Namespace.SampleType+<DoWork>d__1", iteratorTypeFqn)
                {
                    ParentFullyQualifiedName = assemblyName,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(80, "percent"),
                        [MetricIdentifier.AltCoverBranchCoverage] = Metric(60, "percent")
                    }
                },
                // User method that has no AltCover branch metric (typical async/iterator pattern)
                new(CodeElementKind.Member, "DoWork", memberFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 20 },
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(0, "percent")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var @namespace = assembly.Namespaces.Should().ContainSingle(n => n.Name == namespaceFqn).Subject;
    var type = @namespace.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn).Subject;

    // Iterator type should be hidden from the type list
    @namespace.Types.Should().NotContain(t => t.FullyQualifiedName == iteratorTypeFqn);

    // Method should receive sequence coverage but not branch coverage from iterator state machine
    var method = type.Members.Should().ContainSingle(m => m.FullyQualifiedName == memberFqn).Subject;
    method.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(80);
    method.Metrics.Should().NotContainKey(MetricIdentifier.AltCoverBranchCoverage);
    method.IncludesIteratorStateMachineCoverage.Should().BeTrue();
  }

  [Test]
  public void BuildReport_TypeBranchCoverageIsRemoved_WhenNoMemberHasBranchCoverage()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.HelperType";

    var altCoverDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "HelperType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.AltCoverSequenceCoverage] = Metric(100, "percent"),
            [MetricIdentifier.AltCoverBranchCoverage] = Metric(0, "percent")
          }
        },
        // Method without branch coverage metric (e.g. helper with linear code only)
        new(CodeElementKind.Member, "DoWork", typeFqn + ".DoWork(...)")
        {
          ParentFullyQualifiedName = typeFqn,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.AltCoverSequenceCoverage] = Metric(100, "percent")
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var @namespace = assembly.Namespaces.Should().ContainSingle(n => n.Name == namespaceFqn).Subject;
    var type = @namespace.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn).Subject;

    type.Metrics.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
    type.Metrics.Should().NotContainKey(MetricIdentifier.AltCoverBranchCoverage,
      "Type-level AltCoverBranchCoverage should be removed when no member has branch coverage.");
  }

  [Test]
  public void BuildReport_TypeBranchCoverageWithPositiveValue_IsKeptWithoutMemberBranchMetrics()
  {
    // Ensures non-zero type-level branch coverage is preserved when members lack branch metrics.
    // Covers the branch where synthetic zero coverage detection returns false.
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.HelperType";

    var altCoverDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "HelperType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.AltCoverSequenceCoverage] = Metric(100, "percent"),
            [MetricIdentifier.AltCoverBranchCoverage] = Metric(50, "percent")
          }
        },
        new(CodeElementKind.Member, "DoWork", typeFqn + ".DoWork(...)")
        {
          ParentFullyQualifiedName = typeFqn,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.AltCoverSequenceCoverage] = Metric(100, "percent")
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var type = report.Solution.Assemblies.Single().Namespaces.Single().Types.Single();
    type.Metrics.Should().ContainKey(MetricIdentifier.AltCoverBranchCoverage);
    type.Metrics[MetricIdentifier.AltCoverBranchCoverage].Value.Should().Be(50);
  }

  [Test]
  public void BuildReport_TypeBranchCoverageWithoutValue_IsRemovedAsSynthetic()
  {
    // Confirms that a type-level branch coverage metric without a value is treated as synthetic noise.
    // Exercises the branch where IsSyntheticZeroCoverage evaluates missing values.
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.HelperType";

    var altCoverDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "HelperType", typeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.AltCoverSequenceCoverage] = Metric(100, "percent"),
            [MetricIdentifier.AltCoverBranchCoverage] = new MetricValue
            {
              Value = null,
              Status = ThresholdStatus.NotApplicable
            }
          }
        },
        new(CodeElementKind.Member, "DoWork", typeFqn + ".DoWork(...)")
        {
          ParentFullyQualifiedName = typeFqn,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.AltCoverSequenceCoverage] = Metric(100, "percent")
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var type = report.Solution.Assemblies.Single().Namespaces.Single().Types.Single();
    type.Metrics.Should().NotContainKey(MetricIdentifier.AltCoverBranchCoverage);
  }

  [Test]
  public void BuildReport_IteratorType_NoMatchingMethod_KeepsTypeUnchanged()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string iteratorTypeFqn = "Sample.Namespace.SampleType+<Missing>d__1";

    var altCoverDocument = new ParsedMetricsDocument
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
                    ParentFullyQualifiedName = namespaceFqn
                },
                new(CodeElementKind.Type, "Sample.Namespace.SampleType+<Missing>d__1", iteratorTypeFqn)
                {
                    ParentFullyQualifiedName = assemblyName,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(50, "percent"),
                        [MetricIdentifier.AltCoverBranchCoverage] = Metric(40, "percent")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var @namespace = assembly.Namespaces.Should().ContainSingle(n => n.Name == namespaceFqn).Subject;

    // Iterator type should remain because no matching method exists
    @namespace.Types.Should().ContainSingle(t => t.FullyQualifiedName == iteratorTypeFqn);
  }

  [Test]
  public void BuildReport_IteratorType_MethodAlreadyHasCoverage_DoesNotOverrideOrHideType()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string iteratorTypeFqn = "Sample.Namespace.SampleType+<DoWork>d__1";
    const string memberFqn = typeFqn + ".DoWork(...)";

    var filePath = @"C:\Repo\Sample.cs";

    var altCoverDocument = new ParsedMetricsDocument
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
                    ParentFullyQualifiedName = namespaceFqn
                },
                // Iterator state-machine type with coverage
                new(CodeElementKind.Type, "Sample.Namespace.SampleType+<DoWork>d__1", iteratorTypeFqn)
                {
                    ParentFullyQualifiedName = assemblyName,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(80, "percent"),
                        [MetricIdentifier.AltCoverBranchCoverage] = Metric(60, "percent")
                    }
                },
                // User method already has non-zero coverage
                new(CodeElementKind.Member, "DoWork", memberFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 20 },
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(10, "percent"),
                        [MetricIdentifier.AltCoverBranchCoverage] = Metric(5, "percent")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var @namespace = assembly.Namespaces.Should().ContainSingle(n => n.Name == namespaceFqn).Subject;
    var type = @namespace.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn).Subject;

    // Iterator type should remain because method already had non-zero coverage
    @namespace.Types.Should().Contain(t => t.FullyQualifiedName == iteratorTypeFqn);

    var method = type.Members.Should().ContainSingle(m => m.FullyQualifiedName == memberFqn).Subject;
    method.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(10);
    method.Metrics[MetricIdentifier.AltCoverBranchCoverage].Value.Should().Be(5);
    method.IncludesIteratorStateMachineCoverage.Should().BeFalse();
  }

  [Test]
  public void BuildReport_PlainNestedPlusTypeCoverage_IsTransferredToDotTypeAndTypeIsHidden()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace.Logging";
    const string plusTypeFqn = "Sample.Namespace.Logging.LoaderLog+LoaderInternalLogger";
    const string dotNamespaceFqn = "Sample.Namespace.Logging.LoaderLog";
    const string dotTypeFqn = "Sample.Namespace.Logging.LoaderLog.LoaderInternalLogger";

    const string plusInnerTypeFqn = "Sample.Namespace.Logging.LoaderLog+LoaderInternalLogger+NullScope";
    const string dotInnerNamespaceFqn = "Sample.Namespace.Logging.LoaderLog.LoaderInternalLogger";
    const string dotInnerTypeFqn = "Sample.Namespace.Logging.LoaderLog.LoaderInternalLogger.NullScope";

    const string filePath = @"C:\Repo\Logging.cs";

    var altCoverDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
            {
                new(CodeElementKind.Assembly, assemblyName, assemblyName),
                new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
                {
                    ParentFullyQualifiedName = assemblyName
                },
                // Dot types without coverage
                new(CodeElementKind.Type, "LoaderLog", dotNamespaceFqn)
                {
                    ParentFullyQualifiedName = namespaceFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>()
                },
                new(CodeElementKind.Type, "LoaderInternalLogger", dotTypeFqn)
                {
                    ParentFullyQualifiedName = dotNamespaceFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>()
                },
                new(CodeElementKind.Type, "NullScope", dotInnerTypeFqn)
                {
                    ParentFullyQualifiedName = dotInnerNamespaceFqn,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>()
                },
                // Plus type with coverage and methods
                new(CodeElementKind.Type, "Sample.Namespace.Logging.LoaderLog+LoaderInternalLogger", plusTypeFqn)
                {
                    ParentFullyQualifiedName = assemblyName,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(75, "percent"),
                        [MetricIdentifier.AltCoverBranchCoverage] = Metric(50, "percent")
                    }
                },
                new(CodeElementKind.Member, "BeginScope", plusTypeFqn + ".BeginScope(...)")
                {
                    ParentFullyQualifiedName = plusTypeFqn,
                    Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 20 },
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(80, "percent")
                    }
                },
                new(CodeElementKind.Member, "IsEnabled", plusTypeFqn + ".IsEnabled(...)")
                {
                    ParentFullyQualifiedName = plusTypeFqn,
                    Source = new SourceLocation { Path = filePath, StartLine = 21, EndLine = 30 },
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(60, "percent")
                    }
                },
                new(CodeElementKind.Member, "Log", plusTypeFqn + ".Log(...)")
                {
                    ParentFullyQualifiedName = plusTypeFqn,
                    Source = new SourceLocation { Path = filePath, StartLine = 31, EndLine = 40 },
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(90, "percent")
                    }
                },
                // Inner plus type without coverage but with a method
                new(CodeElementKind.Type, "Sample.Namespace.Logging.LoaderLog+LoaderInternalLogger+NullScope", plusInnerTypeFqn)
                {
                    ParentFullyQualifiedName = assemblyName,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(0, "percent"),
                        [MetricIdentifier.AltCoverBranchCoverage] = Metric(0, "percent")
                    }
                },
                new(CodeElementKind.Member, "Dispose", plusInnerTypeFqn + ".Dispose(...)")
                {
                    ParentFullyQualifiedName = plusInnerTypeFqn,
                    Source = new SourceLocation { Path = filePath, StartLine = 41, EndLine = 45 },
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(50, "percent")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var rootNamespace = assembly.Namespaces.Should().ContainSingle(n => n.Name == namespaceFqn).Subject;

    // Plus types should be removed
    rootNamespace.Types.Should().NotContain(t => t.FullyQualifiedName == plusTypeFqn);
    rootNamespace.Types.Should().NotContain(t => t.FullyQualifiedName == plusInnerTypeFqn);

    // Dot types should be present
    assembly.Namespaces.Should().NotContain(n => n.Name == dotNamespaceFqn);
    assembly.Namespaces.Should().NotContain(n => n.Name == dotInnerNamespaceFqn);

    rootNamespace.Types.Should().ContainSingle(t => t.FullyQualifiedName == dotNamespaceFqn);
    var loaderInternalLoggerType = rootNamespace.Types.Should().ContainSingle(t => t.FullyQualifiedName == dotTypeFqn).Subject;
    var nullScopeType = rootNamespace.Types.Should().ContainSingle(t => t.FullyQualifiedName == dotInnerTypeFqn).Subject;

    // Type-level coverage transferred
    loaderInternalLoggerType.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(75);
    loaderInternalLoggerType.Metrics[MetricIdentifier.AltCoverBranchCoverage].Value.Should().Be(50);

    // Method-level coverage transferred and flagged
    var beginScope = loaderInternalLoggerType.Members.Should().ContainSingle(m => m.Name == "BeginScope").Subject;
    beginScope.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(80);
    beginScope.IncludesIteratorStateMachineCoverage.Should().BeTrue();

    var isEnabled = loaderInternalLoggerType.Members.Should().ContainSingle(m => m.Name == "IsEnabled").Subject;
    isEnabled.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(60);
    isEnabled.IncludesIteratorStateMachineCoverage.Should().BeTrue();

    var log = loaderInternalLoggerType.Members.Should().ContainSingle(m => m.Name == "Log").Subject;
    log.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(90);
    log.IncludesIteratorStateMachineCoverage.Should().BeTrue();

    var dispose = nullScopeType.Members.Should().ContainSingle(m => m.Name == "Dispose").Subject;
    dispose.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(50);
    dispose.IncludesIteratorStateMachineCoverage.Should().BeTrue();
  }

  [Test]
  public void BuildReport_RoslynNestedType_UsesDeclaredNamespace()
  {
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string outerTypeFqn = "Sample.Namespace.OuterType";
    const string nestedTypeFqn = "Sample.Namespace.OuterType.InnerType";

    var roslynDocument = new ParsedMetricsDocument
    {
      SolutionName = "SampleSolution",
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        new(CodeElementKind.Type, "OuterType", outerTypeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        },
        new(CodeElementKind.Type, "OuterType.InnerType", nestedTypeFqn)
        {
          ParentFullyQualifiedName = namespaceFqn,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    var report = service.BuildReport(input);

    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    assembly.Namespaces.Should().ContainSingle(n => n.Name == namespaceFqn);
    assembly.Namespaces.Should().NotContain(n => n.Name == nestedTypeFqn);

    var namespaceNode = assembly.Namespaces.Single();
    namespaceNode.Types.Should().ContainSingle(t => t.FullyQualifiedName == outerTypeFqn);
    namespaceNode.Types.Should().ContainSingle(t => t.FullyQualifiedName == nestedTypeFqn);
  }

  [Test]
  public void BuildReport_NamespaceIndexBeatsNestedTypeHeuristics_WhenNamespaceContainsDots()
  {
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "MyCompany.Services.Core";
    const string outerTypeFqn = "MyCompany.Services.Core.StructuralElementMerger";
    const string nestedTypeFqn = "MyCompany.Services.Core.StructuralElementMerger.MemberResolutionContext";

    var altCoverDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Namespace, namespaceFqn, namespaceFqn)
        {
          ParentFullyQualifiedName = assemblyName
        },
        // AltCover classes report the assembly as parent, forcing namespace inference to rely on the index.
        new(CodeElementKind.Type, "MyCompany.Services.Core.StructuralElementMerger", outerTypeFqn)
        {
          ParentFullyQualifiedName = assemblyName,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        },
        new(CodeElementKind.Type, "MyCompany.Services.Core.StructuralElementMerger.MemberResolutionContext", nestedTypeFqn)
        {
          ParentFullyQualifiedName = assemblyName,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    var report = service.BuildReport(input);

    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    assembly.Namespaces.Should().ContainSingle(n => n.Name == namespaceFqn);
    assembly.Namespaces.Should().NotContain(n => n.Name == outerTypeFqn);
    assembly.Namespaces.Should().NotContain(n => n.Name == nestedTypeFqn);

    var namespaceNode = assembly.Namespaces.Single();
    namespaceNode.Types.Should().ContainSingle(t => t.FullyQualifiedName == outerTypeFqn);
    namespaceNode.Types.Should().ContainSingle(t => t.FullyQualifiedName == nestedTypeFqn);
  }

  [Test]
  public void BuildReport_MissingNamespaceFallsBackToStringSlicing()
  {
    const string assemblyName = "Sample.Assembly";
    const string inferredNamespace = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.TypeWithoutNamespaceElement";

    var altCoverDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, assemblyName, assemblyName),
        new(CodeElementKind.Type, typeFqn, typeFqn)
        {
          ParentFullyQualifiedName = assemblyName,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    var report = service.BuildReport(input);

    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    assembly.Namespaces.Should().ContainSingle(n => n.Name == inferredNamespace);
    var namespaceNode = assembly.Namespaces.Single();
    namespaceNode.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn);
  }

  [Test]
  public void BuildReport_IteratorTypeAndMethodBothZeroCoverage_HidesIteratorTypeAsNoise()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string iteratorTypeFqn = "Sample.Namespace.SampleType+<DoWork>d__1";
    const string memberFqn = typeFqn + ".DoWork(...)";

    var filePath = @"C:\Repo\Sample.cs";

    var altCoverDocument = new ParsedMetricsDocument
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
                    ParentFullyQualifiedName = namespaceFqn
                },
                // Iterator state-machine type with zero coverage
                new(CodeElementKind.Type, "Sample.Namespace.SampleType+<DoWork>d__1", iteratorTypeFqn)
                {
                    ParentFullyQualifiedName = assemblyName,
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(0, "percent"),
                        [MetricIdentifier.AltCoverBranchCoverage] = Metric(0, "percent")
                    }
                },
                // User method also with zero coverage
                new(CodeElementKind.Member, "DoWork", memberFqn)
                {
                    ParentFullyQualifiedName = typeFqn,
                    Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 20 },
                    Metrics = new Dictionary<MetricIdentifier, MetricValue>
                    {
                        [MetricIdentifier.AltCoverSequenceCoverage] = Metric(0, "percent"),
                        [MetricIdentifier.AltCoverBranchCoverage] = Metric(0, "percent")
                    }
                }
            }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == assemblyName).Subject;
    var @namespace = assembly.Namespaces.Should().ContainSingle(n => n.Name == namespaceFqn).Subject;
    var type = @namespace.Types.Should().ContainSingle(t => t.FullyQualifiedName == typeFqn).Subject;

    // Iterator type should be removed as non-informative noise
    @namespace.Types.Should().NotContain(t => t.FullyQualifiedName == iteratorTypeFqn);

    // Method remains with zero coverage and without iterator flag
    var method = type.Members.Should().ContainSingle(m => m.FullyQualifiedName == memberFqn).Subject;
    method.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(0);
    method.Metrics[MetricIdentifier.AltCoverBranchCoverage].Value.Should().Be(0);
    method.IncludesIteratorStateMachineCoverage.Should().BeFalse();
  }

  [Test]
  public void BuildReport_RemovesMetricsWithoutValueFromFinalReport()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string memberFqn = "Sample.Namespace.SampleType.DoWork(...)";

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
          ParentFullyQualifiedName = namespaceFqn
        },
        new(CodeElementKind.Member, "DoWork", memberFqn)
        {
          ParentFullyQualifiedName = typeFqn,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.RoslynMaintainabilityIndex] = new MetricValue
            {
              Value = null,
              Delta = null,
              Status = ThresholdStatus.NotApplicable
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var member = report.Solution.Assemblies.Single().Namespaces.Single().Types.Single().Members.Single();
    member.Metrics.Should().BeEmpty("metrics without actionable values must be pruned from the report");
  }

  [Test]
  public void BuildReport_SuppressedSymbols_BindsMissingMetricNames()
  {
    // Verifies suppressed symbol entries have their metric inferred from available SARIF metrics.
    // Ensures the branch that binds suppressed symbols executes when entries are provided.
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
          ParentFullyQualifiedName = namespaceFqn
        },
        new(CodeElementKind.Member, "DoWork", memberFqn)
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 20 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = Metric(1, "count")
          }
        }
      }
    };

    var suppressedSymbols = new List<SuppressedSymbolInfo>
    {
      new()
      {
        FullyQualifiedName = memberFqn,
        RuleId = "CA1502"
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths(),
      SuppressedSymbols = suppressedSymbols
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    report.Metadata.SuppressedSymbols.Should().ContainSingle();
    var suppressed = report.Metadata.SuppressedSymbols[0];
    suppressed.RuleId.Should().Be("CA1502");
    suppressed.Metric.Should().Be(MetricIdentifier.SarifCaRuleViolations.ToString());
  }

  [Test]
  public void BuildReport_SarifMetricsForSameMember_AreAggregated()
  {
    // Ensures repeated SARIF metrics for the same member are merged instead of overwritten.
    // Covers the branch where MergeMetric aggregates values and combines breakdown entries.
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
          ParentFullyQualifiedName = namespaceFqn
        },
        new(CodeElementKind.Member, "DoWork", memberFqn)
        {
          ParentFullyQualifiedName = typeFqn,
          Source = new SourceLocation { Path = filePath, StartLine = 10, EndLine = 20 },
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
          Source = new SourceLocation { Path = filePath, StartLine = 12, EndLine = 12 },
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
        new(CodeElementKind.Member, "CA1502", null)
        {
          Source = new SourceLocation { Path = filePath, StartLine = 14, EndLine = 14 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
            {
              Value = 2,
              Status = ThresholdStatus.NotApplicable,
              Breakdown = SarifBreakdownTestHelper.Create(("CA1502", 2))
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var member = report.Solution.Assemblies.Single().Namespaces.Single().Types.Single().Members.Single();
    var sarifMetric = member.Metrics[MetricIdentifier.SarifCaRuleViolations];
    sarifMetric.Value.Should().Be(3);
    sarifMetric.Status.Should().Be(ThresholdStatus.Error);
    var breakdown = sarifMetric.Breakdown!;
    breakdown.Should().NotBeNull();
    breakdown.Should().ContainKey("CA1502");
    breakdown["CA1502"].Count.Should().Be(3);
  }

  [Test]
  public void MergeMetric_AggregateFalse_ReplacesNullExistingMetric()
  {
    // Validates the defensive branch that replaces a null existing value when aggregation is disabled.
    // This branch is unreachable through public APIs but guards against malformed inputs.
    var mergeMetric = typeof(MetricsAggregationService).GetMethod(
      "MergeMetric",
      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    mergeMetric.Should().NotBeNull();

    var node = new MemberMetricsNode
    {
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.AltCoverSequenceCoverage] = new()
        {
          Value = null,
          Status = ThresholdStatus.NotApplicable
        }
      }
    };

    var incoming = new MetricValue
    {
      Value = 5,
      Delta = 1,
      Status = ThresholdStatus.Warning,
      Breakdown = SarifBreakdownTestHelper.Single("CA0001")
    };

    mergeMetric!.Invoke(null, new object[] { node, MetricIdentifier.AltCoverSequenceCoverage, incoming, false });

    var updated = node.Metrics[MetricIdentifier.AltCoverSequenceCoverage];
    updated.Value.Should().Be(5);
    updated.Delta.Should().Be(1);
    updated.Status.Should().Be(ThresholdStatus.Warning);
    updated.Breakdown.Should().NotBeNull();
    updated.Breakdown!.Should().ContainKey("CA0001");

    var nullValueMetric = new MetricValue
    {
      Value = null,
      Status = ThresholdStatus.Error
    };

    mergeMetric.Invoke(null, new object[] { node, MetricIdentifier.AltCoverSequenceCoverage, nullValueMetric, true });

    node.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(5, "null metrics should be ignored when aggregating");
  }

  [Test]
  public void BuildReport_PreservesMetricsWithValueWhenThresholdsAreMissing()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
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
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.RoslynSourceLines] = new MetricValue
            {
              Value = 120,
              Status = ThresholdStatus.NotApplicable
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var type = report.Solution.Assemblies.Single().Namespaces.Single().Types.Single();
    type.Metrics.Should().ContainKey(MetricIdentifier.RoslynSourceLines);
    type.Metrics[MetricIdentifier.RoslynSourceLines].Status.Should().Be(ThresholdStatus.Success, "metrics with values remain even when thresholds are not defined");
    type.Metrics[MetricIdentifier.RoslynSourceLines].Value.Should().Be(120);
  }

  [Test]
  public void BuildReport_PopulatesMetricDescriptorsWithUnits()
  {
    // Arrange
    var roslynDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, "Sample.Assembly", "Sample.Assembly"),
        new(CodeElementKind.Namespace, "Sample.Namespace", "Sample.Namespace")
        {
          ParentFullyQualifiedName = "Sample.Assembly"
        },
        new(CodeElementKind.Type, "Sample.Namespace.SampleType", "Sample.Namespace.SampleType")
        {
          ParentFullyQualifiedName = "Sample.Namespace"
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    report.Metadata.MetricDescriptors.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
    report.Metadata.MetricDescriptors[MetricIdentifier.AltCoverSequenceCoverage].Unit.Should().Be("percent");
    report.Metadata.MetricDescriptors[MetricIdentifier.RoslynMaintainabilityIndex].Unit.Should().Be("score");
  }

  [Test]
  public void BuildReport_WhenValueMatchesBaseline_DeltaIsNull()
  {
    // Arrange
    const string assemblyName = "Sample.Assembly";
    const string namespaceFqn = "Sample.Namespace";
    const string typeFqn = "Sample.Namespace.SampleType";
    const string memberFqn = "Sample.Namespace.SampleType.DoWork(...)";
    const decimal value = 80m;

    var baseline = CreateBaselineReport(assemblyName, namespaceFqn, typeFqn, memberFqn, value);

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
          ParentFullyQualifiedName = namespaceFqn
        },
        new(CodeElementKind.Member, "DoWork", memberFqn)
        {
          ParentFullyQualifiedName = typeFqn,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.RoslynMaintainabilityIndex] = new MetricValue
            {
              Value = value,
              Status = ThresholdStatus.NotApplicable
            }
          }
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "SampleSolution",
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      AltCoverDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = baseline,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    var member = report.Solution.Assemblies.Single().Namespaces.Single().Types.Single().Members.Single();
    member.Metrics[MetricIdentifier.RoslynMaintainabilityIndex].Delta.Should().BeNull("zero delta must be treated as absent");
  }

  private static MetricValue Metric(decimal value, string _)
      => new()
      {
        Value = value,
        Status = ThresholdStatus.NotApplicable
      };

  private static MetricsReport CreateBaselineReport(string assemblyName, string namespaceFqn, string typeFqn, string memberFqn, decimal maintainability)
  {
    var member = new MemberMetricsNode
    {
      Name = "DoWork",
      FullyQualifiedName = memberFqn,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynMaintainabilityIndex] = new MetricValue
        {
          Value = maintainability,
          Status = ThresholdStatus.NotApplicable
        }
      }
    };

    var type = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = typeFqn,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode> { member }
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = namespaceFqn,
      FullyQualifiedName = namespaceFqn,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { type }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = assemblyName,
      FullyQualifiedName = assemblyName,
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

    return new MetricsReport
    {
      Metadata = new ReportMetadata(),
      Solution = solution
    };
  }
}

