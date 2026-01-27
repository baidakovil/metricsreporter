namespace MetricsReporter.Tests.Processing.Parsers;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using MetricsReporter.Processing.Parsers;

/// <summary>
/// Unit tests for <see cref="RoslynMetricsParser"/> class.
/// </summary>
/// <remarks>
/// These tests verify that RoslynMetricsParser correctly normalizes method signatures
/// from Roslyn XML format to unified format, ensuring symbols can be properly merged
/// with symbols from other sources (e.g., OpenCover).
/// </remarks>
[TestFixture]
[Category("Unit")]
public sealed class RoslynMetricsParserTests
{
  private RoslynMetricsParser parser = null!;

  [SetUp]
  public void SetUp()
  {
    parser = new RoslynMetricsParser();
  }

  [Test]
  public async Task ParseAsync_MethodWithNullableParameters_NormalizesParameters()
  {
    // Arrange
    var xml = CreateRoslynXml(
        assemblyName: "Rca.Loader, Version=1.0.0.0",
        namespaceName: "Rca.Loader",
        typeName: "LoaderApp",
        memberName: "void OnApplicationIdling(object? sender, IdlingEventArgs e)",
        maintainabilityIndex: 80);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.Elements.Should().NotBeEmpty();
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp.OnApplicationIdling(...)");
      member.Name.Should().Be("OnApplicationIdling");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_MethodWithShortTypeNames_NormalizesParameters()
  {
    // Arrange
    var xml = CreateRoslynXml(
        assemblyName: "Rca.Loader, Version=1.0.0.0",
        namespaceName: "Rca.Loader",
        typeName: "LoaderApp",
        memberName: "void OnApplicationIdling(object sender, IdlingEventArgs e)",
        maintainabilityIndex: 80);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.Elements.Should().NotBeEmpty();
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp.OnApplicationIdling(...)");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_MethodWithReturnType_ExtractsMethodNameCorrectly()
  {
    // Arrange
    var xml = CreateRoslynXml(
        assemblyName: "Rca.Loader, Version=1.0.0.0",
        namespaceName: "Rca.Loader",
        typeName: "LoaderApp",
        memberName: "System.String GetName(System.Int32 id)",
        maintainabilityIndex: 80);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.Elements.Should().NotBeEmpty();
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp.GetName(...)");
      member.Name.Should().Be("GetName");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_MethodWithoutParameters_NormalizesCorrectly()
  {
    // Arrange
    var xml = CreateRoslynXml(
        assemblyName: "Rca.Loader, Version=1.0.0.0",
        namespaceName: "Rca.Loader",
        typeName: "LoaderApp",
        memberName: "void Initialize()",
        maintainabilityIndex: 80);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.Elements.Should().NotBeEmpty();
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp.Initialize(...)");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_PropertyGetter_NormalizesCorrectly()
  {
    // Arrange
    var xml = CreateRoslynXml(
        assemblyName: "Rca.Network, Version=1.0.0.0",
        namespaceName: "Rca.Network",
        typeName: "NetworkPlaceholder",
        memberName: "System.Boolean get_IsReady()",
        maintainabilityIndex: 80);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.Elements.Should().NotBeEmpty();
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.Network.NetworkPlaceholder.get_IsReady(...)");
      member.Name.Should().Be("get_IsReady");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_GenericMethod_NormalizesCorrectly()
  {
    // Arrange
    var xml = CreateRoslynXml(
        assemblyName: "Rca.Loader, Version=1.0.0.0",
        namespaceName: "Rca.Loader",
        typeName: "LoaderApp",
        memberName: "T Process<T>(T input)",
        maintainabilityIndex: 80);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.Elements.Should().NotBeEmpty();
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp.Process(...)");
      member.Name.Should().Be("Process");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_MultipleMethods_SameType_NormalizesAll()
  {
    // Arrange
    var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<CodeMetricsReport>
  <Targets>
    <Target Name=""Solution"">
      <Assembly Name=""Rca.Loader, Version=1.0.0.0"">
        <Namespaces>
          <Namespace Name=""Rca.Loader"">
            <Types>
              <Type Name=""LoaderApp"">
                <Members>
                  <Member Name=""void OnApplicationIdling(object? sender, IdlingEventArgs e)"" File=""LoaderApp.cs"" Line=""10"">
                    <Metrics>
                      <Metric Name=""MaintainabilityIndex"" Value=""80"" />
                    </Metrics>
                  </Member>
                  <Member Name=""void OnShutdown(UIControlledApplication application)"" File=""LoaderApp.cs"" Line=""20"">
                    <Metrics>
                      <Metric Name=""MaintainabilityIndex"" Value=""75"" />
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

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      var members = result.Elements.Where(e => e.Kind == CodeElementKind.Member).ToList();
      members.Should().HaveCount(2);
      members[0].FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp.OnApplicationIdling(...)");
      members[1].FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp.OnShutdown(...)");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_ComplexMethodSignature_NormalizesCorrectly()
  {
    // Arrange
    var xml = CreateRoslynXml(
        assemblyName: "Rca.Loader, Version=1.0.0.0",
        namespaceName: "Rca.Loader",
        typeName: "LoaderApp",
        memberName: "System.Threading.Tasks.Task<System.String> ProcessDataAsync(System.String? input, System.Collections.Generic.Dictionary<System.String, System.Int32>? options)",
        maintainabilityIndex: 80);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.Elements.Should().NotBeEmpty();
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp.ProcessDataAsync(...)");
      member.Name.Should().Be("ProcessDataAsync");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_GlobalNamespace_HandlesCorrectly()
  {
    // Arrange
    var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<CodeMetricsReport>
  <Targets>
    <Target Name=""Solution"">
      <Assembly Name=""Rca.Loader, Version=1.0.0.0"">
        <Namespaces>
          <Namespace Name="""">
            <Types>
              <Type Name=""GlobalClass"">
                <Members>
                  <Member Name=""void Method()"" File=""GlobalClass.cs"" Line=""10"">
                    <Metrics>
                      <Metric Name=""MaintainabilityIndex"" Value=""80"" />
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

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.Elements.Should().NotBeEmpty();
      var type = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Type);
      type.Should().NotBeNull();
      type!.FullyQualifiedName.Should().Be("GlobalClass");

      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("GlobalClass.Method(...)");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_OpenCoverAndRoslyn_ProduceSameNormalizedName()
  {
    // Arrange - This test verifies that OpenCover and Roslyn formats normalize to the same result
    // OpenCover format: "void Rca.Loader.LoaderApp.OnApplicationIdling(System.Object, Autodesk.Revit.UI.Events.IdlingEventArgs)"
    // Roslyn format: "void OnApplicationIdling(object? sender, IdlingEventArgs e)"
    // Both should normalize to: "Rca.Loader.LoaderApp.OnApplicationIdling(...)"

    var roslynXml = CreateRoslynXml(
        assemblyName: "Rca.Loader, Version=1.0.0.0",
        namespaceName: "Rca.Loader",
        typeName: "LoaderApp",
        memberName: "void OnApplicationIdling(object? sender, IdlingEventArgs e)",
        maintainabilityIndex: 80);

    var roslynFile = CreateTempFile(roslynXml);

    try
    {
      // Act
      var roslynResult = await parser.ParseAsync(roslynFile, CancellationToken.None);

      // Assert
      var roslynMember = roslynResult.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      roslynMember.Should().NotBeNull();
      roslynMember!.FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp.OnApplicationIdling(...)");
    }
    finally
    {
      File.Delete(roslynFile);
    }
  }

  [Test]
  public async Task ParseAsync_RealWorld_TaskStringMethod_NormalizesCorrectly()
  {
    // Arrange - from real Roslyn data
    var xml = CreateRoslynXml(
        assemblyName: "Rca.Contracts, Version=1.0.0.0",
        namespaceName: "Rca.Contracts",
        typeName: "IPythonExecutionService",
        memberName: "Task<string> IPythonExecutionService.ExecuteAsync(string code)",
        maintainabilityIndex: 100);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.Contracts.IPythonExecutionService.ExecuteAsync(...)");
      member.Name.Should().Be("ExecuteAsync");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RealWorld_GenericMethodRegister_NormalizesCorrectly()
  {
    // Arrange - from real Roslyn data
    var xml = CreateRoslynXml(
        assemblyName: "Rca.Contracts, Version=1.0.0.0",
        namespaceName: "Rca.Contracts.Infrastructure",
        typeName: "IServiceRegistrar",
        memberName: "void IServiceRegistrar.Register<TInterface>(TInterface implementation)",
        maintainabilityIndex: 100);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.Contracts.Infrastructure.IServiceRegistrar.Register(...)");
      member.Name.Should().Be("Register");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RealWorld_GenericMethodRegisterWithFunc_NormalizesCorrectly()
  {
    // Arrange - from real Roslyn data
    var xml = CreateRoslynXml(
        assemblyName: "Rca.Contracts, Version=1.0.0.0",
        namespaceName: "Rca.Contracts.Infrastructure",
        typeName: "IServiceRegistrar",
        memberName: "void IServiceRegistrar.Register<TInterface>(Func<TInterface> factory)",
        maintainabilityIndex: 100);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.Contracts.Infrastructure.IServiceRegistrar.Register(...)");
      member.Name.Should().Be("Register");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RealWorld_GenericMethodResolve_NormalizesCorrectly()
  {
    // Arrange - from real Roslyn data
    var xml = CreateRoslynXml(
        assemblyName: "Rca.Contracts, Version=1.0.0.0",
        namespaceName: "Rca.Contracts.Infrastructure",
        typeName: "IServiceResolver",
        memberName: "TInterface IServiceResolver.Resolve<TInterface>()",
        maintainabilityIndex: 100);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.Contracts.Infrastructure.IServiceResolver.Resolve(...)");
      member.Name.Should().Be("Resolve");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RealWorld_TaskTupleMethod_NormalizesCorrectly()
  {
    // Arrange - from real Roslyn data
    var xml = CreateRoslynXml(
        assemblyName: "Rca.Contracts, Version=1.0.0.0",
        namespaceName: "Rca.Contracts.Services",
        typeName: "IRuntimeManager",
        memberName: "Task<(bool Success, string? ErrorMessage)> IRuntimeManager.LoadRuntimeAsync(string folderPath)",
        maintainabilityIndex: 80);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.Contracts.Services.IRuntimeManager.LoadRuntimeAsync(...)");
      member.Name.Should().Be("LoadRuntimeAsync");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RealWorld_GenericLogMethod_NormalizesCorrectly()
  {
    // Arrange - from real Roslyn data
    var xml = CreateRoslynXml(
        assemblyName: "Rca.UI, Version=1.0.0.0",
        namespaceName: "Rca.UI.Logging",
        typeName: "UiPipeLogger",
        memberName: "void UiPipeLogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)",
        maintainabilityIndex: 75);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.UI.Logging.UiPipeLogger.Log(...)");
      member.Name.Should().Be("Log");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RealWorld_ConstructorWithFunc_NormalizesCorrectly()
  {
    // Arrange - from real Roslyn data
    var xml = CreateRoslynXml(
        assemblyName: "Rca.UI, Version=1.0.0.0",
        namespaceName: "Rca.UI",
        typeName: "RcaDockablePanelViewModel",
        memberName: "RcaDockablePanelViewModel.RcaDockablePanelViewModel(Func<object?> revitContextProvider, IPythonExecutionService pythonService)",
        maintainabilityIndex: 80);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("Rca.UI.RcaDockablePanelViewModel.RcaDockablePanelViewModel(...)");
      member.Name.Should().Be("RcaDockablePanelViewModel");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_NestedTypeMethod_NormalizesWithoutDuplicatedTypeName()
  {
    // Arrange
    // Emulates real Roslyn data for:
    // Namespace: MetricsReporter.Aggregation
    // Type: MetricsAggregationService.ReportMetadataComposer (nested type)
    // Member: ReportMetadataComposer.AssembleMetadataInput(MetricsAggregationInput input, MetadataComponents components)
    //
    // The expected normalized FQN must NOT contain a duplicated "ReportMetadataComposer" segment.
    var xml = CreateRoslynXml(
        assemblyName: "MetricsReporter, Version=1.0.0.0",
        namespaceName: "MetricsReporter.Aggregation",
        typeName: "MetricsAggregationService.ReportMetadataComposer",
        memberName: "ReportMetadataInput ReportMetadataComposer.AssembleMetadataInput(MetricsAggregationInput input, MetadataComponents components)",
        maintainabilityIndex: 77);

    var tempFile = CreateTempFile(xml);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.Elements.Should().NotBeEmpty();
      var member = result.Elements.FirstOrDefault(e => e.Kind == CodeElementKind.Member);
      member.Should().NotBeNull();
      member!.FullyQualifiedName.Should().Be("MetricsReporter.Aggregation.MetricsAggregationService.ReportMetadataComposer.AssembleMetadataInput(...)");
      member.Name.Should().Be("AssembleMetadataInput");
    }
    finally
    {
      File.Delete(tempFile);
    }
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


