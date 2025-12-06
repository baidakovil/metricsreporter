namespace MetricsReporter.MetricsReader.Services;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Runs the MSBuild targets that refresh MetricsReport.g.json and collect code coverage.
/// </summary>
/// <remarks>
/// This updater runs two MSBuild targets in sequence:
/// 1. CollectCoverage target to collect code coverage (only runs if AltCoverEnabled=true in code-metrics.props)
/// 2. Build target with GenerateMetricsDashboard=true to regenerate metrics report, which includes coverage data from step 1
/// The CollectCoverage target condition ensures it only runs when AltCoverEnabled is true, so no explicit check is needed here.
/// Coverage must be collected first because GenerateMetricsDashboard includes coverage files in the consolidated metrics report.
/// </remarks>
internal class MetricsUpdater : IMetricsUpdater
{
  private readonly string _solutionPath;

  public MetricsUpdater(string solutionPath)
    => _solutionPath = solutionPath ?? throw new ArgumentNullException(nameof(solutionPath));

  /// <summary>
  /// Updates metrics by collecting code coverage (if enabled), then running GenerateMetricsDashboard target.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <remarks>
  /// Coverage collection is controlled by the AltCoverEnabled property in code-metrics.props.
  /// The CollectCoverage target will automatically skip if AltCoverEnabled=false.
  /// Coverage is collected first because GenerateMetricsDashboard includes coverage data in the consolidated metrics report.
  /// </remarks>
  public async Task UpdateAsync(CancellationToken cancellationToken)
  {
    var solutionDirectory = Path.GetDirectoryName(_solutionPath)
      ?? throw new InvalidOperationException($"Cannot resolve solution directory for '{_solutionPath}'.");

    // Step 1: Collect coverage (will automatically skip if AltCoverEnabled=false due to target condition)
    // This must run first to generate coverage files that will be included in the metrics dashboard
    // Use a runtime project for coverage collection as it contains the instrumentation targets
    var runtimeProjectPath = ResolveRuntimeProjectPath(solutionDirectory);
    var coverageStartInfo = CreateCoverageStartInfo(runtimeProjectPath, solutionDirectory);
    await RunProcessAsync(coverageStartInfo, "Collecting code coverage...", "Coverage collected successfully.", cancellationToken).ConfigureAwait(false);

    // Step 2: Generate metrics dashboard (includes coverage data from Step 1)
    // Use MetricsReporter.Tests project for metrics dashboard generation
    var metricsProjectPath = ResolveMetricsProjectPath(solutionDirectory);
    var startInfo = CreateStartInfo(metricsProjectPath, solutionDirectory);
    await RunProcessAsync(startInfo, "Updating metrics via GenerateMetricsDashboard...", "Metrics updated successfully.", cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Runs a process asynchronously and handles output redirection.
  /// </summary>
  /// <param name="startInfo">Process start information.</param>
  /// <param name="startMessage">Message to write before starting the process.</param>
  /// <param name="successMessage">Message to write after successful completion.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <remarks>
  /// This method is virtual to allow test classes to override it and suppress console output.
  /// </remarks>
  protected virtual async Task RunProcessAsync(ProcessStartInfo startInfo, string startMessage, string successMessage, CancellationToken cancellationToken)
  {
    using var process = new Process { StartInfo = startInfo };
    Console.WriteLine(startMessage);
    if (!process.Start())
    {
      throw new InvalidOperationException("Failed to start metrics update process.");
    }

    var stdOutTask = PumpAsync(process.StandardOutput, Console.Out, cancellationToken);
    var stdErrTask = PumpAsync(process.StandardError, Console.Error, cancellationToken);

    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
    await Task.WhenAll(stdOutTask, stdErrTask).ConfigureAwait(false);

    if (process.ExitCode != 0)
    {
      throw new InvalidOperationException($"Metrics update failed with exit code {process.ExitCode}.");
    }

    Console.WriteLine(successMessage);
  }

  /// <summary>
  /// Creates ProcessStartInfo for running the GenerateMetricsDashboard target.
  /// </summary>
  /// <param name="projectPath">Path to the project file.</param>
  /// <param name="solutionDirectory">Directory containing the solution.</param>
  /// <returns>ProcessStartInfo configured for MSBuild.</returns>
  protected virtual ProcessStartInfo CreateStartInfo(string projectPath, string solutionDirectory)
  {
    var arguments = $"msbuild \"{projectPath}\" /t:Build /p:GenerateMetricsDashboard=true /p:BuildProjectReferences=false /p:SkipMetricsReporterBuild=true /p:RoslynMetricsEnabled=true";
    return new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = arguments,
      WorkingDirectory = solutionDirectory,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };
  }

  /// <summary>
  /// Creates ProcessStartInfo for running the CollectCoverage target.
  /// </summary>
  /// <param name="projectPath">Path to the project file (for example, a runtime project).</param>
  /// <param name="solutionDirectory">Directory containing the solution.</param>
  /// <returns>ProcessStartInfo configured for MSBuild.</returns>
  /// <remarks>
  /// Uses a runtime project because it contains the instrumentation targets needed for coverage collection.
  /// Explicitly passes AltCoverEnabled=true to ensure coverage collection runs even if not set in code-metrics.props.
  /// </remarks>
  protected virtual ProcessStartInfo CreateCoverageStartInfo(string projectPath, string solutionDirectory)
  {
    var arguments = $"msbuild \"{projectPath}\" /t:CollectCoverage /p:AltCoverEnabled=true";
    return new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = arguments,
      WorkingDirectory = solutionDirectory,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };
  }

