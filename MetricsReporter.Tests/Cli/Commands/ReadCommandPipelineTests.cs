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
internal sealed class ReadCommandPipelineTests
{
  private string _workingDirectory = null!;
  private IProcessRunner _processRunner = null!;
  private ScriptAggregationRunner _scriptRunner = null!;
  private ReadScriptContextFactory _scriptContextFactory = null!;
  private TestReadCommandExecutorFactory _executorFactory = null!;
  private ReadCommandPipeline _pipeline = null!;

  [SetUp]
  public void SetUp()
  {
    _workingDirectory = Path.Combine(Path.GetTempPath(), $"read-pipeline-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_workingDirectory);

    _processRunner = Substitute.For<IProcessRunner>();
    _scriptRunner = new ScriptAggregationRunner(new ScriptExecutionService(_processRunner));
    _scriptContextFactory = new ReadScriptContextFactory();
    _executorFactory = new TestReadCommandExecutorFactory(CreateEngine());
    _pipeline = new ReadCommandPipeline(_scriptRunner, _executorFactory, _scriptContextFactory);
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
  public async Task ExecuteAsync_WithScriptsSkipped_ExecutesMetricsReaderOnce()
  {
    // Arrange
    using var _ = new MetricsReporter.Tests.TestHelpers.ConsoleSilencer();
    var general = new ResolvedGeneralOptions("normal", TimeSpan.FromSeconds(5), _workingDirectory, 4000, RunScripts: false, AggregateAfterScripts: true);
    var readerSettings = CreateReaderSettings();
    var metrics = new[] { readerSettings.ResolvedMetric.ToString() };
    var context = new ReadCommandContext(
      general,
      new MetricsReporterConfiguration(),
      new MetricsReporterConfiguration(),
      new ResolvedScripts(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<MetricScript>(), Array.Empty<string>(), Array.Empty<MetricScript>()),
      readerSettings,
      metrics,
      readerSettings.ReportPath!);

    var snapshots = new[]
    {
      new SymbolMetricSnapshot(
        "Company.Type",
        CodeElementKind.Type,
        null,
        readerSettings.ResolvedMetric,
        new MetricValue { Status = ThresholdStatus.Warning },
        null,
        IsSuppressed: false)
    };
    _executorFactory.QueryService.Result = snapshots;

    // Act
    var exitCode = await _pipeline.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    exitCode.Should().Be(0);
    _executorFactory.QueryService.Calls.Should().ContainSingle(call => call.Namespace == readerSettings.Namespace && call.Metric == readerSettings.ResolvedMetric);
    _executorFactory.ResultHandler.HandledParameters.Should().NotBeNull();
    _executorFactory.ResultHandler.HandledSnapshots.Should().ContainSingle();
    await _processRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
  }

  private NamespaceMetricSettings CreateReaderSettings()
  {
    var settings = new NamespaceMetricSettings
    {
      ReportPath = Path.Combine(_workingDirectory, "report.json"),
      Namespace = "Company.Project",
      Metric = MetricIdentifier.RoslynClassCoupling.ToString(),
      SymbolKind = MetricsReaderSymbolKind.Type,
      ShowAll = true,
      IncludeSuppressed = true
    };

    settings.Validate();
    return settings;
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

internal sealed class TestReadCommandExecutorFactory : IReadCommandExecutorFactory
{
  public TestReadCommandExecutorFactory(MetricsReaderEngine engine)
  {
    Engine = engine;
    QueryService = new StubSymbolQueryService();
    Orderer = new StubSymbolSnapshotOrderer();
    ResultHandler = new StubReadAnyResultHandler();
  }

  public MetricsReaderEngine Engine { get; }

  public StubSymbolQueryService QueryService { get; }

  public StubSymbolSnapshotOrderer Orderer { get; }

  public StubReadAnyResultHandler ResultHandler { get; }

  public ReadAnyCommandExecutor Create()
  {
    return new ReadAnyCommandExecutor(
      (_settings, _token) => Task.FromResult(Engine),
      QueryService,
      Orderer,
      ResultHandler);
  }
}

internal sealed class StubSymbolQueryService : ISymbolQueryService
{
  public List<(string Namespace, MetricIdentifier Metric, MetricsReaderSymbolKind Kind, bool IncludeSuppressed)> Calls { get; } = [];

  public IEnumerable<SymbolMetricSnapshot> Result { get; set; } = Enumerable.Empty<SymbolMetricSnapshot>();

  public IEnumerable<SymbolMetricSnapshot> GetProblematicSymbols(
    MetricsReaderEngine engine,
    string @namespace,
    MetricIdentifier metric,
    MetricsReaderSymbolKind symbolKind,
    bool includeSuppressed)
  {
    Calls.Add((@namespace, metric, symbolKind, includeSuppressed));
    return Result;
  }
}

internal sealed class StubSymbolSnapshotOrderer : ISymbolSnapshotOrderer
{
  public IOrderedEnumerable<SymbolMetricSnapshot> Order(IEnumerable<SymbolMetricSnapshot> snapshots, SymbolSnapshotOrderingParameters parameters)
  {
    return snapshots.OrderBy(snapshot => snapshot.Symbol);
  }
}

internal sealed class StubReadAnyResultHandler : IReadAnyCommandResultHandler
{
  public IEnumerable<SymbolMetricSnapshot>? HandledSnapshots { get; private set; }

  public ReadAnyCommandResultParameters? HandledParameters { get; private set; }

  public void HandleResults(IEnumerable<SymbolMetricSnapshot> snapshots, ReadAnyCommandResultParameters parameters)
  {
    HandledSnapshots = snapshots.ToArray();
    HandledParameters = parameters;
  }
}

