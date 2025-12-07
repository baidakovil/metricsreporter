namespace MetricsReporter.Tests.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MetricsReporter;
using MetricsReporter.Logging;
using MetricsReporter.Services.Processes;
using MetricsReporter.Services.Scripts;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

[TestFixture]
[Category("Unit")]
internal sealed class ScriptExecutionServiceTests
{
  private string _workingDirectory = null!;
  private IProcessRunner _processRunner = null!;
  private ILogger _logger = null!;
  private ScriptExecutionService _service = null!;

  [SetUp]
  public void SetUp()
  {
    _workingDirectory = Path.Combine(Path.GetTempPath(), $"script-tests-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_workingDirectory);
    _processRunner = Substitute.For<IProcessRunner>();
    _logger = Substitute.For<ILogger>();
    _service = new ScriptExecutionService(_processRunner);
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
  public async Task RunAsync_WithNoScripts_ReturnsSuccessAndDoesNotInvokeRunner()
  {
    // Arrange
    var context = CreateContext();

    // Act
    var result = await _service.RunAsync(Array.Empty<string>(), context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.IsSuccess.Should().BeTrue();
    await _processRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
  }

  [Test]
  public async Task RunAsync_MissingScript_ReturnsValidationErrorAndSkipsRunner()
  {
    // Arrange
    var missingScript = Path.Combine(_workingDirectory, "missing.ps1");
    var context = CreateContext();

    // Act
    var result = await _service.RunAsync(new[] { missingScript }, context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.ExitCode.Should().Be(MetricsReporterExitCode.ValidationError);
    result.FailedScript.Should().Be(missingScript);
    await _processRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
  }

  [Test]
  public async Task RunAsync_SingleScriptSuccess_InvokesRunnerAndReturnsSuccess()
  {
    // Arrange
    var scriptPath = CreateScriptFile("scripts/run.ps1");
    var context = CreateContext();
    _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
      .Returns(CreateResult(exitCode: 0));

    // Act
    var result = await _service.RunAsync(new[] { "scripts/run.ps1" }, context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.IsSuccess.Should().BeTrue();
    await _processRunner.Received(1).RunAsync(
      Arg.Is<ProcessRunRequest>(r => r.FileName == "pwsh" && r.Arguments.Contains(scriptPath) && r.WorkingDirectory == _workingDirectory),
      Arg.Any<CancellationToken>());
    _logger.Received().LogInformation(Arg.Is<string>(m => m.Contains("Starting script")));
    _logger.Received().LogInformation(Arg.Is<string>(m => m.Contains("completed")));
  }

  [Test]
  public async Task RunAsync_FirstScriptFails_StopsPipelineAndReturnsFailure()
  {
    // Arrange
    var script1 = CreateScriptFile("scripts/first.ps1");
    var script2 = CreateScriptFile("scripts/second.ps1");
    var context = CreateContext();
    _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
      .Returns(CreateResult(exitCode: 1));

    // Act
    var result = await _service.RunAsync(new[] { script1, script2 }, context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.FailedScript.Should().Be(script1);
    result.ExitCode.Should().Be(MetricsReporterExitCode.ValidationError);
    await _processRunner.Received(1).RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>());
    _logger.Received().LogError(Arg.Is<string>(m => m.Contains("failed with exit code 1")), null);
  }

  [Test]
  public async Task RunAsync_ScriptTimesOut_ReturnsFailureAndLogsTimeout()
  {
    // Arrange
    var script = CreateScriptFile("scripts/slow.ps1");
    var context = CreateContext();
    _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
      .Returns(CreateResult(exitCode: -1, timedOut: true));

    // Act
    var result = await _service.RunAsync(new[] { script }, context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.FailedScript.Should().Be(script);
    result.ExitCode.Should().Be(MetricsReporterExitCode.ValidationError);
    result.ErrorMessage.Should().Contain("timed out");
    _logger.Received().LogError(Arg.Is<string>(m => m.Contains("timed out")), null);
  }

  [Test]
  public async Task RunAsync_OnFailure_LogsTruncatedOutput()
  {
    // Arrange
    var script = CreateScriptFile("scripts/fail.ps1");
    var context = CreateContext(logTruncationLimit: 5);
    _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
      .Returns(CreateResult(exitCode: 1, stdout: "ABCDEFGHIJ", stderr: "KLMNOPQRST"));

    // Act
    _ = await _service.RunAsync(new[] { script }, context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    _logger.Received().LogError(Arg.Is<string>(m => m.Contains("stdout (truncated)") && m.Contains("...")), null);
    _logger.Received().LogError(Arg.Is<string>(m => m.Contains("stderr (truncated)") && m.Contains("...")), null);
  }

  [Test]
  public async Task RunAsync_WhenProcessStartThrows_ReturnsValidationError()
  {
    // Arrange
    var script = CreateScriptFile("scripts/fail-fast.ps1");
    var context = CreateContext();
    var exception = new InvalidOperationException("start failed");
    _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
      .Throws(exception);

    // Act
    var result = await _service.RunAsync(new[] { script }, context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.ExitCode.Should().Be(MetricsReporterExitCode.ValidationError);
    result.ErrorMessage.Should().Be(exception.Message);
    await _processRunner.Received(1).RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>());
    _logger.Received().LogError(Arg.Is<string>(m => m.Contains("Failed to start script")), exception);
  }

  [Test]
  public async Task RunAsync_WithWhitespaceEntries_SkipsEmptyScripts()
  {
    // Arrange
    var validScript = CreateScriptFile("scripts/valid.ps1");
    var context = CreateContext();
    _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
      .Returns(CreateResult(exitCode: 0));

    // Act
    var result = await _service.RunAsync(new[] { "  ", validScript }, context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.IsSuccess.Should().BeTrue();
    await _processRunner.Received(1).RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>());
  }

  [Test]
  public async Task RunAsync_LogTruncationLimitNotPositive_UsesDefaultLimit()
  {
    // Arrange
    var script = CreateScriptFile("scripts/overflow.ps1");
    var context = CreateContext(logTruncationLimit: 0);
    var longOutput = new string('A', 4005);
    _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
      .Returns(CreateResult(exitCode: 1, stdout: longOutput, stderr: longOutput));

    // Act
    _ = await _service.RunAsync(new[] { script }, context, CancellationToken.None).ConfigureAwait(false);

    // Assert
    _logger.Received().LogError(Arg.Is<string>(m => m.Contains("stdout (truncated)") && m.Contains("...")), null);
    _logger.Received().LogError(Arg.Is<string>(m => m.Contains("stderr (truncated)") && m.Contains("...")), null);
  }

  private ScriptExecutionContext CreateContext(int logTruncationLimit = 4000)
  {
    return new ScriptExecutionContext(_workingDirectory, TimeSpan.FromSeconds(30), logTruncationLimit, _logger);
  }

  private string CreateScriptFile(string relativePath)
  {
    var fullPath = Path.GetFullPath(Path.Combine(_workingDirectory, relativePath));
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    File.WriteAllText(fullPath, "# dummy");
    return fullPath;
  }

  private static ProcessRunResult CreateResult(int exitCode, bool timedOut = false, string stdout = "", string stderr = "")
  {
    var started = DateTimeOffset.UtcNow;
    return new ProcessRunResult(exitCode, timedOut, started, started.AddSeconds(1), stdout, stderr);
  }
}

