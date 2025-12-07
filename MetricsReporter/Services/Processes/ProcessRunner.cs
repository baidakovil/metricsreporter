using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MetricsReporter.Services.Processes;

/// <summary>
/// Default implementation of <see cref="IProcessRunner"/> that captures stdout/stderr.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
  /// <inheritdoc />
  public async Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);

    using var execution = ProcessExecutionScope.Start(request, cancellationToken);
    var waitResult = await execution.WaitForExitAsync(request.Timeout, cancellationToken).ConfigureAwait(false);

    return execution.ToResult(waitResult);
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

