namespace MetricsReporter.Tests.Aggregation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using MetricsReporter.Tests.TestHelpers;
using NUnit.Framework;
using ProcessingAssemblyFilter = MetricsReporter.Processing.AssemblyFilter;
using ProcessingMemberFilter = MetricsReporter.Processing.MemberFilter;
using ProcessingTypeFilter = MetricsReporter.Processing.TypeFilter;

/// <summary>
/// Tests the private AggregationWorkspaceWorkflow to ensure its orchestration logic
/// stays covered even though it is normally exercised through MetricsAggregationService.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class MetricsAggregationServiceWorkflowTests
{
  private const string AssemblyName = "Sample.Assembly";
  private const string NamespaceFqn = "Sample.Namespace";
  private const string TypeFqn = "Sample.Namespace.SampleType";
  private const string MemberFqn = "Sample.Namespace.SampleType.DoWork(...)";
  private const string FilePath = @"C:\Repo\Sample.cs";

  // Ensures dependency guards remain defensive so the workflow cannot be constructed in an invalid state.
  [TestCaseSource(nameof(ConstructorNullDependencyCases))]
  public void Constructor_NullDependency_ThrowsArgumentNullException(string parameterName, int argumentIndex)
  {
    // Arrange
    var args = CreateWorkflowConstructorArguments();
    args[argumentIndex] = null!;

    // Act
    Action act = () => CreateWorkflowInstance(args);

    // Assert
    var exception = act.Should().Throw<TargetInvocationException>().Which;
    exception.InnerException.Should().BeOfType<ArgumentNullException>();
    ((ArgumentNullException)exception.InnerException!).ParamName.Should().Be(parameterName);
  }

  // Validates ProcessDocuments flows documents through structural merge, line indexing, and SARIF application.
  [Test]
  public void ProcessDocuments_WithDocuments_BuildsLineIndexAndAppliesSarifMetrics()
  {
    // Arrange
    var workspace = CreateWorkspace();
    var input = CreateMetricsAggregationInputWithCoverage();

    // Act
    InvokeWorkspace(workspace, "ProcessDocuments", input);

    // Assert
    var member = GetWorkspaceSolution(workspace).Assemblies.Single().Namespaces.Single().Types.Single().Members.Single();
    member.Metrics.Should().ContainKey(MetricIdentifier.OpenCoverBranchCoverage);
    member.Metrics.Should().ContainKey(MetricIdentifier.SarifCaRuleViolations);
    member.Metrics[MetricIdentifier.SarifCaRuleViolations].Value.Should().Be(1);
  }

  // Confirms ProcessDocuments safely handles empty input collections without creating stray nodes.
  [Test]
  public void ProcessDocuments_EmptyDocuments_LeavesHierarchyEmpty()
  {
    // Arrange
    var workspace = CreateWorkspace();
    var input = new MetricsAggregationInput
    {
      SolutionName = "WorkflowTestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>(),
      Paths = new ReportPaths()
    };

    // Act
    InvokeWorkspace(workspace, "ProcessDocuments", input);

    // Assert
    var solution = GetWorkspaceSolution(workspace);
    solution.Assemblies.Should().BeEmpty();
    solution.Metrics.Should().NotBeNull();
  }

  // Confirms ProcessDocuments rejects null inputs to keep the workflow resilient against misconfiguration.
  [Test]
  public void ProcessDocuments_NullInput_ThrowsArgumentNullException()
  {
    // Arrange
    var workspace = CreateWorkspace();

    // Act
    Action act = () => InvokeWorkspace(workspace, "ProcessDocuments", new object?[] { null! });

    // Assert
    var exception = act.Should().Throw<TargetInvocationException>().Which;
    exception.InnerException.Should().BeOfType<ArgumentNullException>();
    ((ArgumentNullException)exception.InnerException!).ParamName.Should().Be("input");
  }

  // Ensures PrepareReport triggers reconciliation and threshold application after document processing.
  [Test]
  public void PrepareReport_WithBaselineAndThresholds_ReconcilesAndAppliesThresholds()
  {
    // Arrange
    var workspace = CreateWorkspace();
    var input = CreateMetricsAggregationInputWithCoverage(
        new MetricsReport(),
        new Dictionary<MetricIdentifier, MetricThresholdDefinition>
        {
          [MetricIdentifier.OpenCoverBranchCoverage] = ThresholdTestFactory.CreateDefinition(75, 50, true)
        });

    // Act
    InvokeWorkspace(workspace, "PrepareReport", input);

    // Assert
    var member = GetWorkspaceSolution(workspace).Assemblies.Single().Namespaces.Single().Types.Single().Members.Single();
    member.Metrics[MetricIdentifier.OpenCoverBranchCoverage].Status.Should().NotBe(ThresholdStatus.NotApplicable);
  }

  // Covers direct workflow entry points to ensure non-PrepareReport operations remain functional and counted for coverage.
  [Test]
  public void WorkflowMethods_ExplicitCalls_StayOperationalAfterProcessing()
  {
    // Arrange
    var workspace = CreateWorkspace();
    var input = CreateMetricsAggregationInputWithCoverage();
    InvokeWorkspace(workspace, "ProcessDocuments", input);

    var thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>
    {
      [MetricIdentifier.OpenCoverBranchCoverage] = ThresholdTestFactory.CreateDefinition(60, 40, true)
    };

    var additionalSarif = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Member, "IDE0051", MemberFqn)
        {
          Source = new SourceLocation { Path = FilePath, StartLine = 15, EndLine = 15 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifIdeRuleViolations] = CreateMetric(2, "count")
          }
        }
      }
    };

    // Act
    InvokeWorkspace(workspace, "BuildLineIndex");
    InvokeWorkspace(workspace, "ApplySarifDocument", additionalSarif);
    InvokeWorkspace(workspace, "ApplyBaselineAndThresholds", new MetricsReport(), thresholds);
    InvokeWorkspace(workspace, "ReconcileIteratorStateMachineMetrics");
    InvokeWorkspace(workspace, "ReconcilePlainNestedTypeMetrics");

    var workflow = GetWorkflow(workspace);
    InvokeWorkspace(workflow, "ReconcileTypeBranchCoverageApplicability");

    // Assert
    var member = GetWorkspaceSolution(workspace).Assemblies.Single().Namespaces.Single().Types.Single().Members.Single();
    member.Metrics.Should().ContainKey(MetricIdentifier.SarifIdeRuleViolations);
    member.Metrics[MetricIdentifier.SarifIdeRuleViolations].Value.Should().Be(2);
  }

  private static IEnumerable<TestCaseData> ConstructorNullDependencyCases()
  {
    yield return new TestCaseData("state", 0);
    yield return new TestCaseData("documentProcessor", 1);
    yield return new TestCaseData("lineIndexProcessor", 2);
    yield return new TestCaseData("sarifProcessor", 3);
    yield return new TestCaseData("baselineProcessor", 4);
    yield return new TestCaseData("reconciliationProcessor", 5);
    yield return new TestCaseData("branchCoverageProcessor", 6);
  }

  private static object CreateWorkspace()
  {
    var workspaceType = typeof(MetricsAggregationService).GetNestedType("AggregationWorkspace", BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("AggregationWorkspace type not found.");

    var constructor = workspaceType.GetConstructor(
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
        binder: null,
        types: new[] { typeof(string), typeof(ProcessingMemberFilter), typeof(MemberKindFilter), typeof(ProcessingAssemblyFilter), typeof(ProcessingTypeFilter) },
        modifiers: null)
        ?? throw new InvalidOperationException("AggregationWorkspace constructor not found.");

    return constructor.Invoke(
    [
      "WorkflowTestSolution",
      new ProcessingMemberFilter(),
      new MemberKindFilter(false, false, false, false),
      new ProcessingAssemblyFilter(),
      new ProcessingTypeFilter()
    ])!;
  }

  private static MetricsAggregationInput CreateMetricsAggregationInputWithCoverage(
      MetricsReport? baseline = null,
      IDictionary<MetricIdentifier, MetricThresholdDefinition>? thresholds = null)
  {
    var roslynDocument = new ParsedMetricsDocument
    {
      SolutionName = "WorkflowTestSolution",
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, AssemblyName, AssemblyName),
        new(CodeElementKind.Namespace, NamespaceFqn, NamespaceFqn)
        {
          ParentFullyQualifiedName = AssemblyName
        },
        new(CodeElementKind.Type, "SampleType", TypeFqn)
        {
          ParentFullyQualifiedName = NamespaceFqn
        },
        new(CodeElementKind.Member, "DoWork", MemberFqn)
        {
          ParentFullyQualifiedName = TypeFqn,
          Source = new SourceLocation { Path = FilePath, StartLine = 10, EndLine = 20 }
        }
      }
    };

    var openCoverDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Assembly, AssemblyName, AssemblyName),
        new(CodeElementKind.Type, "Sample.Namespace.SampleType", TypeFqn)
        {
          ParentFullyQualifiedName = AssemblyName,
          Metrics = new Dictionary<MetricIdentifier, MetricValue>()
        },
        new(CodeElementKind.Member, "Sample.Namespace.SampleType::DoWork()", MemberFqn)
        {
          ParentFullyQualifiedName = TypeFqn,
          Source = new SourceLocation { Path = FilePath, StartLine = 10, EndLine = 20 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.OpenCoverBranchCoverage] = CreateMetric(75, "percent")
          }
        }
      }
    };

    var sarifDocument = new ParsedMetricsDocument
    {
      Elements = new List<ParsedCodeElement>
      {
        new(CodeElementKind.Member, "CA1000", MemberFqn)
        {
          Source = new SourceLocation { Path = FilePath, StartLine = 12, EndLine = 12 },
          Metrics = new Dictionary<MetricIdentifier, MetricValue>
          {
            [MetricIdentifier.SarifCaRuleViolations] = CreateMetric(1, "count")
          }
        }
      }
    };

    return new MetricsAggregationInput
    {
      SolutionName = "WorkflowTestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument> { openCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      Thresholds = thresholds ?? new Dictionary<MetricIdentifier, MetricThresholdDefinition>(),
      Paths = new ReportPaths(),
      Baseline = baseline
    };
  }

  private static object[] CreateWorkflowConstructorArguments()
  {
    var state = Activator.CreateInstance(GetNestedType("AggregationWorkspaceState"), "WorkflowTestSolution")!;
    var documentProcessor = Activator.CreateInstance(
        GetNestedType("AggregationDocumentProcessor"),
        state,
        new ProcessingMemberFilter(),
        new MemberKindFilter(false, false, false, false),
        new ProcessingAssemblyFilter(),
        new ProcessingTypeFilter())!;
    var lineIndexProcessor = Activator.CreateInstance(
        GetNestedType("AggregationLineIndexProcessor"),
        state,
        new ProcessingAssemblyFilter(),
        new MemberKindFilter(false, false, false, false))!;
    var sarifProcessor = Activator.CreateInstance(
        GetNestedType("AggregationSarifProcessor"),
        state,
        new ProcessingAssemblyFilter())!;
    var baselineProcessor = Activator.CreateInstance(GetNestedType("AggregationBaselineAndThresholdProcessor"), state)!;
    var reconciliationProcessor = Activator.CreateInstance(GetNestedType("AggregationReconciliationProcessor"), state)!;
    var branchCoverageProcessor = Activator.CreateInstance(GetNestedType("TypeBranchCoverageApplicabilityProcessor"), state)!;

    return
    [
      state,
      documentProcessor,
      lineIndexProcessor,
      sarifProcessor,
      baselineProcessor,
      reconciliationProcessor,
      branchCoverageProcessor
    ];
  }

  private static object CreateWorkflowInstance(object?[] args)
  {
    var workflowType = GetNestedType("AggregationWorkspaceWorkflow");
    var constructor = workflowType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();
    return constructor.Invoke(args)!;
  }

  private static SolutionMetricsNode GetWorkspaceSolution(object workspace)
  {
    var property = workspace.GetType().GetProperty("Solution", BindingFlags.Instance | BindingFlags.Public)
        ?? throw new InvalidOperationException("Solution property not found.");
    return (SolutionMetricsNode)property.GetValue(workspace)!;
  }

  private static object InvokeWorkspace(object workspace, string methodName, params object?[] args)
  {
    var method = workspace.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    method.Should().NotBeNull($"Workspace method {methodName} should exist");
    return method!.Invoke(workspace, args)!;
  }

  private static object GetWorkflow(object workspace)
  {
    var field = workspace.GetType().GetField("_workflow", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Workflow field not found.");
    return field.GetValue(workspace)!;
  }

  private static Type GetNestedType(string name)
  {
    return typeof(MetricsAggregationService).GetNestedType(name, BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"Nested type {name} not found.");
  }

  private static MetricValue CreateMetric(decimal value, string unit)
    => new() { Value = value, Unit = unit };
}


