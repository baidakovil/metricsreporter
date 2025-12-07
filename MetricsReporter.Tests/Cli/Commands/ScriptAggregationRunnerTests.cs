using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MetricsReporter;
using MetricsReporter.Cli.Commands;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Configuration;
using MetricsReporter.Services.Processes;
using MetricsReporter.Services.Scripts;
using NSubstitute;
using NUnit.Framework;

namespace MetricsReporter.Tests.Cli.Commands;

[TestFixture]
[Category("Unit")]
internal sealed class ScriptAggregationRunnerTests
{
  private string _workingDirectory = null!;
  private IProcessRunner _processRunner = null!;
  private ScriptExecutionService _scriptExecutor = null!;
  private ScriptAggregationRunner _runner = null!;

  [SetUp]
  public void SetUp()
  {
    _workingDirectory = Path.Combine(Path.GetTempPath(), $"script-agg-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_workingDirectory);

    _processRunner = Substitute.For<IProcessRunner>();
    _scriptExecutor = new ScriptExecutionService(_processRunner);
    _runner = new ScriptAggregationRunner(_scriptExecutor);
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
  public async Task RunAsync_ScriptsDisabled_ReturnsNullAndDoesNotStartProcess()
  {
    // Arrange
    using var _ = new MetricsReporter.Tests.TestHelpers.ConsoleSilencer();
    var context = CreateContext(runScripts: false, aggregateAfterScripts: true, scripts: new[] { "script.ps1" });
    var expectedLogPath = Path.Combine(_workingDirectory, "MetricsReporter.read.log");

    // Act
    var result = await _runner.RunAsync(context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().BeNull();
    File.Exists(expectedLogPath).Should().BeFalse();
    await _processRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
  }

  [Test]
  public async Task RunAsync_WhenScriptMissing_ReturnsValidationExitCode()
  {
    // Arrange
    using var _ = new MetricsReporter.Tests.TestHelpers.ConsoleSilencer();
    var context = CreateContext(runScripts: true, aggregateAfterScripts: true, scripts: new[] { "missing.ps1" });

    // Act
    var result = await _runner.RunAsync(context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().Be((int)MetricsReporterExitCode.ValidationError);
    await _processRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
  }

  private ScriptAggregationContext CreateContext(bool runScripts, bool aggregateAfterScripts, string[] scripts)
  {
    var general = new ResolvedGeneralOptions(
      Verbosity: "normal",
      Timeout: TimeSpan.FromSeconds(5),
      WorkingDirectory: _workingDirectory,
      LogTruncationLimit: 4000,
      RunScripts: runScripts,
      AggregateAfterScripts: aggregateAfterScripts);

    var resolvedScripts = new ResolvedScripts(
      Generate: Array.Empty<string>(),
      ReadAny: scripts,
      ReadByMetric: Array.Empty<MetricScript>(),
      TestAny: Array.Empty<string>(),
      TestByMetric: Array.Empty<MetricScript>());

    var reportPath = Path.Combine(_workingDirectory, "report.json");
    return new ScriptAggregationContext(
      general,
      new MetricsReporterConfiguration(),
      new MetricsReporterConfiguration(),
      resolvedScripts,
      new[] { "Metric" },
      reportPath,
      ScriptSelection.SelectReadScripts,
      "read",
      "MetricsReporter.read.log");
  }
}

