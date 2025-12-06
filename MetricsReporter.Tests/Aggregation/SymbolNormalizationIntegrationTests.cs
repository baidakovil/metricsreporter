namespace MetricsReporter.Tests.Aggregation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using MetricsReporter.Processing.Parsers;

/// <summary>
/// Integration tests for symbol normalization across different metric sources.
/// </summary>
/// <remarks>
/// These tests verify that symbols from AltCover and Roslyn are properly normalized
/// and merged into a single entry in the final metrics report, even when they have
/// different parameter representations.
/// </remarks>
[TestFixture]
[Category("Unit")]
public sealed class SymbolNormalizationIntegrationTests
{
  private MetricsAggregationService service = null!;

  [SetUp]
  public void SetUp()
  {
    service = new MetricsAggregationService();
  }

  private static Dictionary<MetricIdentifier, MetricThresholdDefinition> CreateEmptyThresholds()
      => new();

  [Test]
  public async Task BuildReport_AltCoverAndRoslynSameMethod_MergesIntoOneEntry()
  {
    // Arrange - Create AltCover document with fully qualified parameter types
    var altCoverXml = CreateAltCoverXml(
        assemblyName: "Rca.Loader",
        className: "Rca.Loader.LoaderApp",
        methodName: "void Rca.Loader.LoaderApp.OnApplicationIdling(System.Object, Autodesk.Revit.UI.Events.IdlingEventArgs)",
        methodCoverage: 50.0m);

    var altCoverFile = CreateTempFile(altCoverXml);
    var altCoverParser = new AltCoverMetricsParser();
    var altCoverDocument = await altCoverParser.ParseAsync(altCoverFile, CancellationToken.None);

    // Create Roslyn document with short parameter types and nullable annotations
    var roslynXml = CreateRoslynXml(
        assemblyName: "Rca.Loader, Version=1.0.0.0",
        namespaceName: "Rca.Loader",
        typeName: "LoaderApp",
        memberName: "void OnApplicationIdling(object? sender, IdlingEventArgs e)",
        maintainabilityIndex: 80);

    var roslynFile = CreateTempFile(roslynXml);
    var roslynParser = new RoslynMetricsParser();
    var roslynDocument = await roslynParser.ParseAsync(roslynFile, CancellationToken.None);

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = CreateEmptyThresholds(),
      Paths = new ReportPaths
      {
        MetricsDirectory = @"C:\Test\Metrics",
        Baseline = @"C:\Test\Metrics\baseline.json",
        Report = @"C:\Test\Metrics\report.json",
        Html = @"C:\Test\Metrics\report.html"
      }
    };

    try
    {
      // Act
      var report = service.BuildReport(input);

      // Assert - Should have only one member entry for OnApplicationIdling
      var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == "Rca.Loader").Subject;
      var namespaceNode = assembly.Namespaces.Should().ContainSingle(n => n.Name == "Rca.Loader").Subject;
      var typeNode = namespaceNode.Types.Should().ContainSingle(t => t.Name == "LoaderApp").Subject;

      // Critical assertion: Should have only ONE member, not two
      var members = typeNode.Members.Where(m => m.Name == "OnApplicationIdling").ToList();
      members.Should().HaveCount(1, because: "AltCover and Roslyn methods should be merged into one entry");

      var member = members[0];
      member.FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp.OnApplicationIdling(...)");

      // Both metrics should be present
      member.Metrics.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
      member.Metrics[MetricIdentifier.AltCoverSequenceCoverage].Value.Should().Be(50.0m);

