using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MetricsReporter.Services.Processes;

/// <summary>
/// Runs external processes and captures their outputs.
/// </summary>
public interface IProcessRunner
{
  /// <summary>
  /// Executes the specified process request.
  /// </summary>
  /// <param name="request">Process configuration.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Execution result.</returns>
  Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Describes the process to run.
/// </summary>
public sealed record ProcessRunRequest
{
  /// <summary>
  /// Initializes a new instance of the <see cref="ProcessRunRequest"/> class.
  /// </summary>
  /// <param name="fileName">Process executable.</param>
  /// <param name="arguments">Process arguments.</param>
  /// <param name="workingDirectory">Working directory. Defaults to current directory when null or whitespace.</param>
  /// <param name="timeout">Timeout for execution.</param>
  /// <param name="environmentVariables">Optional environment variable overrides.</param>
  public ProcessRunRequest(
    string fileName,
    string arguments,
    string? workingDirectory,
    TimeSpan timeout,
    IDictionary<string, string?>? environmentVariables = null)
  {
    FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
    Arguments = arguments ?? string.Empty;
    WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
      ? Environment.CurrentDirectory
      : workingDirectory;
    Timeout = timeout;
    EnvironmentVariables = environmentVariables;
  }

  /// <summary>
  /// Gets the executable to run.
  /// </summary>
  public string FileName { get; }

  /// <summary>
  /// Gets the argument string.
  /// </summary>
  public string Arguments { get; }

  /// <summary>
  /// Gets the working directory.
  /// </summary>
  public string WorkingDirectory { get; }

  /// <summary>
  /// Gets the timeout.
  /// </summary>
  public TimeSpan Timeout { get; }

  /// <summary>
  /// Gets environment variables to apply.
  /// </summary>
  public IDictionary<string, string?>? EnvironmentVariables { get; }
}

/// <summary>
/// Captures the output of a process execution.
/// </summary>
public sealed record ProcessRunResult(
  int ExitCode,
  bool TimedOut,
  DateTimeOffset StartedAt,
  DateTimeOffset FinishedAt,
  string StandardOutput,
  string StandardError);

