namespace MetricsReporter.Tests.MetricsReader;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.MetricsReader.Services;

/// <summary>
/// Tests for <see cref="MetricsUpdater"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class MetricsUpdaterTests
{
  [Test]
  public async Task UpdateAsync_UsesProjectMsbuildInvocation_WhenUpdateFlagSpecified()
  {
    using var sandbox = new TempDirectory();
    var solutionPath = Path.Combine(sandbox.Path, "rca-plugin.sln");
    File.WriteAllText(solutionPath, string.Empty);

    var testsProjectDir = Path.Combine(sandbox.Path, "tests", "MetricsReporter.Tests");
    Directory.CreateDirectory(testsProjectDir);
    var testsProjectPath = Path.Combine(testsProjectDir, "MetricsReporter.Tests.csproj");
    File.WriteAllText(testsProjectPath, "<Project />");

    var runtimeProjectDir = Path.Combine(sandbox.Path, "src", "Rca.Runtime");
    Directory.CreateDirectory(runtimeProjectDir);
    var runtimeProjectPath = Path.Combine(runtimeProjectDir, "Rca.Runtime.csproj");
    File.WriteAllText(runtimeProjectPath, "<Project />");

    // Ensure file is written to disk before searching
    if (!File.Exists(runtimeProjectPath))
    {
      throw new InvalidOperationException($"Runtime project file was not created at {runtimeProjectPath}");
    }

    var updater = new TestMetricsUpdater(solutionPath);
    await updater.UpdateAsync(CancellationToken.None).ConfigureAwait(false);

    updater.CapturedProjectPath.Should().Be(testsProjectPath);
    updater.CapturedArguments.Should().Be($"msbuild \"{testsProjectPath}\" /t:Build /p:GenerateMetricsDashboard=true /p:BuildProjectReferences=false /p:SkipMetricsReporterBuild=true /p:RoslynMetricsEnabled=true");
    updater.CapturedCoverageProjectPath.Should().Be(runtimeProjectPath);
    updater.CapturedCoverageArguments.Should().Be($"msbuild \"{runtimeProjectPath}\" /t:CollectCoverage /p:AltCoverEnabled=true");
  }

  private sealed class TestMetricsUpdater(string solutionPath) : MetricsUpdater(solutionPath)
  {
    public string? CapturedProjectPath { get; private set; }

    public string? CapturedArguments { get; private set; }

    public string? CapturedCoverageProjectPath { get; private set; }

    public string? CapturedCoverageArguments { get; private set; }

    protected override ProcessStartInfo CreateStartInfo(string projectPath, string solutionDirectory)
    {
      var startInfo = base.CreateStartInfo(projectPath, solutionDirectory);
      CapturedProjectPath = projectPath;
      CapturedArguments = startInfo.Arguments;

      var shell = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe";
      startInfo.FileName = shell;
      startInfo.Arguments = "/c exit 0";
      return startInfo;
    }

    protected override ProcessStartInfo CreateCoverageStartInfo(string projectPath, string solutionDirectory)
    {
      var startInfo = base.CreateCoverageStartInfo(projectPath, solutionDirectory);
      CapturedCoverageProjectPath = projectPath;
      CapturedCoverageArguments = startInfo.Arguments;

      var shell = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe";
      startInfo.FileName = shell;
      startInfo.Arguments = "/c exit 0";
      return startInfo;
    }

    /// <summary>
    /// Overrides RunProcessAsync to suppress console output during tests.
    /// The base implementation writes to Console.Out/Console.Error which pollutes test output.
    /// </summary>
    protected override async Task RunProcessAsync(ProcessStartInfo startInfo, string startMessage, string successMessage, CancellationToken cancellationToken)
    {
      using var process = new Process { StartInfo = startInfo };
      // Suppress console output: don't call Console.WriteLine here
      if (!process.Start())
      {
        throw new InvalidOperationException("Failed to start metrics update process.");
      }

      // Redirect output to null to prevent console pollution
      var stdOutTask = PumpAsync(process.StandardOutput, TextWriter.Null, cancellationToken);
      var stdErrTask = PumpAsync(process.StandardError, TextWriter.Null, cancellationToken);

      await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
      await Task.WhenAll(stdOutTask, stdErrTask).ConfigureAwait(false);

      if (process.ExitCode != 0)
      {
        throw new InvalidOperationException($"Metrics update failed with exit code {process.ExitCode}.");
      }

      // Suppress success message output
    }
  }

  private sealed class TempDirectory : IDisposable
  {
    public TempDirectory()
    {
      Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"metrics-updater-tests-{Guid.NewGuid():N}");
      Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
      try
      {
        if (Directory.Exists(Path))
        {
          Directory.Delete(Path, recursive: true);
        }
      }
      catch
      {
        // ignore cleanup failures
      }
    }
  }
}