      member.Metrics.Should().ContainKey(MetricIdentifier.RoslynMaintainabilityIndex);
      member.Metrics[MetricIdentifier.RoslynMaintainabilityIndex].Value.Should().Be(80m);
    }
    finally
    {
      File.Delete(altCoverFile);
      File.Delete(roslynFile);
    }
  }

  [Test]
  public async Task BuildReport_PropertyGetterFromBothSources_MergesIntoOneEntry()
  {
    // Arrange
    var altCoverXml = CreateAltCoverXml(
        assemblyName: "Rca.Network",
        className: "Rca.Network.NetworkPlaceholder",
        methodName: "System.Boolean get_IsReady()",
        methodCoverage: 100.0m);

    var altCoverFile = CreateTempFile(altCoverXml);
    var altCoverParser = new AltCoverMetricsParser();
    var altCoverDocument = await altCoverParser.ParseAsync(altCoverFile, CancellationToken.None);

    var roslynXml = CreateRoslynXml(
        assemblyName: "Rca.Network, Version=1.0.0.0",
        namespaceName: "Rca.Network",
        typeName: "NetworkPlaceholder",
        memberName: "System.Boolean get_IsReady()",
        maintainabilityIndex: 95);

    var roslynFile = CreateTempFile(roslynXml);
    var roslynParser = new RoslynMetricsParser();
    var roslynDocument = await roslynParser.ParseAsync(roslynFile, CancellationToken.None);

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = CreateEmptyThresholds(),
      Paths = new ReportPaths
      {
        MetricsDirectory = @"C:\Test\Metrics",
        Baseline = @"C:\Test\Metrics\baseline.json",
        Report = @"C:\Test\Metrics\report.json",
        Html = @"C:\Test\Metrics\report.html"
      }
    };

    try
    {
      // Act
      var report = service.BuildReport(input);

      // Assert
      var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == "Rca.Network").Subject;
      var namespaceNode = assembly.Namespaces.Should().ContainSingle(n => n.Name == "Rca.Network").Subject;
      var typeNode = namespaceNode.Types.Should().ContainSingle(t => t.Name == "NetworkPlaceholder").Subject;

      var members = typeNode.Members.Where(m => m.Name == "get_IsReady").ToList();
      members.Should().HaveCount(1, because: "Property getter from both sources should be merged");

      var member = members[0];
      member.FullyQualifiedName.Should().Be("Rca.Network.NetworkPlaceholder.get_IsReady(...)");
      member.Metrics.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
      member.Metrics.Should().ContainKey(MetricIdentifier.RoslynMaintainabilityIndex);
    }
    finally
    {
      File.Delete(altCoverFile);
      File.Delete(roslynFile);
    }
  }

  [Test]
  public async Task BuildReport_SimpleMethodWithDifferentParameterFormats_MergesCorrectly()
  {
    // Arrange
    var altCoverXml = CreateAltCoverXml(
        assemblyName: "Test.Assembly",
        className: "Test.Assembly.TestClass",
        methodName: "void Method(System.Object, System.String)",
        methodCoverage: 75.0m);

    var altCoverFile = CreateTempFile(altCoverXml);
    var altCoverParser = new AltCoverMetricsParser();
    var altCoverDocument = await altCoverParser.ParseAsync(altCoverFile, CancellationToken.None);

    var roslynXml = CreateRoslynXml(
        assemblyName: "Test.Assembly, Version=1.0.0.0",
        namespaceName: "Test.Assembly",
        typeName: "TestClass",
        memberName: "void Method(object sender, string name)",
        maintainabilityIndex: 80);

    var roslynFile = CreateTempFile(roslynXml);
    var roslynParser = new RoslynMetricsParser();
    var roslynDocument = await roslynParser.ParseAsync(roslynFile, CancellationToken.None);

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = CreateEmptyThresholds(),
      Paths = new ReportPaths
      {
        MetricsDirectory = @"C:\Test\Metrics",
        Baseline = @"C:\Test\Metrics\baseline.json",
        Report = @"C:\Test\Metrics\report.json",
        Html = @"C:\Test\Metrics\report.html"
      }
    };

    try
    {
      // Act
      var report = service.BuildReport(input);

      // Assert
      var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == "Test.Assembly").Subject;
      var namespaceNode = assembly.Namespaces.Should().ContainSingle(n => n.Name == "Test.Assembly").Subject;
      var typeNode = namespaceNode.Types.Should().ContainSingle(t => t.Name == "TestClass").Subject;

      var members = typeNode.Members.Where(m => m.Name == "Method").ToList();
      members.Should().HaveCount(1, because: "Method from both sources should be merged");

      var member = members[0];
      member.FullyQualifiedName.Should().Be("Test.Assembly.TestClass.Method(...)");
      member.Metrics.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
      member.Metrics.Should().ContainKey(MetricIdentifier.RoslynMaintainabilityIndex);
    }
    finally
    {
      File.Delete(altCoverFile);
      File.Delete(roslynFile);
    }
  }

  [Test]
  public async Task BuildReport_MethodWithGenericParameters_MergesCorrectly()
  {
    // Arrange
    var altCoverXml = CreateAltCoverXml(
        assemblyName: "Test.Assembly",
        className: "Test.Assembly.TestClass",
        methodName: "void Process(System.Collections.Generic.List<System.String>)",
        methodCoverage: 75.0m);

    var altCoverFile = CreateTempFile(altCoverXml);
    var altCoverParser = new AltCoverMetricsParser();
    var altCoverDocument = await altCoverParser.ParseAsync(altCoverFile, CancellationToken.None);

    var roslynXml = CreateRoslynXml(
        assemblyName: "Test.Assembly, Version=1.0.0.0",
        namespaceName: "Test.Assembly",
        typeName: "TestClass",
        memberName: "void Process(List<string> items)",
        maintainabilityIndex: 80);

    var roslynFile = CreateTempFile(roslynXml);
    var roslynParser = new RoslynMetricsParser();
    var roslynDocument = await roslynParser.ParseAsync(roslynFile, CancellationToken.None);

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = CreateEmptyThresholds(),
      Paths = new ReportPaths
      {
        MetricsDirectory = @"C:\Test\Metrics",
        Baseline = @"C:\Test\Metrics\baseline.json",
        Report = @"C:\Test\Metrics\report.json",
        Html = @"C:\Test\Metrics\report.html"
      }
    };

    try
    {
      // Act
      var report = service.BuildReport(input);

      // Assert
      var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == "Test.Assembly").Subject;
      var namespaceNode = assembly.Namespaces.Should().ContainSingle(n => n.Name == "Test.Assembly").Subject;
      var typeNode = namespaceNode.Types.Should().ContainSingle(t => t.Name == "TestClass").Subject;

      var members = typeNode.Members.Where(m => m.Name == "Process").ToList();
      members.Should().HaveCount(1, because: "Method with generic parameters from both sources should be merged");

      var member = members[0];
      member.FullyQualifiedName.Should().Be("Test.Assembly.TestClass.Process(...)");
      member.Metrics.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
      member.Metrics.Should().ContainKey(MetricIdentifier.RoslynMaintainabilityIndex);
    }
    finally
    {
      File.Delete(altCoverFile);
      File.Delete(roslynFile);
    }
  }

  [Test]
  public async Task BuildReport_MethodWithReturnType_MergesCorrectly()
  {
    // Arrange
    var altCoverXml = CreateAltCoverXml(
        assemblyName: "Test.Assembly",
        className: "Test.Assembly.TestClass",
        methodName: "System.String GetValue(System.Int32)",
        methodCoverage: 75.0m);

    var altCoverFile = CreateTempFile(altCoverXml);
    var altCoverParser = new AltCoverMetricsParser();
    var altCoverDocument = await altCoverParser.ParseAsync(altCoverFile, CancellationToken.None);

    var roslynXml = CreateRoslynXml(
        assemblyName: "Test.Assembly, Version=1.0.0.0",
        namespaceName: "Test.Assembly",
        typeName: "TestClass",
        memberName: "string GetValue(int id)",
        maintainabilityIndex: 80);

    var roslynFile = CreateTempFile(roslynXml);
    var roslynParser = new RoslynMetricsParser();
    var roslynDocument = await roslynParser.ParseAsync(roslynFile, CancellationToken.None);

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = CreateEmptyThresholds(),
      Paths = new ReportPaths
      {
        MetricsDirectory = @"C:\Test\Metrics",
        Baseline = @"C:\Test\Metrics\baseline.json",
        Report = @"C:\Test\Metrics\report.json",
        Html = @"C:\Test\Metrics\report.html"
      }
    };

    try
    {
      // Act
      var report = service.BuildReport(input);

      // Assert
      var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == "Test.Assembly").Subject;
      var namespaceNode = assembly.Namespaces.Should().ContainSingle(n => n.Name == "Test.Assembly").Subject;
      var typeNode = namespaceNode.Types.Should().ContainSingle(t => t.Name == "TestClass").Subject;

      var members = typeNode.Members.Where(m => m.Name == "GetValue").ToList();
      members.Should().HaveCount(1, because: "Method with return type from both sources should be merged");

      var member = members[0];
      member.FullyQualifiedName.Should().Be("Test.Assembly.TestClass.GetValue(...)");
      member.Metrics.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
      member.Metrics.Should().ContainKey(MetricIdentifier.RoslynMaintainabilityIndex);
    }
    finally
    {
      File.Delete(altCoverFile);
      File.Delete(roslynFile);
    }
  }

  [Test]
  public async Task BuildReport_GenericMethodRegisterFromBothSources_MergesIntoOneEntry()
  {
    // Arrange - AltCover format with generic type parameter
    var altCoverXml = CreateAltCoverXml(
        assemblyName: "Rca.Loader.Contracts",
        className: "Rca.Loader.Contracts.SharedServiceRegistry",
        methodName: "System.Void Rca.Loader.Contracts.SharedServiceRegistry::Register(TInterface)",
        methodCoverage: 87.5m);

    var altCoverFile = CreateTempFile(altCoverXml);
    var altCoverParser = new AltCoverMetricsParser();
    var altCoverDocument = await altCoverParser.ParseAsync(altCoverFile, CancellationToken.None);

    // Roslyn format with generic method signature
    var roslynXml = CreateRoslynXml(
        assemblyName: "Rca.Loader.Contracts, Version=1.0.0.0",
        namespaceName: "Rca.Loader.Contracts",
        typeName: "SharedServiceRegistry",
        memberName: "void SharedServiceRegistry.Register<TInterface>(TInterface implementation)",
        maintainabilityIndex: 95);

    var roslynFile = CreateTempFile(roslynXml);
    var roslynParser = new RoslynMetricsParser();
    var roslynDocument = await roslynParser.ParseAsync(roslynFile, CancellationToken.None);

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = CreateEmptyThresholds(),
      Paths = new ReportPaths
      {
        MetricsDirectory = @"C:\Test\Metrics",
        Baseline = @"C:\Test\Metrics\baseline.json",
        Report = @"C:\Test\Metrics\report.json",
        Html = @"C:\Test\Metrics\report.html"
      }
    };

    try
    {
      // Act
      var report = service.BuildReport(input);

      // Assert
      var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == "Rca.Loader.Contracts").Subject;
      var namespaceNode = assembly.Namespaces.Should().ContainSingle(n => n.Name == "Rca.Loader.Contracts").Subject;
      var typeNode = namespaceNode.Types.Should().ContainSingle(t => t.Name == "SharedServiceRegistry").Subject;

      var members = typeNode.Members.Where(m => m.Name == "Register").ToList();
      members.Should().HaveCount(1, because: "Generic method Register from both sources should be merged");

      var member = members[0];
      member.FullyQualifiedName.Should().Be("Rca.Loader.Contracts.SharedServiceRegistry.Register(...)");
      member.Metrics.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
      member.Metrics.Should().ContainKey(MetricIdentifier.RoslynMaintainabilityIndex);
    }
    finally
    {
      File.Delete(altCoverFile);
      File.Delete(roslynFile);
    }
  }

  [Test]
  public async Task BuildReport_GenericMethodResolveFromBothSources_MergesIntoOneEntry()
  {
    // Arrange - AltCover format
    var altCoverXml = CreateAltCoverXml(
        assemblyName: "Rca.Loader.Contracts",
        className: "Rca.Loader.Contracts.SharedServiceRegistry",
        methodName: "TInterface Rca.Loader.Contracts.SharedServiceRegistry::Resolve()",
        methodCoverage: 87.5m);

    var altCoverFile = CreateTempFile(altCoverXml);
    var altCoverParser = new AltCoverMetricsParser();
    var altCoverDocument = await altCoverParser.ParseAsync(altCoverFile, CancellationToken.None);

    // Roslyn format
    var roslynXml = CreateRoslynXml(
        assemblyName: "Rca.Loader.Contracts, Version=1.0.0.0",
        namespaceName: "Rca.Loader.Contracts",
        typeName: "SharedServiceRegistry",
        memberName: "TInterface SharedServiceRegistry.Resolve<TInterface>()",
        maintainabilityIndex: 95);

    var roslynFile = CreateTempFile(roslynXml);
    var roslynParser = new RoslynMetricsParser();
    var roslynDocument = await roslynParser.ParseAsync(roslynFile, CancellationToken.None);

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = CreateEmptyThresholds(),
      Paths = new ReportPaths
      {
        MetricsDirectory = @"C:\Test\Metrics",
        Baseline = @"C:\Test\Metrics\baseline.json",
        Report = @"C:\Test\Metrics\report.json",
        Html = @"C:\Test\Metrics\report.html"
      }
    };

    try
    {
      // Act
      var report = service.BuildReport(input);

      // Assert
      var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == "Rca.Loader.Contracts").Subject;
      var namespaceNode = assembly.Namespaces.Should().ContainSingle(n => n.Name == "Rca.Loader.Contracts").Subject;
      var typeNode = namespaceNode.Types.Should().ContainSingle(t => t.Name == "SharedServiceRegistry").Subject;

      var members = typeNode.Members.Where(m => m.Name == "Resolve").ToList();
      members.Should().HaveCount(1, because: "Generic method Resolve from both sources should be merged");

      var member = members[0];
      member.FullyQualifiedName.Should().Be("Rca.Loader.Contracts.SharedServiceRegistry.Resolve(...)");
      member.Metrics.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
      member.Metrics.Should().ContainKey(MetricIdentifier.RoslynMaintainabilityIndex);
    }
    finally
    {
      File.Delete(altCoverFile);
      File.Delete(roslynFile);
    }
  }

  [Test]
  public async Task BuildReport_ToStringMethodFromBothSources_MergesIntoOneEntry()
  {
    // Arrange - AltCover format
    var altCoverXml = CreateAltCoverXml(
        assemblyName: "Rca.Logging.Contracts",
        className: "Rca.Logging.Contracts.LogEntryDto",
        methodName: "System.String Rca.Logging.Contracts.LogEntryDto::ToString()",
        methodCoverage: 0.0m);

    var altCoverFile = CreateTempFile(altCoverXml);
    var altCoverParser = new AltCoverMetricsParser();
    var altCoverDocument = await altCoverParser.ParseAsync(altCoverFile, CancellationToken.None);

    // Roslyn format (typically not in Roslyn metrics, but test for completeness)
    var roslynXml = CreateRoslynXml(
        assemblyName: "Rca.Logging.Contracts, Version=1.0.0.0",
        namespaceName: "Rca.Logging.Contracts",
        typeName: "LogEntryDto",
        memberName: "string LogEntryDto.ToString()",
        maintainabilityIndex: 100);

    var roslynFile = CreateTempFile(roslynXml);
    var roslynParser = new RoslynMetricsParser();
    var roslynDocument = await roslynParser.ParseAsync(roslynFile, CancellationToken.None);

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = CreateEmptyThresholds(),
      Paths = new ReportPaths
      {
        MetricsDirectory = @"C:\Test\Metrics",
        Baseline = @"C:\Test\Metrics\baseline.json",
        Report = @"C:\Test\Metrics\report.json",
        Html = @"C:\Test\Metrics\report.html"
      }
    };

    try
    {
      // Act
      var report = service.BuildReport(input);

      // Assert
      var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == "Rca.Logging.Contracts").Subject;
      var namespaceNode = assembly.Namespaces.Should().ContainSingle(n => n.Name == "Rca.Logging.Contracts").Subject;
      var typeNode = namespaceNode.Types.Should().ContainSingle(t => t.Name == "LogEntryDto").Subject;

      var members = typeNode.Members.Where(m => m.Name == "ToString").ToList();
      members.Should().HaveCount(1, because: "ToString method from both sources should be merged");

      var member = members[0];
      member.FullyQualifiedName.Should().Be("Rca.Logging.Contracts.LogEntryDto.ToString(...)");
      member.Metrics.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
      member.Metrics.Should().ContainKey(MetricIdentifier.RoslynMaintainabilityIndex);
    }
    finally
    {
      File.Delete(altCoverFile);
      File.Delete(roslynFile);
    }
  }

  [Test]
  public async Task BuildReport_EqualsMethodFromBothSources_MergesIntoOneEntry()
  {
    // Arrange - AltCover format
    var altCoverXml = CreateAltCoverXml(
        assemblyName: "Rca.Logging.Contracts",
        className: "Rca.Logging.Contracts.LogEntryDto",
        methodName: "System.Boolean Rca.Logging.Contracts.LogEntryDto::Equals(Rca.Logging.Contracts.LogEntryDto)",
        methodCoverage: 100.0m);

    var altCoverFile = CreateTempFile(altCoverXml);
    var altCoverParser = new AltCoverMetricsParser();
    var altCoverDocument = await altCoverParser.ParseAsync(altCoverFile, CancellationToken.None);

    // Roslyn format
    var roslynXml = CreateRoslynXml(
        assemblyName: "Rca.Logging.Contracts, Version=1.0.0.0",
        namespaceName: "Rca.Logging.Contracts",
        typeName: "LogEntryDto",
        memberName: "bool LogEntryDto.Equals(LogEntryDto other)",
        maintainabilityIndex: 95);

    var roslynFile = CreateTempFile(roslynXml);
    var roslynParser = new RoslynMetricsParser();
    var roslynDocument = await roslynParser.ParseAsync(roslynFile, CancellationToken.None);

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      AltCoverDocuments = new List<ParsedMetricsDocument> { altCoverDocument },
      RoslynDocuments = new List<ParsedMetricsDocument> { roslynDocument },
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = CreateEmptyThresholds(),
      Paths = new ReportPaths
      {
        MetricsDirectory = @"C:\Test\Metrics",
        Baseline = @"C:\Test\Metrics\baseline.json",
        Report = @"C:\Test\Metrics\report.json",
        Html = @"C:\Test\Metrics\report.html"
      }
    };

    try
    {
      // Act
      var report = service.BuildReport(input);

      // Assert
      var assembly = report.Solution.Assemblies.Should().ContainSingle(a => a.Name == "Rca.Logging.Contracts").Subject;
      var namespaceNode = assembly.Namespaces.Should().ContainSingle(n => n.Name == "Rca.Logging.Contracts").Subject;
      var typeNode = namespaceNode.Types.Should().ContainSingle(t => t.Name == "LogEntryDto").Subject;

      var members = typeNode.Members.Where(m => m.Name == "Equals").ToList();
      members.Should().HaveCount(1, because: "Equals method from both sources should be merged");

      var member = members[0];
      member.FullyQualifiedName.Should().Be("Rca.Logging.Contracts.LogEntryDto.Equals(...)");
      member.Metrics.Should().ContainKey(MetricIdentifier.AltCoverSequenceCoverage);
      member.Metrics.Should().ContainKey(MetricIdentifier.RoslynMaintainabilityIndex);
    }
    finally
    {
      File.Delete(altCoverFile);
      File.Delete(roslynFile);
    }
  }

  private static string CreateAltCoverXml(
      string assemblyName,
      string className,
      string methodName,
      decimal methodCoverage)
  {
    // Escape XML special characters in methodName (especially < and > for generic types)
    var escapedMethodName = methodName
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal)
        .Replace("'", "&apos;", StringComparison.Ordinal);

    return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<CoverageSession>
  <Modules>
    <Module>
      <ModuleName>{assemblyName}</ModuleName>
      <Summary sequenceCoverage=""{methodCoverage}"" branchCoverage=""{methodCoverage}"" />
      <Classes>
        <Class>
          <FullName>{className}</FullName>
          <Summary sequenceCoverage=""{methodCoverage}"" branchCoverage=""{methodCoverage}"" />
          <Methods>
            <Method sequenceCoverage=""{methodCoverage}"" branchCoverage=""{methodCoverage}"" cyclomaticComplexity=""1"" nPathComplexity=""1"">
              <Name>{escapedMethodName}</Name>
            </Method>
          </Methods>
        </Class>
      </Classes>
    </Module>
  </Modules>