  private static string ResolveMetricsProjectPath(string solutionDirectory)
  {
    // Allow overriding the anchor test project via environment variable for solution-specific setups
    // (e.g., when the metrics tool is consumed from another repo).
    var anchorName = Environment.GetEnvironmentVariable("METRICS_TARGETS_ANCHOR_PROJECT")
      ?? Environment.GetEnvironmentVariable("METRICS_REPORTER_ANCHOR_PROJECT");

    string[] candidates = string.IsNullOrWhiteSpace(anchorName)
      ? new[]
        {
          "MetricsReporter.Tests.csproj"        // default for standalone repo
        }
      : new[]
        {
          anchorName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? anchorName
            : $"{anchorName}.csproj"
        };

    var projectPath = candidates
      .SelectMany(pattern => Directory.EnumerateFiles(solutionDirectory, pattern, SearchOption.AllDirectories))
      .FirstOrDefault();

    if (string.IsNullOrWhiteSpace(projectPath))
    {
      throw new InvalidOperationException($"Metrics anchor project file could not be located. Looked for: {string.Join(", ", candidates)}");
    }

    return projectPath;
  }

  private static string ResolveRuntimeProjectPath(string solutionDirectory)
  {
    var runtimeName = Environment.GetEnvironmentVariable("METRICS_RUNTIME_PROJECT")
      ?? Environment.GetEnvironmentVariable("METRICS_REPORTER_RUNTIME_PROJECT");

    var searchPatterns = string.IsNullOrWhiteSpace(runtimeName)
      ? new[] { "*Runtime.csproj" }
      : new[]
        {
          runtimeName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? runtimeName
            : $"{runtimeName}.csproj"
        };

    var projectPath = searchPatterns
      .SelectMany(pattern => Directory.EnumerateFiles(solutionDirectory, pattern, SearchOption.AllDirectories))
      .FirstOrDefault();

    if (string.IsNullOrWhiteSpace(projectPath))
    {
      throw new InvalidOperationException(
        "Runtime project file could not be located. Provide METRICS_RUNTIME_PROJECT or ensure a *Runtime.csproj exists.");
    }

    return projectPath;
  }

  /// <summary>
  /// Pumps data from a StreamReader to a TextWriter asynchronously.
  /// </summary>
  /// <param name="reader">The source reader.</param>
  /// <param name="destination">The destination writer.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <remarks>
  /// This method is protected to allow test classes to access it when overriding RunProcessAsync.
  /// </remarks>
  protected static async Task PumpAsync(StreamReader reader, TextWriter destination, CancellationToken cancellationToken)
  {
    var buffer = new char[4096];
    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
    {
      var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
      if (read == 0)
      {
        break;
      }

      await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
    }
  }
}

