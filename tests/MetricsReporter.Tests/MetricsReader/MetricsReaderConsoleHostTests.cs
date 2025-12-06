namespace MetricsReporter.Tests.MetricsReader;

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.MetricsReader;
using MetricsReporter.Model;

/// <summary>
/// Verifies end-to-end execution through <see cref="MetricsReaderConsoleHost"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
[Parallelizable(ParallelScope.None)]
internal sealed class MetricsReaderConsoleHostTests : MetricsReaderCommandTestsBase
{
  [Test]
  public async Task ExecuteAsync_ReadAnyCommand_RunsSuccessfully()
  {
    // Arrange
    var report = MetricsReaderCommandTestData.CreateReport(new[]
    {
      MetricsReaderCommandTestData.CreateTypeNode("Rca.Loader.Services.HostType", 35, ThresholdStatus.Error)
    });
    var reportPath = WriteReport(report);
    var args = new[]
    {
      "readany",
      "--namespace", "Rca.Loader.Services",
      "--metric", "Complexity",
      "--report", reportPath,
      "--no-update",
      "--all"
    };

    // Act
    var (exitCode, output) = await ExecuteHostAsync(args).ConfigureAwait(false);

    // Assert
    exitCode.Should().Be(0);
    var json = JsonDocument.Parse(output).RootElement;
    json.ValueKind.Should().Be(JsonValueKind.Array);
    json.GetArrayLength().Should().BeGreaterThan(0);
  }

  private static async Task<(int ExitCode, string Output)> ExecuteHostAsync(string[] args)
  {
    var originalOut = Console.Out;
    using var writer = new StringWriter();
    Console.SetOut(writer);
    try
    {
      var exitCode = await MetricsReaderConsoleHost.ExecuteAsync(args).ConfigureAwait(false);
      return (exitCode, writer.ToString());
    }
    finally
    {
      Console.SetOut(originalOut);
    }
  }
}