</CoverageSession>";
  }

  private static string CreateRoslynXml(
      string assemblyName,
      string namespaceName,
      string typeName,
      string memberName,
      int maintainabilityIndex)
  {
    // Escape XML special characters in memberName (especially < and > for generic types)
    var escapedMemberName = memberName
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal)
        .Replace("'", "&apos;", StringComparison.Ordinal);

    return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<CodeMetricsReport>
  <Targets>
    <Target Name=""Solution"">
      <Assembly Name=""{assemblyName}"">
        <Metrics>
          <Metric Name=""MaintainabilityIndex"" Value=""{maintainabilityIndex}"" />
        </Metrics>
        <Namespaces>
          <Namespace Name=""{namespaceName}"">
            <Metrics>
              <Metric Name=""MaintainabilityIndex"" Value=""{maintainabilityIndex}"" />
            </Metrics>
            <Types>
              <Type Name=""{typeName}"" File=""{typeName}.cs"" Line=""10"">
                <Metrics>
                  <Metric Name=""MaintainabilityIndex"" Value=""{maintainabilityIndex}"" />
                </Metrics>
                <Members>
                  <Member Name=""{escapedMemberName}"" File=""{typeName}.cs"" Line=""20"">
                    <Metrics>
                      <Metric Name=""MaintainabilityIndex"" Value=""{maintainabilityIndex}"" />
                    </Metrics>
                  </Member>
                </Members>
              </Type>
            </Types>
          </Namespace>
        </Namespaces>
      </Assembly>
    </Target>
  </Targets>
</CodeMetricsReport>";
  }

  private static string CreateTempFile(string content)
  {
    var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xml");
    File.WriteAllText(tempFile, content);
    return tempFile;
  }
}


