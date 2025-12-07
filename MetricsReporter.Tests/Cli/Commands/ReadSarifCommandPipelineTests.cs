using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MetricsReporter;
using MetricsReporter.Cli.Commands;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Configuration;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;
using MetricsReporter.Services.Processes;
using MetricsReporter.Services.Scripts;
using NSubstitute;
using NUnit.Framework;

namespace MetricsReporter.Tests.Cli.Commands;

[TestFixture]
[Category("Unit")]
internal sealed class ReadSarifCommandPipelineTests
{
  private string _workingDirectory = null!;
  private IProcessRunner _processRunner = null!;
  private ScriptAggregationRunner _scriptRunner = null!;
  private ReadSarifScriptContextFactory _scriptContextFactory = null!;
  private TestReadSarifExecutorFactory _executorFactory = null!;
  private ReadSarifCommandPipeline _pipeline = null!;

  [SetUp]
  public void SetUp()
  {
    _workingDirectory = Path.Combine(Path.GetTempPath(), $"readsarif-pipeline-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_workingDirectory);

    _processRunner = Substitute.For<IProcessRunner>();
    _scriptRunner = new ScriptAggregationRunner(new ScriptExecutionService(_processRunner));
    _scriptContextFactory = new ReadSarifScriptContextFactory();
    _executorFactory = new TestReadSarifExecutorFactory(CreateEngine());
    _pipeline = new ReadSarifCommandPipeline(_scriptRunner, _executorFactory, _scriptContextFactory);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_workingDirectory))
    {
      Directory.Delete(_workingDirectory, recursive: true);
    }
  }

  [Test]
  public async Task ExecuteAsync_WithScriptsSkipped_InvokesSarifExecutor()
  {
    // Arrange
    using var _ = new MetricsReporter.Tests.TestHelpers.ConsoleSilencer();
    var general = new ResolvedGeneralOptions("normal", TimeSpan.FromSeconds(5), _workingDirectory, 4000, RunScripts: false, AggregateAfterScripts: true);
    var sarifSettings = new SarifMetricSettings
    {
      ReportPath = Path.Combine(_workingDirectory, "report.json"),
      Namespace = "Company.Project",
      Metric = null,
      SymbolKind = MetricsReaderSymbolKind.Any,
      IncludeSuppressed = false
    };
    sarifSettings.Validate();
    sarifSettings.TryResolveSarifMetrics(out var resolvedMetricIdentifiers);
    var metrics = resolvedMetricIdentifiers?.Select(metric => metric.ToString()) ?? Array.Empty<string>();

    var context = new ReadSarifCommandContext(
      general,
      new MetricsReporterConfiguration(),
      new MetricsReporterConfiguration(),
      new ResolvedScripts(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<MetricScript>(), Array.Empty<string>(), Array.Empty<MetricScript>()),
      sarifSettings,
      metrics);

    var groups = new[]
    {
      new SarifViolationGroup(
        "CA1506",
        "Avoid excessive class coupling",
        MetricIdentifier.SarifCaRuleViolations,
        1,
        Array.Empty<SarifViolationRecord>(),
        Array.Empty<SarifSymbolContribution>())
    };

    _executorFactory.Aggregator.Groups = groups.ToList();

    // Act
    var exitCode = await _pipeline.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    exitCode.Should().Be(0);
    _executorFactory.Aggregator.Calls.Should().ContainSingle(call => call.Namespace == sarifSettings.Namespace);
    _executorFactory.ResultHandler.WroteResponse.Should().BeTrue();
    _executorFactory.ResultHandler.WroteInvalidMetric.Should().BeFalse();
    await _processRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
  }

  private static MetricsReaderEngine CreateEngine()
  {
    var enumerator = Substitute.For<IMetricsNodeEnumerator>();
    enumerator.EnumerateTypeNodes().Returns(Array.Empty<TypeMetricsNode>());
    enumerator.EnumerateMemberNodes().Returns(Array.Empty<MemberMetricsNode>());
    enumerator.EnumerateNodes(Arg.Any<SymbolFilter>()).Returns(Array.Empty<MetricsNode>());

    var snapshotBuilder = Substitute.For<ISymbolSnapshotBuilder>();
    var violationAggregator = Substitute.For<ISarifViolationAggregator>();
    var violationOrderer = Substitute.For<ISarifViolationOrderer>();
    return new MetricsReaderEngine(enumerator, snapshotBuilder, violationAggregator, violationOrderer, new MetricsReport());
  }
}

internal sealed class TestReadSarifExecutorFactory : IReadSarifExecutorFactory
{
  public TestReadSarifExecutorFactory(MetricsReaderEngine engine)
  {
    Engine = engine;
    Aggregator = new StubSarifGroupAggregator();
    Sorter = new StubSarifGroupSorter();
    Filter = new StubSarifGroupFilter();
    ResultHandler = new StubReadSarifResultHandler();
  }

  public MetricsReaderEngine Engine { get; }

  public StubSarifGroupAggregator Aggregator { get; }

  public StubSarifGroupSorter Sorter { get; }

  public StubSarifGroupFilter Filter { get; }

  public StubReadSarifResultHandler ResultHandler { get; }

  public ReadSarifCommandExecutor Create()
  {
    return new ReadSarifCommandExecutor(
      (_settings, _token) => Task.FromResult(Engine),
      Aggregator,
      Sorter,
      Filter,
      ResultHandler);
  }
}

internal sealed class StubSarifGroupAggregator : ISarifGroupAggregator
{
  public List<(string Namespace, IEnumerable<MetricIdentifier> Metrics, MetricsReaderSymbolKind Kind, bool IncludeSuppressed)> Calls { get; } = [];

  public List<SarifViolationGroup> Groups { get; set; } = new();

  public List<SarifViolationGroup> AggregateGroups(
    MetricsReaderEngine engine,
    string @namespace,
    IEnumerable<MetricIdentifier> metrics,
    MetricsReaderSymbolKind symbolKind,
    bool includeSuppressed)
  {
    Calls.Add((@namespace, metrics, symbolKind, includeSuppressed));
    return Groups;
  }
}

internal sealed class StubSarifGroupSorter : ISarifGroupSorter
{
  public List<SarifViolationGroup> SortByCountAndRuleId(IEnumerable<SarifViolationGroup> groups)
  {
    return groups.OrderBy(group => group.RuleId).ToList();
  }
}

internal sealed class StubSarifGroupFilter : ISarifGroupFilter
{
  public List<SarifViolationGroup> Filter(List<SarifViolationGroup> groups, string? ruleId)
  {
    return groups;
  }
}

internal sealed class StubReadSarifResultHandler : IReadSarifCommandResultHandler
{
  public bool WroteResponse { get; private set; }

  public bool WroteInvalidMetric { get; private set; }

  public void WriteResponse(SarifMetricSettings settings, IEnumerable<SarifViolationGroup> groups)
  {
    WroteResponse = true;
  }

  public void WriteNoViolationsFound(string metric, string @namespace, string symbolKind, string? ruleId)
  {
    WroteResponse = true;
  }

  public void WriteInvalidMetricError(string metric)
  {
    WroteInvalidMetric = true;
  }
}

