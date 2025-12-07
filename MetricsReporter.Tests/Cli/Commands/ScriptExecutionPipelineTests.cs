using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MetricsReporter.Cli.Commands;
using NSubstitute;
using NUnit.Framework;

namespace MetricsReporter.Tests.Cli.Commands;

[TestFixture]
[Category("Unit")]
internal sealed class ScriptExecutionGuardTests
{
  private IScriptRunNotifier _notifier = null!;
  private ScriptExecutionGuard _guard = null!;

  [SetUp]
  public void SetUp()
  {
    _notifier = Substitute.For<IScriptRunNotifier>();
    _guard = new ScriptExecutionGuard(_notifier);
  }

  [Test]
  public void ShouldSkip_WhenScriptsDisabled_NotifiesAndReturnsTrue()
  {
    // Arrange
    var request = CreateRequest(shouldRunScripts: false, hasScripts: true);

    // Act
    var result = _guard.ShouldSkip(request, "read");

    // Assert
    result.Should().BeTrue();
    _notifier.Received(1).NotifyScriptsDisabled("read");
    _notifier.DidNotReceive().NotifyNoScripts(Arg.Any<string>());
  }

  [Test]
  public void ShouldSkip_WhenNoScripts_NotifiesAndReturnsTrue()
  {
    // Arrange
    var request = CreateRequest(shouldRunScripts: true, hasScripts: false);

    // Act
    var result = _guard.ShouldSkip(request, "generate");

    // Assert
    result.Should().BeTrue();
    _notifier.Received(1).NotifyNoScripts("generate");
    _notifier.DidNotReceive().NotifyScriptsDisabled(Arg.Any<string>());
  }

  [Test]
  public void ShouldSkip_WhenScriptsPresent_ReturnsFalseWithoutNotifications()
  {
    // Arrange
    var request = CreateRequest(shouldRunScripts: true, hasScripts: true);

    // Act
    var result = _guard.ShouldSkip(request, "test");

    // Assert
    result.Should().BeFalse();
    _notifier.DidNotReceiveWithAnyArgs().NotifyScriptsDisabled(default!);
    _notifier.DidNotReceiveWithAnyArgs().NotifyNoScripts(default!);
  }

  private static GenerateScriptRunRequest CreateRequest(bool shouldRunScripts, bool hasScripts)
  {
    return new GenerateScriptRunRequest(
      shouldRunScripts,
      hasScripts,
      "log",
      "normal",
      Array.Empty<string>(),
      Environment.CurrentDirectory,
      TimeSpan.FromSeconds(5),
      4000);
  }
}

[TestFixture]
[Category("Unit")]
internal sealed class GenerateScriptExecutionPipelineTests
{
  private IScriptExecutionGuard _guard = null!;
  private IGenerateScriptExecutionClient _client = null!;
  private GenerateScriptExecutionPipeline _pipeline = null!;

  [SetUp]
  public void SetUp()
  {
    _guard = Substitute.For<IScriptExecutionGuard>();
    _client = Substitute.For<IGenerateScriptExecutionClient>();
    _pipeline = new GenerateScriptExecutionPipeline(_guard, _client);
  }

  [Test]
  public async Task ExecuteAsync_WhenGuardSkips_ReturnsNullAndDoesNotCallClient()
  {
    // Arrange
    var request = CreateRequest();
    _guard.ShouldSkip(request, "generate").Returns(true);

    // Act
    var result = await _pipeline.ExecuteAsync(request, CancellationToken.None).ConfigureAwait(false);

    // Assert
    result.Should().BeNull();
    await _client.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default);
  }

  [Test]
  public async Task ExecuteAsync_WhenGuardAllows_InvokesClientAndForwardsOperationName()
  {
    // Arrange
    var request = CreateRequest();
    _guard.ShouldSkip(request, "custom").Returns(false);
    _client.ExecuteAsync(request, Arg.Any<CancellationToken>()).Returns(Task.FromResult<int?>(5));

    // Act
    var result = await _pipeline.ExecuteAsync(request, CancellationToken.None, "custom").ConfigureAwait(false);

    // Assert
    result.Should().Be(5);
    _guard.Received(1).ShouldSkip(request, "custom");
    await _client.Received(1).ExecuteAsync(request, Arg.Any<CancellationToken>());
  }

  private static GenerateScriptRunRequest CreateRequest()
  {
    return new GenerateScriptRunRequest(
      ShouldRunScripts: true,
      HasScripts: true,
      LogPath: "log",
      Verbosity: "normal",
      Scripts: Array.Empty<string>(),
      WorkingDirectory: Environment.CurrentDirectory,
      Timeout: TimeSpan.FromSeconds(5),
      LogTruncationLimit: 4000);
  }
}

