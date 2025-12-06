namespace MetricsReporter.Tests.Aggregation;

using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using MetricsReporter.Processing.Parsers;
using MetricsReporter.Tests.TestHelpers;

/// <summary>
/// Unit tests for <see cref="SarifMetricsApplier"/> focusing on symbol mapping accuracy.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class SarifMetricsApplierSymbolMappingTests
{
  private LineIndex _lineIndex = null!;
  private AssemblyFilter _assemblyFilter = null!;
  private SarifMetricsApplier _applier = null!;
  private SolutionMetricsNode _solution = null!;
  private List<MetricsNode> _mergeTargets = null!;

  [SetUp]
  public void SetUp()
  {
    _lineIndex = new LineIndex();
    _assemblyFilter = new AssemblyFilter([]);
    _mergeTargets = [];
    _applier = new SarifMetricsApplier(_lineIndex, _assemblyFilter, (node, id, value) => _mergeTargets.Add(node));
    _solution = new SolutionMetricsNode
    {
      Name = "TestSolution",
      FullyQualifiedName = "TestSolution",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };
  }

  [Test]
  public void Apply_MetricOnMemberLine_MapsToMemberNotType()
  {
    // Arrange
    const string filePath = @"C:\Repo\Test.cs";
    const string normalizedPath = @"C:\Repo\Test.cs";
    var assembly = CreateAssembly("TestAssembly");
    var type = CreateType(assembly, "TestType", filePath, startLine: 10, endLine: 50);
    var member = CreateMember(type, "TestMethod", filePath, startLine: 20, endLine: 30);

    _lineIndex.AddMember(normalizedPath, member, 20, 30);
    _lineIndex.AddType(normalizedPath, type, 10, 50);
    _lineIndex.RegisterFileAssembly(normalizedPath, assembly);
    _solution.Assemblies.Add(assembly);

    var sarifDocument = CreateSarifDocument(filePath, startLine: 25);

    // Act
    _applier.Apply(sarifDocument, _solution);

    // Assert
    _mergeTargets.Should().HaveCount(1);
    _mergeTargets[0].Should().Be(member, because: "Metric on line 25 should map to member (lines 20-30), not type");
  }

  [Test]
  public void Apply_MetricOnTypeOnlyLine_MapsToType()
  {
    // Arrange
    const string filePath = @"C:\Repo\Test.cs";
    const string normalizedPath = @"C:\Repo\Test.cs";
    var assembly = CreateAssembly("TestAssembly");
    var type = CreateType(assembly, "TestType", filePath, startLine: 10, endLine: 50);
    var member = CreateMember(type, "TestMethod", filePath, startLine: 20, endLine: 30);

    _lineIndex.AddMember(normalizedPath, member, 20, 30);
    _lineIndex.AddType(normalizedPath, type, 10, 50);
    _lineIndex.RegisterFileAssembly(normalizedPath, assembly);
    _solution.Assemblies.Add(assembly);

    var sarifDocument = CreateSarifDocument(filePath, startLine: 15);

    // Act
    _applier.Apply(sarifDocument, _solution);

    // Assert
    _mergeTargets.Should().HaveCount(1);
    _mergeTargets[0].Should().Be(type, because: "Metric on line 15 should map to type (no member on that line)");
  }

  [Test]
  public void Apply_MetricOnMethodStartLine_MapsToMethod()
  {
    // Arrange - Method starts at line 159 (same as in the real example)
    const string filePath = @"C:\Repo\ReloadRuntimeCommand.cs";
    const string normalizedPath = @"C:\Repo\ReloadRuntimeCommand.cs";
    var assembly = CreateAssembly("Rca.Loader");
    var type = CreateType(assembly, "ReloadRuntimeCommand", filePath, startLine: 16, endLine: 211);
    var member = CreateMember(type, "TryReplaceDockableContent", filePath, startLine: 159, endLine: 209);

    _lineIndex.AddMember(normalizedPath, member, 159, 209);
    _lineIndex.AddType(normalizedPath, type, 16, 211);
    _lineIndex.RegisterFileAssembly(normalizedPath, assembly);
    _solution.Assemblies.Add(assembly);

    var sarifDocument = CreateSarifDocument(filePath, startLine: 159);

    // Act
    _applier.Apply(sarifDocument, _solution);

    // Assert
    _mergeTargets.Should().HaveCount(1);
    _mergeTargets[0].Should().Be(member, because: "Metric on method start line should map to method, not type");
  }

  [Test]
  public void Apply_MemberNotInIndex_FallsBackToType()
  {
    // Arrange - Member not added to index, but type is
    const string filePath = @"C:\Repo\Test.cs";
    const string normalizedPath = @"C:\Repo\Test.cs";
    var assembly = CreateAssembly("TestAssembly");
    var type = CreateType(assembly, "TestType", filePath, startLine: 10, endLine: 50);

    _lineIndex.AddType(normalizedPath, type, 10, 50);
    _lineIndex.RegisterFileAssembly(normalizedPath, assembly);
    _solution.Assemblies.Add(assembly);

    var sarifDocument = CreateSarifDocument(filePath, startLine: 25);

    // Act
    _applier.Apply(sarifDocument, _solution);

    // Assert
    _mergeTargets.Should().HaveCount(1);
    _mergeTargets[0].Should().Be(type, because: "When member is not in index, should fallback to type");
  }

  [Test]
  public void Apply_MultipleMembersOnSameLine_MapsToShortestMember()
  {
    // Arrange - Two methods on same line (edge case)
    const string filePath = @"C:\Repo\Test.cs";
    const string normalizedPath = @"C:\Repo\Test.cs";
    var assembly = CreateAssembly("TestAssembly");
    var type = CreateType(assembly, "TestType", filePath, startLine: 10, endLine: 50);
    var member1 = CreateMember(type, "ShortMethod", filePath, startLine: 20, endLine: 22);
    var member2 = CreateMember(type, "LongMethod", filePath, startLine: 20, endLine: 30);

    _lineIndex.AddMember(normalizedPath, member1, 20, 22);
    _lineIndex.AddMember(normalizedPath, member2, 20, 30);
    _lineIndex.AddType(normalizedPath, type, 10, 50);
    _lineIndex.RegisterFileAssembly(normalizedPath, assembly);
    _solution.Assemblies.Add(assembly);

    var sarifDocument = CreateSarifDocument(filePath, startLine: 21);

    // Act
    _applier.Apply(sarifDocument, _solution);

    // Assert
    _mergeTargets.Should().HaveCount(1);
    _mergeTargets[0].Should().Be(member1, because: "Should prefer shortest member containing the line");
  }

  [Test]
  public void Apply_MemberOutsideIndexedRange_MapsToType()
  {
    // Arrange - Metric on line 5, but member is at lines 20-30
    const string filePath = @"C:\Repo\Test.cs";
    const string normalizedPath = @"C:\Repo\Test.cs";
    var assembly = CreateAssembly("TestAssembly");
    var type = CreateType(assembly, "TestType", filePath, startLine: 1, endLine: 50);
    var member = CreateMember(type, "TestMethod", filePath, startLine: 20, endLine: 30);

    _lineIndex.AddMember(normalizedPath, member, 20, 30);
    _lineIndex.AddType(normalizedPath, type, 1, 50);
    _lineIndex.RegisterFileAssembly(normalizedPath, assembly);
    _solution.Assemblies.Add(assembly);

    var sarifDocument = CreateSarifDocument(filePath, startLine: 5);

    // Act
    _applier.Apply(sarifDocument, _solution);

    // Assert
    _mergeTargets.Should().HaveCount(1);
    _mergeTargets[0].Should().Be(type, because: "Metric outside member range should map to type");
  }

  [Test]
  public void Apply_MetricOnMethodDeclarationLine_MethodIndexedAtBodyStart_MapsToMethod()
  {
    // Arrange - This reproduces the bug that was fixed:
    // SARIF reports violation on method declaration line (159), but Roslyn indexes method starting at body (160).
    // This simulates the real scenario with TryReplaceDockableContent where:
    // - SARIF violation is on line 159 (declaration: "private void TryReplaceDockableContent()")
    // - Method is indexed starting at line 160 (body start: "{")
    const string filePath = @"C:\Repo\ReloadRuntimeCommand.cs";
    const string normalizedPath = @"C:\Repo\ReloadRuntimeCommand.cs";
    var assembly = CreateAssembly("Rca.Loader");
    var type = CreateType(assembly, "ReloadRuntimeCommand", filePath, startLine: 16, endLine: 211);
    var member = CreateMember(type, "TryReplaceDockableContent", filePath, startLine: 160, endLine: 209);

    _lineIndex.AddMember(normalizedPath, member, 160, 209);
    _lineIndex.AddType(normalizedPath, type, 16, 211);
    _lineIndex.RegisterFileAssembly(normalizedPath, assembly);
    _solution.Assemblies.Add(assembly);

    // SARIF violation is on line 159 (declaration), but method indexed at line 160 (body)
    var sarifDocument = CreateSarifDocument(filePath, startLine: 159);

    // Act
    _applier.Apply(sarifDocument, _solution);

    // Assert
    _mergeTargets.Should().HaveCount(1);
    _mergeTargets[0].Should().Be(member, because:
      "When SARIF violation is on method declaration line (159) but method is indexed at body start (160), " +
      "the violation should still map to the method using near-start matching logic");
  }

  [Test]
  public void Apply_MetricOnMethodDeclarationLine_MethodIndexedAtBodyStart_WithMultipleMethods_MapsToCorrectMethod()
  {
    // Arrange - Multiple methods, violation on declaration line of one method
    const string filePath = @"C:\Repo\Test.cs";
    const string normalizedPath = @"C:\Repo\Test.cs";
    var assembly = CreateAssembly("TestAssembly");
    var type = CreateType(assembly, "TestType", filePath, startLine: 10, endLine: 100);

    // First method: declaration on line 20, body starts on line 21
    var method1 = CreateMember(type, "Method1", filePath, startLine: 21, endLine: 30);

    // Second method: declaration on line 40, body starts on line 41
    var method2 = CreateMember(type, "Method2", filePath, startLine: 41, endLine: 50);

    _lineIndex.AddMember(normalizedPath, method1, 21, 30);
    _lineIndex.AddMember(normalizedPath, method2, 41, 50);
    _lineIndex.AddType(normalizedPath, type, 10, 100);
    _lineIndex.RegisterFileAssembly(normalizedPath, assembly);
    _solution.Assemblies.Add(assembly);

    // SARIF violation on declaration line of Method1
    var sarifDocument = CreateSarifDocument(filePath, startLine: 20);

    // Act
    _applier.Apply(sarifDocument, _solution);

    // Assert
    _mergeTargets.Should().HaveCount(1);
    _mergeTargets[0].Should().Be(method1, because:
      "Violation on declaration line 20 should map to Method1 (indexed at body start 21), not Method2");
  }

  [Test]
  public void Apply_MetricInsideSingleLineIndexedMethod_MapsToMethodNotType()
  {
    // Arrange - This reproduces the bug that was fixed:
    // Method is indexed as single line (52-52) because Roslyn only provides declaration line.
    // SARIF violation is inside method body (line 54), but method doesn't contain it in range.
    // This simulates HandlePipeCommandAsync scenario:
    // - Method indexed at line 52 (StartLine=EndLine=52)
    // - SARIF violation on line 54 (inside method body)
    // - Next method starts at line 85 (HandleSyncCommand)
    const string filePath = @"C:\Repo\RuntimeCommandHandler.cs";
    const string normalizedPath = @"C:\Repo\RuntimeCommandHandler.cs";
    var assembly = CreateAssembly("Rca.Loader");
    var type = CreateType(assembly, "RuntimeCommandHandler", filePath, startLine: 20, endLine: 200);

    // Method indexed as single line (Roslyn limitation - only provides declaration line)
    var method = CreateMember(type, "HandlePipeCommandAsync", filePath, startLine: 52, endLine: 52);

    // Next method starts later (simulating HandleSyncCommand at line 85)
    var nextMethod = CreateMember(type, "HandleSyncCommand", filePath, startLine: 85, endLine: 85);

    _lineIndex.AddMember(normalizedPath, method, 52, 52);
    _lineIndex.AddMember(normalizedPath, nextMethod, 85, 85);
    _lineIndex.AddType(normalizedPath, type, 20, 200);
    _lineIndex.RegisterFileAssembly(normalizedPath, assembly);
    _solution.Assemblies.Add(assembly);

    // SARIF violation inside method body (line 54)
    var sarifDocument = CreateSarifDocument(filePath, startLine: 54);

    // Act
    _applier.Apply(sarifDocument, _solution);

    // Assert
    _mergeTargets.Should().HaveCount(1);
    _mergeTargets[0].Should().Be(method, because:
      "Violation on line 54 should map to HandlePipeCommandAsync (indexed at 52-52), not type, " +
      "since no other method starts between line 52 and line 54");
  }

  [Test]
  public void Apply_MetricBetweenTwoSingleLineIndexedMethods_MapsToCloserMethod()
  {
    // Arrange - Two methods indexed as single lines, violation between them
    const string filePath = @"C:\Repo\Test.cs";
    const string normalizedPath = @"C:\Repo\Test.cs";
    var assembly = CreateAssembly("TestAssembly");
    var type = CreateType(assembly, "TestType", filePath, startLine: 10, endLine: 100);

    var method1 = CreateMember(type, "Method1", filePath, startLine: 20, endLine: 20);
    var method2 = CreateMember(type, "Method2", filePath, startLine: 50, endLine: 50);

    _lineIndex.AddMember(normalizedPath, method1, 20, 20);
    _lineIndex.AddMember(normalizedPath, method2, 50, 50);
    _lineIndex.AddType(normalizedPath, type, 10, 100);
    _lineIndex.RegisterFileAssembly(normalizedPath, assembly);
    _solution.Assemblies.Add(assembly);

    // Violation between methods (line 35)
    var sarifDocument = CreateSarifDocument(filePath, startLine: 35);

    // Act
    _applier.Apply(sarifDocument, _solution);

    // Assert
    _mergeTargets.Should().HaveCount(1);
    _mergeTargets[0].Should().Be(method1, because:
      "Violation on line 35 should map to Method1 (line 20), not Method2 (line 50), " +
      "since Method1 is the closest preceding method");
  }

  [Test]
  public void Apply_MetricAfterSingleLineIndexedMethod_WithMethodInBetween_MapsToCloserMethod()
  {
    // Arrange - Violation is after Method2 starts, should map to Method2, not Method1
    // This tests that we correctly handle the case where another method exists between Method1 and violation
    const string filePath = @"C:\Repo\Test.cs";
    const string normalizedPath = @"C:\Repo\Test.cs";
    var assembly = CreateAssembly("TestAssembly");
    var type = CreateType(assembly, "TestType", filePath, startLine: 10, endLine: 100);

    var method1 = CreateMember(type, "Method1", filePath, startLine: 20, endLine: 20);
    // Method2 starts between Method1 and violation
    var method2 = CreateMember(type, "Method2", filePath, startLine: 25, endLine: 25);

    _lineIndex.AddMember(normalizedPath, method1, 20, 20);
    _lineIndex.AddMember(normalizedPath, method2, 25, 25);
    _lineIndex.AddType(normalizedPath, type, 10, 100);
    _lineIndex.RegisterFileAssembly(normalizedPath, assembly);
    _solution.Assemblies.Add(assembly);

    // Violation after Method2 starts (line 30 > Method2 start 25)
    var sarifDocument = CreateSarifDocument(filePath, startLine: 30);

    // Act
    _applier.Apply(sarifDocument, _solution);

    // Assert
    _mergeTargets.Should().HaveCount(1);
    // Should map to Method2 (line 25), not Method1 (line 20), since violation is after Method2 starts
    // Note: Method2 will be found via exact/near/containing match logic first, but if not,
    // the single-line logic should prefer Method2 over Method1 since Method2 is closer to violation
    _mergeTargets[0].Should().NotBe(method1, because:
      "Violation on line 30 should not map to Method1 (20), " +
      "since Method2 (25) starts between Method1 and violation");
    // Should map to Method2 or type
    _mergeTargets[0].Should().Match<MetricsNode>(n => n == method2 || n == type, because:
      "Violation on line 30 should map to Method2 (25) or type, since violation is after Method2 starts");
  }

  private static AssemblyMetricsNode CreateAssembly(string name)
  {
    return new AssemblyMetricsNode
    {
      Name = name,
      FullyQualifiedName = name,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = []
    };
  }

  private static TypeMetricsNode CreateType(
      AssemblyMetricsNode assembly,
      string name,
      string filePath,
      int startLine,
      int endLine)
  {
    var @namespace = new NamespaceMetricsNode
    {
      Name = "TestNamespace",
      FullyQualifiedName = "TestNamespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = []
    };

    var type = new TypeMetricsNode
    {
      Name = name,
      FullyQualifiedName = $"TestNamespace.{name}",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = [],
      Source = new SourceLocation
      {
        Path = filePath,
        StartLine = startLine,
        EndLine = endLine
      }
    };

    @namespace.Types.Add(type);
    assembly.Namespaces.Add(@namespace);

    return type;
  }

  private static MemberMetricsNode CreateMember(
      TypeMetricsNode type,
      string name,
      string filePath,
      int startLine,
      int endLine)
  {
    var member = new MemberMetricsNode
    {
      Name = name,
      FullyQualifiedName = $"{type.FullyQualifiedName}.{name}(...)",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Source = new SourceLocation
      {
        Path = filePath,
        StartLine = startLine,
        EndLine = endLine
      }
    };

    type.Members.Add(member);

    return member;
  }

  private static ParsedMetricsDocument CreateSarifDocument(string filePath, int startLine)
  {
    var location = new SourceLocation
    {
      Path = filePath,
      StartLine = startLine,
      EndLine = startLine
    };

    var element = new ParsedCodeElement(CodeElementKind.Member, "CA1822", null)
    {
      Source = location,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
        {
          Value = 1,
          Status = ThresholdStatus.NotApplicable,
          Breakdown = SarifBreakdownTestHelper.Single("CA1822")
        }
      }
    };

    return new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = [element],
      RuleDescriptions = new Dictionary<string, RuleDescription>()
    };
  }
}


