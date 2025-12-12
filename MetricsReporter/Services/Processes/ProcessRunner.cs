using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Logging;
using Microsoft.Extensions.Logging;

namespace MetricsReporter.Services.Processes;

/// <summary>
/// Default implementation of <see cref="IProcessRunner"/> that captures stdout/stderr.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
  private readonly ILogger<ProcessRunner> _logger;

  /// <summary>
  /// Initializes a new instance of the <see cref="ProcessRunner"/> class.
  /// </summary>
  /// <param name="logger">Logger used to record process start/finish events.</param>
  public ProcessRunner(ILogger<ProcessRunner> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <inheritdoc />
  public async Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);

    _logger.LogDebug(
      "Starting process {FileName} {Arguments}",
      request.FileName,
      request.Arguments);

    using var execution = ProcessExecutionScope.Start(request, cancellationToken);
    var waitResult = await execution.WaitForExitAsync(request.Timeout, cancellationToken).ConfigureAwait(false);

    var result = execution.ToResult(waitResult);
    LogCompletion(request, result);
    return result;
  }

  private void LogCompletion(ProcessRunRequest request, ProcessRunResult result)
  {
    var duration = result.FinishedAt - result.StartedAt;
    if (result.TimedOut)
    {
      _logger.LogWarning(
        "Process {FileName} timed out after {DurationSeconds:F0}s (cwd: {WorkingDirectory})",
        request.FileName,
        duration.TotalSeconds,
        request.WorkingDirectory);
    }
    else
    {
      _logger.LogDebug(
        "Process {FileName} exited with code {ExitCode} in {DurationSeconds:F0}s",
        request.FileName,
        result.ExitCode,
        duration.TotalSeconds);
    }

    if (result.ExitCode != 0 || result.TimedOut)
    {
      LogLimitedBuffer("stdout", result.StandardOutput);
      LogLimitedBuffer("stderr", result.StandardError);
    }
  }

  private void LogLimitedBuffer(string name, string content)
  {
    if (string.IsNullOrWhiteSpace(content))
    {
      return;
    }

    var truncated = LogTruncator.Truncate(content, LogTruncator.DefaultLimit);
    _logger.LogDebug("{Stream} (truncated): {Content}", name, truncated);
  }

  private static Process CreateProcess(ProcessRunRequest request)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = request.FileName,
      Arguments = request.Arguments,
      WorkingDirectory = request.WorkingDirectory,
      RedirectStandardError = true,
      RedirectStandardOutput = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    if (request.EnvironmentVariables is not null)
    {
      foreach (var kvp in request.EnvironmentVariables)
      {
        if (string.IsNullOrWhiteSpace(kvp.Key))
        {
          continue;
        }

        startInfo.Environment[kvp.Key] = kvp.Value;
      }
    }

    return new Process { StartInfo = startInfo };
  }

  private static async Task ConsumeAsync(System.IO.StreamReader reader, StringBuilder sink, CancellationToken cancellationToken)
  {
    char[] buffer = new char[4096];
    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
    {
      var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
      if (read == 0)
      {
        break;
      }

      sink.Append(buffer, 0, read);
    }
  }

  private static void TryKill(Process process)
  {
    try
    {
      if (!process.HasExited)
      {
        process.Kill(entireProcessTree: true);
      }
    }
    catch
    {
      // Best-effort cleanup; swallow exceptions to avoid masking the original timeout/cancellation.
    }
  }

  private readonly record struct ProcessWaitResult(DateTimeOffset FinishedAt, bool TimedOut);

  private sealed class ProcessExecutionScope : IDisposable
  {
    private readonly Process _process;
    private readonly StringBuilder _stdout;
    private readonly StringBuilder _stderr;
    private readonly Task _stdoutTask;
    private readonly Task _stderrTask;
    private readonly DateTimeOffset _startedAt;

    private ProcessExecutionScope(
      Process process,
      StringBuilder stdout,
      StringBuilder stderr,
      Task stdoutTask,
      Task stderrTask,
      DateTimeOffset startedAt)
    {
      _process = process;
      _stdout = stdout;
      _stderr = stderr;
      _stdoutTask = stdoutTask;
      _stderrTask = stderrTask;
      _startedAt = startedAt;
    }

    public static ProcessExecutionScope Start(ProcessRunRequest request, CancellationToken cancellationToken)
    {
      var process = CreateProcess(request);
      var startedAt = DateTimeOffset.UtcNow;

      if (!process.Start())
      {
        throw new InvalidOperationException($"Failed to start process '{request.FileName}'.");
      }

      var stdout = new StringBuilder();
      var stderr = new StringBuilder();
      var stdoutTask = ConsumeAsync(process.StandardOutput, stdout, cancellationToken);
      var stderrTask = ConsumeAsync(process.StandardError, stderr, cancellationToken);

      return new ProcessExecutionScope(process, stdout, stderr, stdoutTask, stderrTask, startedAt);
    }

    public async Task<ProcessWaitResult> WaitForExitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
      using var timeoutCts = new CancellationTokenSource(timeout);
      using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

      try
      {
        await _process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        var timedOut = timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
        TryKill(_process);
        await Task.WhenAll(_stdoutTask, _stderrTask).ConfigureAwait(false);
        return new ProcessWaitResult(DateTimeOffset.UtcNow, timedOut);
      }

      await Task.WhenAll(_stdoutTask, _stderrTask).ConfigureAwait(false);
      return new ProcessWaitResult(DateTimeOffset.UtcNow, false);
    }

    public ProcessRunResult ToResult(ProcessWaitResult waitResult)
    {
      return new ProcessRunResult(
        _process.HasExited ? _process.ExitCode : -1,
        waitResult.TimedOut,
        _startedAt,
        waitResult.FinishedAt,
        _stdout.ToString(),
        _stderr.ToString());
    }

    public void Dispose()
    {
      _process.Dispose();
    }
  }
}

