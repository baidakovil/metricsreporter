namespace MetricsReporter.Tests.MetricsReader;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.MetricsReader.Commands;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Tests for the readsarif command.
/// </summary>
[TestFixture]
[Category("Unit")]
[Parallelizable(ParallelScope.None)]
internal sealed class ReadSarifCommandTests : MetricsReaderCommandTestsBase
{
  [Test]
  public async Task ExecuteAsync_ReturnsLargestViolationGroupByDefault()
  {
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.RuleConsumer",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifIdeRuleViolations] = CreateSarifMetric(
          ("IDE0060", new[]
          {
            ("Remove unused parameter", "file:///src/Consumer.cs", 42),
            ("Remove unused parameter", "file:///src/Consumer.cs", 48)
          }),
          ("IDE0040", new[]
          {
            ("Add call to ConfigureAwait", "file:///src/Consumer.cs", 30)
          }))
      });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    report.Metadata.RuleDescriptions["IDE0060"] = new RuleDescription { ShortDescription = "Unused parameter" };
    report.Metadata.RuleDescriptions["IDE0040"] = new RuleDescription { ShortDescription = "Await configure" };

    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(reportPath, "Rca.Loader.Services", metricName: "SarifIdeRuleViolations");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var root = json.RootElement;
    root.GetProperty("metric").GetString().Should().Be("SarifIdeRuleViolations");
    root.GetProperty("groupBy").GetString().Should().Be("ruleId");
    root.GetProperty("violationsGroupsCount").GetInt32().Should().Be(2);
    var groups = root.GetProperty("violationsGroups").EnumerateArray().ToList();
    groups.Should().HaveCount(1);
    var group = groups[0];
    group.GetProperty("ruleId").GetString().Should().Be("IDE0060");
    group.GetProperty("violationsCount").GetInt32().Should().Be(2);
    group.GetProperty("shortDescription").GetString().Should().Be("Unused parameter");
  }

  [Test]
  public async Task ExecuteAsync_DefaultMetricPicksMostSevereGroupAcrossSarifMetrics()
  {
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.RuleConsumer",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifIdeRuleViolations] = CreateSarifMetric(
          ("IDE0060", new[]
          {
            ("Unused parameter", "file:///src/Consumer.cs", 10),
            ("Unused parameter", "file:///src/Consumer.cs", 15)
          })),
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(
          ("CA1502", new[]
          {
            ("Avoid complexity", "file:///src/Consumer.cs", 20)
          }))
      });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(reportPath, "Rca.Loader.Services");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("metric").GetString().Should().Be("Any");
    var group = json.RootElement.GetProperty("violationsGroups")[0];
    group.GetProperty("ruleId").GetString().Should().Be("IDE0060");
    group.GetProperty("violationsCount").GetInt32().Should().Be(2);
  }

  [Test]
  public async Task ExecuteAsync_AllFlag_ReturnsAllGroupsSortedByCount()
  {
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.RuleConsumer",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifIdeRuleViolations] = CreateSarifMetric(
          ("IDE0060", new[]
          {
            ("Remove unused parameter", "file:///src/Consumer.cs", 42)
          }),
          ("IDE0040", new[]
          {
            ("Add call to ConfigureAwait", "file:///src/Consumer.cs", 30),
            ("Add call to ConfigureAwait", "file:///src/Consumer.cs", 31),
          }))
      });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Services",
      showAll: true,
      metricName: "SarifIdeRuleViolations");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var groups = json.RootElement.GetProperty("violationsGroups").EnumerateArray().ToList();
    groups.Should().HaveCount(2);
    groups[0].GetProperty("ruleId").GetString().Should().Be("IDE0040");
    groups[0].GetProperty("violationsCount").GetInt32().Should().Be(2);
    groups[1].GetProperty("ruleId").GetString().Should().Be("IDE0060");
    groups[1].GetProperty("violationsCount").GetInt32().Should().Be(1);
  }

  [Test]
  public async Task ExecuteAsync_NonSarifMetric_PrintsMessage()
  {
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.Target", 12, ThresholdStatus.Warning)
    });

    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(reportPath, "Rca.Loader.Services", metricName: "RoslynCyclomaticComplexity");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("metric").GetString().Should().Be("RoslynCyclomaticComplexity");
    json.RootElement.GetProperty("message").GetString().Should().Contain("does not expose SARIF rule breakdown data");
  }

  [Test]
  public async Task ExecuteAsync_NoMatchingViolations_PrintsMessage()
  {
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.RuleConsumer",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifIdeRuleViolations] = CreateSarifMetric(
          ("IDE0060", new[]
          {
            ("Remove unused parameter", "file:///src/Consumer.cs", 42)
          }))
      });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Other.Namespace",
      metricName: "SarifIdeRuleViolations",
      showAll: true);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("metric").GetString().Should().Be("SarifIdeRuleViolations");
    json.RootElement.GetProperty("namespace").GetString().Should().Be("Rca.Other.Namespace");
    json.RootElement.GetProperty("message").GetString().Should().Contain("No SARIF violations");
  }

  [Test]
  public async Task ExecuteAsync_MemberSymbolKindWithoutMemberMetrics_PrintsMessage()
  {
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.RuleConsumer",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(
          ("CA1502", new[]
          {
            ("Avoid complexity", "file:///src/Consumer.cs", 10)
          }))
      });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Services",
      symbolKind: MetricsReaderSymbolKind.Member,
      metricName: "SarifCaRuleViolations",
      showAll: true);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("metric").GetString().Should().Be("SarifCaRuleViolations");
    json.RootElement.GetProperty("symbolKind").GetString().Should().Be("Member");
    json.RootElement.GetProperty("message").GetString().Should().Contain("No SARIF violations");
  }

  [Test]
  public async Task ExecuteAsync_WithRuleIdFilter_ReturnsOnlyMatchingGroup()
  {
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.RuleConsumer",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(
          ("CA1502", new[]
          {
            ("Avoid complexity", "file:///src/Consumer.cs", 10)
          }),
          ("CA1506", new[]
          {
            ("Reduce coupling", "file:///src/Consumer.cs", 20),
            ("Reduce coupling", "file:///src/Consumer.cs", 25)
          }))
      });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Services",
      showAll: true,
      metricName: "SarifCaRuleViolations",
      ruleId: "CA1506");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var groups = json.RootElement.GetProperty("violationsGroups").EnumerateArray().ToList();
    groups.Should().HaveCount(1);
    groups[0].GetProperty("ruleId").GetString().Should().Be("CA1506");
    groups[0].GetProperty("violationsCount").GetInt32().Should().Be(2);
  }

  [Test]
  public async Task ExecuteAsync_RuleIdFilterWithDefaultMetricFiltersGroups()
  {
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.RuleConsumer",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(
          ("CA1502", new[]
          {
            ("Avoid complexity", "file:///src/Consumer.cs", 10)
          }),
          ("CA1506", new[]
          {
            ("Reduce coupling", "file:///src/Consumer.cs", 20),
            ("Reduce coupling", "file:///src/Consumer.cs", 25)
          }))
      });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Services",
      showAll: true,
      ruleId: "CA1506");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var groups = json.RootElement.GetProperty("violationsGroups").EnumerateArray().ToList();
    groups.Should().HaveCount(1);
    groups[0].GetProperty("ruleId").GetString().Should().Be("CA1506");
  }

  [Test]
  public async Task ExecuteAsync_WithRuleIdFilter_IsCaseInsensitive()
  {
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.RuleConsumer",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifIdeRuleViolations] = CreateSarifMetric(
          ("IDE0060", new[]
          {
            ("Remove unused parameter", "file:///src/Consumer.cs", 42)
          }))
      });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Services",
      showAll: true,
      metricName: "SarifIdeRuleViolations",
      ruleId: "ide0060");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var groups = json.RootElement.GetProperty("violationsGroups").EnumerateArray().ToList();
    groups.Should().HaveCount(1);
    groups[0].GetProperty("ruleId").GetString().Should().Be("IDE0060");
  }

  [Test]
  public async Task ExecuteAsync_WithRuleIdFilterButNoMatches_PrintsMessage()
  {
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.RuleConsumer",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(
          ("CA1502", new[]
          {
            ("Avoid complexity", "file:///src/Consumer.cs", 10)
          }))
      });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Services",
      metricName: "SarifCaRuleViolations",
      ruleId: "CA9999");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("metric").GetString().Should().Be("SarifCaRuleViolations");
    json.RootElement.GetProperty("message").GetString().Should().Contain("rule 'CA9999'");
  }

  [Test]
  public async Task ExecuteAsync_DefaultSymbolKindAny_IncludesMemberViolations()
  {
    var member = MetricsReaderCommandTestData.CreateMemberNode(
      "Rca.Loader.Services.RuleConsumer.Process(...)",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(("CA1502", new[]
        {
          ("Avoid complexity", "file:///src/Consumer.cs", 10)
        }))
      });

    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.RuleConsumer",
      new Dictionary<MetricIdentifier, MetricValue>(),
      new[] { member });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Services",
      showAll: true,
      metricName: "SarifCaRuleViolations");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var group = json.RootElement.GetProperty("violationsGroups")[0];
    group.GetProperty("ruleId").GetString().Should().Be("CA1502");
    group.GetProperty("violations")[0].GetProperty("symbol").GetString().Should().Contain("Process(...)");
  }

  [Test]
  public async Task ExecuteAsync_SuppressedSymbolsExcludedByDefault()
  {
    const string suppressedFqn = "Rca.Loader.Services.RuleConsumer";
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      suppressedFqn,
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(
          ("CA1502", new[]
          {
            ("Avoid complexity", "file:///src/Consumer.cs", 10)
          }))
      });

    var suppressedInfo = new SuppressedSymbolInfo
    {
      FullyQualifiedName = suppressedFqn,
      Metric = MetricIdentifier.SarifCaRuleViolations.ToString(),
      RuleId = "CA1502",
      FilePath = "src/Rca.Loader/RuleConsumer.cs"
    };

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type }, new[] { suppressedInfo });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(reportPath, "Rca.Loader.Services", metricName: "SarifCaRuleViolations");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("metric").GetString().Should().Be("SarifCaRuleViolations");
    var message = json.RootElement.GetProperty("message").GetString();
    message.Should().Contain("No SARIF violations");
    message.Should().NotContain("rule");
  }

  [Test]
  public async Task ExecuteAsync_SuppressedSymbolsIncludedWhenRequested()
  {
    const string suppressedFqn = "Rca.Loader.Services.RuleConsumer";
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      suppressedFqn,
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(
          ("CA1502", new[]
          {
            ("Avoid complexity", "file:///src/Consumer.cs", 10)
          }))
      });

    var suppressedInfo = new SuppressedSymbolInfo
    {
      FullyQualifiedName = suppressedFqn,
      Metric = MetricIdentifier.SarifCaRuleViolations.ToString(),
      RuleId = "CA1502",
      FilePath = "src/Rca.Loader/RuleConsumer.cs"
    };

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type }, new[] { suppressedInfo });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Services",
      includeSuppressed: true,
      showAll: true,
      metricName: "SarifCaRuleViolations");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var groups = json.RootElement.GetProperty("violationsGroups").EnumerateArray().ToList();
    groups.Should().HaveCount(1);
    groups[0].GetProperty("ruleId").GetString().Should().Be("CA1502");
  }

  [Test]
  public async Task ExecuteAsync_SuppressedViaRuleIdMapping_ExcludedByDefault()
  {
    const string suppressedFqn = "Rca.Loader.Services.RuleConsumer";
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      suppressedFqn,
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(
          ("CA1506", new[]
          {
            ("Avoid excessive class coupling", "file:///src/Consumer.cs", 10)
          }))
      });

    var suppressedInfo = new SuppressedSymbolInfo
    {
      FullyQualifiedName = suppressedFqn,
      Metric = MetricIdentifier.RoslynClassCoupling.ToString(),
      RuleId = "CA1506",
      FilePath = "src/Rca.Loader/RuleConsumer.cs"
    };

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type }, new[] { suppressedInfo });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(reportPath, "Rca.Loader.Services", metricName: "SarifCaRuleViolations");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("metric").GetString().Should().Be("SarifCaRuleViolations");
    json.RootElement.GetProperty("message").GetString().Should().Contain("No SARIF violations");
  }

  [Test]
  public async Task ExecuteAsync_MemberSymbolKind_UsesMemberSymbols()
  {
    var memberMetric = CreateSarifMetric(("IDE0060", new[]
    {
      ("Remove unused parameter", "file:///src/Member.cs", 10)
    }));

    var member = MetricsReaderCommandTestData.CreateMemberNode(
      "Rca.Loader.Services.Type.DoWork(...)",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifIdeRuleViolations] = memberMetric
      });

    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.Type",
      new Dictionary<MetricIdentifier, MetricValue>(),
      new[] { member });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Services",
      symbolKind: MetricsReaderSymbolKind.Member,
      showAll: true,
      metricName: "SarifIdeRuleViolations");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var violation = json.RootElement
      .GetProperty("violationsGroups")[0]
      .GetProperty("violations")[0];

    violation.GetProperty("symbol").GetString().Should().Be("Rca.Loader.Services.Type.DoWork(...)");
  }

  [Test]
  public async Task ExecuteAsync_GroupByMethod_ReturnsMethodKeys()
  {
    var memberMetric = CreateSarifMetric(("CA1502", new[]
    {
      ("Avoid complexity", "file:///src/Consumer.cs", 42)
    }));

    var member = MetricsReaderCommandTestData.CreateMemberNode(
      "Rca.Loader.Services.RuleConsumer.Process(...)",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = memberMetric
      });

    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.RuleConsumer",
      new Dictionary<MetricIdentifier, MetricValue>(),
      new[] { member });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Services",
      showAll: true,
      groupBy: MetricsReaderGroupByOption.Method,
      metricName: "SarifCaRuleViolations");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var group = json.RootElement.GetProperty("violationsGroups")[0];
    group.GetProperty("method").GetString().Should().Be("Process");
    group.GetProperty("violationsCount").GetInt32().Should().Be(1);
  }

  [Test]
  public async Task ExecuteAsync_GroupByNamespaceWithRuleFilterWithoutMatches_PrintsMessage()
  {
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Services.RuleConsumer",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(
          ("CA1502", new[]
          {
            ("Avoid complexity", "file:///src/Consumer.cs", 10)
          }))
      });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Services",
      groupBy: MetricsReaderGroupByOption.Namespace,
      metricName: "SarifCaRuleViolations",
      ruleId: "CA9999");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    json.RootElement.GetProperty("message").GetString().Should().Contain("rule 'CA9999'");
  }

  [Test]
  public async Task ExecuteAsync_GroupByNamespace_UsesCountsWhenViolationsEmpty()
  {
    var breakdown = new Dictionary<string, SarifRuleBreakdownEntry>
    {
      ["CA1502"] = new SarifRuleBreakdownEntry
      {
        Count = 5,
        Violations = new List<SarifRuleViolationDetail>()
      }
    };

    var metrics = new Dictionary<MetricIdentifier, MetricValue>
    {
      [MetricIdentifier.SarifCaRuleViolations] = new MetricValue
      {
        Value = 5,
        Status = ThresholdStatus.NotApplicable,
        Breakdown = breakdown
      }
    };

    var type = MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.RuleConsumer", metrics);
    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Services",
      showAll: true,
      groupBy: MetricsReaderGroupByOption.Namespace,
      metricName: "SarifCaRuleViolations");

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var group = json.RootElement.GetProperty("violationsGroups")[0];
    group.GetProperty("namespace").GetString().Should().Be("Rca.Loader.Services");
    group.GetProperty("violationsCount").GetInt32().Should().Be(5);
    group.GetProperty("violations").GetArrayLength().Should().Be(0);
  }

  [Test]
  public async Task ExecuteAsync_GroupByNamespace_ReturnsAggregatedGroups()
  {
    var typeA = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Core.TypeA",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(
          ("CA1502", new[]
          {
            ("Avoid complexity", "file:///src/Core/TypeA.cs", 10),
            ("Avoid complexity", "file:///src/Core/TypeA.cs", 20)
          }))
      });

    var typeB = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Infrastructure.TypeB",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(
          ("CA1506", new[]
          {
            ("Reduce coupling", "file:///src/Infra/TypeB.cs", 30)
          }))
      });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { typeA, typeB }, namespaceName: "Rca.Loader");
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader",
      showAll: true,
      groupBy: MetricsReaderGroupByOption.Namespace);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var root = json.RootElement;
    root.GetProperty("groupBy").GetString().Should().Be("namespace");
    root.GetProperty("violationsGroupsCount").GetInt32().Should().Be(2);
    var groups = root.GetProperty("violationsGroups").EnumerateArray().ToList();
    groups.Should().HaveCount(2);
    groups[0].GetProperty("namespace").GetString().Should().Be("Rca.Loader.Core");
    groups[0].GetProperty("violationsCount").GetInt32().Should().Be(2);
    groups[1].GetProperty("namespace").GetString().Should().Be("Rca.Loader.Infrastructure");
    groups[1].GetProperty("violationsCount").GetInt32().Should().Be(1);
  }

  [Test]
  public async Task ExecuteAsync_GroupByMetric_ReturnsBucketsPerMetric()
  {
    var type = MetricsReaderCommandTestData.CreateTypeNode(
      "Rca.Loader.Core.TypeA",
      new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.SarifCaRuleViolations] = CreateSarifMetric(
          ("CA1502", new[]
          {
            ("Avoid complexity", "file:///src/Core/TypeA.cs", 10)
          })),
        [MetricIdentifier.SarifIdeRuleViolations] = CreateSarifMetric(
          ("IDE0060", new[]
          {
            ("Remove unused parameter", "file:///src/Core/TypeA.cs", 15),
            ("Remove unused parameter", "file:///src/Core/TypeA.cs", 18)
          }))
      });

    var report = MetricsReaderCommandTestData.CreateReport(new[] { type });
    var reportPath = WriteReport(report);
    var settings = CreateSarifSettings(
      reportPath,
      "Rca.Loader.Core",
      showAll: true,
      groupBy: MetricsReaderGroupByOption.Metric);

    var (exitCode, output) = await MetricsReaderCommandTestHarness
      .RunSarifCommandAsync<ReadSarifCommand>(settings)
      .ConfigureAwait(false);

    exitCode.Should().Be(0);
    using var json = JsonDocument.Parse(output);
    var root = json.RootElement;
    root.GetProperty("groupBy").GetString().Should().Be("metric");
    root.GetProperty("violationsGroupsCount").GetInt32().Should().Be(2);
    var groups = root.GetProperty("violationsGroups").EnumerateArray().ToList();
    groups.Should().HaveCount(2);
    groups[0].GetProperty("metric").GetString().Should().Be("SarifIdeRuleViolations");
    groups[0].GetProperty("violationsCount").GetInt32().Should().Be(2);
    groups[1].GetProperty("metric").GetString().Should().Be("SarifCaRuleViolations");
    groups[1].GetProperty("violationsCount").GetInt32().Should().Be(1);
  }

  private static MetricValue CreateSarifMetric(params (string RuleId, (string Message, string Uri, int StartLine)[] Violations)[] entries)
  {
    var breakdown = new Dictionary<string, SarifRuleBreakdownEntry>();
    foreach (var entry in entries)
    {
      var violations = entry.Violations
        .Select(detail => new SarifRuleViolationDetail
        {
          Message = detail.Message,
          Uri = detail.Uri,
          StartLine = detail.StartLine,
          EndLine = detail.StartLine
        })
        .ToList();

      breakdown[entry.RuleId] = new SarifRuleBreakdownEntry
      {
        Count = violations.Count,
        Violations = violations
      };
    }

    return new MetricValue
    {
      Value = breakdown.Values.Sum(v => (decimal?)v.Count),
      Status = ThresholdStatus.NotApplicable,
      Breakdown = breakdown
    };
  }
}

