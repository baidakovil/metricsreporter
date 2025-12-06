namespace MetricsReporter.Tests.MetricsReader;

using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.MetricsReader.Commands;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Integration-style tests for the readany command.
/// </summary>
[TestFixture]
[Category("Unit")]
[Parallelizable(ParallelScope.None)]
internal sealed class ReadAnyCommandTests : MetricsReaderCommandTestsBase
{
  [Test]
  public async Task ExecuteAsync_ShowAllTrue_ReturnsSortedList()
  {
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Warning", 12, ThresholdStatus.Warning),
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.ErrorMinor", 30, ThresholdStatus.Error),
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.ErrorMajor", 40, ThresholdStatus.Error)
    });

    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(reportPath, "Rca.Loader.Services", showAll: true);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    var rows = json.RootElement.EnumerateArray().ToList();
    rows.Should().HaveCount(3);
    rows.Select(r => r.GetProperty("symbolFqn").GetString()).Should().ContainInOrder(
      "Rca.Loader.Services.ErrorMajor",
      "Rca.Loader.Services.ErrorMinor",
      "Rca.Loader.Services.Warning");
  }

  [Test]
  public async Task ExecuteAsync_ShowAllFalse_ReturnsSingleMostSevereSymbol()
  {
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Warning", 12, ThresholdStatus.Warning),
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Error", 35, ThresholdStatus.Error)
    });

    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(reportPath, "Rca.Loader.Services");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    json.RootElement.GetProperty("symbolFqn").GetString().Should().Be("Rca.Loader.Services.Error");
    json.RootElement.GetProperty("status").GetString().Should().Be("Error");
  }

  [Test]
  public async Task ExecuteAsync_IgnoresSuppressedSymbolsWhenIncludeSuppressedFalse()
  {
    const string suppressedFqn = "Rca.Loader.Services.SuppressedService";
    var suppressedInfo = new SuppressedSymbolInfo
    {
      FullyQualifiedName = suppressedFqn,
      Metric = MetricIdentifier.RoslynCyclomaticComplexity.ToString(),
      RuleId = "CA1502",
      FilePath = "src/Rca.Loader/SuppressedService.cs"
    };

    var report = MetricsReaderCommandTestData.CreateReport(
      new[]
      {
        MetricsReaderCommandTestData.CreateTypeNode(suppressedFqn, 40, ThresholdStatus.Error),
        MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Active", 25, ThresholdStatus.Warning)
      },
      new[] { suppressedInfo });

    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(reportPath, "Rca.Loader.Services");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("symbolFqn").GetString().Should().Be("Rca.Loader.Services.Active");
  }

  [Test]
  public async Task ExecuteAsync_ShowAllTrue_IncludesSuppressedEntriesWhenRequested()
  {
    const string suppressedFqn = "Rca.Loader.Services.Suppressed";
    var suppressedInfo = new SuppressedSymbolInfo
    {
      FullyQualifiedName = suppressedFqn,
      Metric = MetricIdentifier.RoslynCyclomaticComplexity.ToString(),
      RuleId = "CA1502",
      FilePath = "src/Rca.Loader/Suppressed.cs"
    };

    var report = MetricsReaderCommandTestData.CreateReport(
      new[]
      {
        MetricsReaderCommandTestData.CreateTypeNode(suppressedFqn, 40, ThresholdStatus.Error)
      },
      new[] { suppressedInfo });

    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(reportPath, "Rca.Loader.Services", includeSuppressed: true, showAll: true);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetArrayLength().Should().Be(1);
    json.RootElement[0].GetProperty("symbolFqn").GetString().Should().Be(suppressedFqn);
    json.RootElement[0].GetProperty("isSuppressed").GetBoolean().Should().BeTrue();
  }

  [Test]
  public async Task ExecuteAsync_MemberSymbolKind_ReturnsMembers()
  {
    var member = MetricsReaderCommandTestData.CreateMemberNode("Rca.Loader.Services.Type.Execute(...)", 30, ThresholdStatus.Error);
    var type = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Type", 5, ThresholdStatus.Success, new[] { member });
    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });

    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(reportPath, "Rca.Loader.Services", symbolKind: MetricsReaderSymbolKind.Member, showAll: true);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetArrayLength().Should().Be(1);
    json.RootElement[0].GetProperty("symbolType").GetString().Should().Be("Member");
    json.RootElement[0].GetProperty("symbolFqn").GetString().Should().Contain("Execute(...)");
  }

  [Test]
  public async Task ExecuteAsync_SymbolKindAny_WithAll_PrefersTypesBeforeMembers()
  {
    // Arrange: Create a member with higher priority (Error + higher magnitude) than the type
    // to verify that types are still listed first when SymbolKind is Any
    var member = MetricsReaderCommandTestData.CreateMemberNode("Rca.Loader.Services.MixedType.Execute(...)", 60, ThresholdStatus.Error);
    var type = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.MixedType", 30, ThresholdStatus.Error, new[] { member });
    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });

    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(
      reportPath,
      "Rca.Loader.Services",
      symbolKind: MetricsReaderSymbolKind.Any,
      showAll: true);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var rows = json.RootElement.EnumerateArray().ToList();
    rows.Should().HaveCount(2);
    // Verify that type comes before member even though member has higher magnitude (60-25=35 vs 30-20=10)
    rows[0].GetProperty("symbolType").GetString().Should().Be("Type");
    rows[0].GetProperty("symbolFqn").GetString().Should().Be("Rca.Loader.Services.MixedType");
    rows[1].GetProperty("symbolType").GetString().Should().Be("Member");
    rows[1].GetProperty("symbolFqn").GetString().Should().Contain("Execute(...)");
  }

  [Test]
  public async Task ExecuteAsync_ThresholdOverride_IsApplied()
  {
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Target", 12, ThresholdStatus.Warning)
    });

    var reportPath = WriteReport(report);
    var overridePath = WriteThresholdOverride(5, 6);
    var settings = CreateNamespaceSettings(reportPath, "Rca.Loader.Services", thresholdsFile: overridePath);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("threshold").GetDecimal().Should().Be(5);
    json.RootElement.GetProperty("thresholdKind").GetString().Should().Be("Warning");
  }

  [Test]
  public async Task ExecuteAsync_ShowAllTrue_EmptyNamespace_PrintsMessage()
  {
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.SomeType", 40, ThresholdStatus.Error)
    });

    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(reportPath, "Rca.Other.Namespace", showAll: true);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("metric").GetString().Should().Be("Complexity");
    json.RootElement.GetProperty("namespace").GetString().Should().Be("Rca.Other.Namespace");
    json.RootElement.GetProperty("message").GetString().Should().Contain("No violations were found");
  }

  [Test]
  public async Task ExecuteAsync_NoViolations_PrintsMessage()
  {
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Clean", 5, ThresholdStatus.Success)
    });

    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(reportPath, "Rca.Loader.Services");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("metric").GetString().Should().Be("Complexity");
    json.RootElement.GetProperty("namespace").GetString().Should().Be("Rca.Loader.Services");
    json.RootElement.GetProperty("message").GetString().Should().Contain("No violations were found");
  }

  [Test]
  public async Task ExecuteAsync_GroupByType_ReturnsGroupedEnvelope()
  {
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Critical.TypeA", 40, ThresholdStatus.Error),
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Critical.TypeB", 20, ThresholdStatus.Warning)
    });

    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(
      reportPath,
      "Rca.Loader.Services",
      showAll: true,
      groupBy: MetricsReaderGroupByOption.Type);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var root = json.RootElement;
    root.GetProperty("groupBy").GetString().Should().Be("type");
    root.GetProperty("violationsGroupsCount").GetInt32().Should().Be(2);

    var groups = root.GetProperty("violationsGroups").EnumerateArray().ToList();
    groups.Should().HaveCount(2);

    var first = groups[0];
    first.GetProperty("type").GetString().Should().Be("Rca.Loader.Services.Critical.TypeA");
    first.GetProperty("violationsCount").GetInt32().Should().Be(1);
    first.GetProperty("violations")[0].GetProperty("symbolFqn").GetString()
      .Should().Be("Rca.Loader.Services.Critical.TypeA");

    var second = groups[1];
    second.GetProperty("violationsCount").GetInt32().Should().Be(1);
    second.GetProperty("type").GetString().Should().Be("Rca.Loader.Services.Critical.TypeB");
  }

  [Test]
  public async Task ExecuteAsync_GroupByNamespaceWithoutAll_ReturnsSingleGroupAndCount()
  {
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Primary.TypeA", 40, ThresholdStatus.Error),
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Secondary.TypeB", 30, ThresholdStatus.Error)
    });

    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(
      reportPath,
      "Rca.Loader",
      groupBy: MetricsReaderGroupByOption.Namespace);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var root = json.RootElement;
    root.GetProperty("groupBy").GetString().Should().Be("namespace");
    root.GetProperty("violationsGroupsCount").GetInt32().Should().Be(2);
    root.GetProperty("violationsGroups").GetArrayLength().Should().Be(1);
    var group = root.GetProperty("violationsGroups")[0];
    group.GetProperty("namespace").GetString().Should().Be("Rca.Loader.Primary");
  }

  [Test]
  public void NamespaceSettings_GroupByRuleId_ReturnsValidationError()
  {
    var settings = new NamespaceMetricSettings
    {
      ReportPath = "build/Metrics/Report/MetricsReport.g.json",
      Namespace = "Rca.Loader",
      Metric = "Complexity",
      GroupBy = MetricsReaderGroupByOption.RuleId
    };

    var validation = settings.Validate();
    validation.Successful.Should().BeFalse();
    validation.Message.Should().Contain("ruleId");
  }

  [Test]
  public async Task ExecuteAsync_GroupByMethod_UsesMethodKeys()
  {
    var member = MetricsReaderCommandTestData.CreateMemberNode(
      "Rca.Loader.Services.Type.Process(...)",
      60,
      ThresholdStatus.Error);
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.Type",
      15,
      ThresholdStatus.Warning,
      new[] { member });
    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(
      reportPath,
      "Rca.Loader.Services",
      symbolKind: MetricsReaderSymbolKind.Member,
      showAll: true,
      groupBy: MetricsReaderGroupByOption.Method);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var group = json.RootElement.GetProperty("violationsGroups")[0];
    group.GetProperty("method").GetString().Should().Be("Process");
    group.GetProperty("violationsCount").GetInt32().Should().Be(1);
  }

  [Test]
  public async Task ExecuteAsync_GroupByMetric_AggregatesAcrossSymbols()
  {
    var typeA = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Core.TypeA", 40, ThresholdStatus.Error);
    var typeB = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Core.TypeB", 35, ThresholdStatus.Error);
    var report = MetricsReaderCommandTestData.CreateReport(new[] { typeA, typeB });

    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(
      reportPath,
      "Rca.Loader.Core",
      showAll: true,
      groupBy: MetricsReaderGroupByOption.Metric);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var groups = json.RootElement.GetProperty("violationsGroups").EnumerateArray().ToList();
    groups.Should().HaveCount(1);
    groups[0].GetProperty("metric").GetString().Should().Be("RoslynCyclomaticComplexity");
    groups[0].GetProperty("violationsCount").GetInt32().Should().Be(2);
  }

  [Test]
  public async Task ExecuteAsync_GroupByType_NoViolations_PrintsMessage()
  {
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Exists", 5, ThresholdStatus.Success)
    });

    var reportPath = WriteReport(report);
    var settings = CreateNamespaceSettings(
      reportPath,
      "Rca.Other.Namespace",
      groupBy: MetricsReaderGroupByOption.Type);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunNamespaceCommandAsync<ReadAnyCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("message").GetString().Should().Contain("No violations were found");
  }
}

