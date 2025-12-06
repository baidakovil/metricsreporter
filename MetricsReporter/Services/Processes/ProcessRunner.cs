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

    using var process = CreateProcess(request);
    var startedAt = DateTimeOffset.UtcNow;
    var stdout = new StringBuilder();
    var stderr = new StringBuilder();
    var timedOut = false;

    if (!process.Start())
    {
      throw new InvalidOperationException($"Failed to start process '{request.FileName}'.");
    }

    var stdoutTask = ConsumeAsync(process.StandardOutput, stdout, cancellationToken);
    var stderrTask = ConsumeAsync(process.StandardError, stderr, cancellationToken);

    using var timeoutCts = new CancellationTokenSource(request.Timeout);
    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

    try
    {
      await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
      timedOut = timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
      TryKill(process);
    }

    await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
    var finishedAt = DateTimeOffset.UtcNow;

    return new ProcessRunResult(
      process.HasExited ? process.ExitCode : -1,
      timedOut,
      startedAt,
      finishedAt,
      stdout.ToString(),
      stderr.ToString());
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
}

