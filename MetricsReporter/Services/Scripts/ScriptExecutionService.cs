using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Logging;
using MetricsReporter.Services.Processes;
using Microsoft.Extensions.Logging;

namespace MetricsReporter.Services.Scripts;

/// <summary>
/// Executes PowerShell scripts sequentially and logs start/finish events.
/// </summary>
public sealed class ScriptExecutionService
{
  private const string PowerShellExecutable = "pwsh";
  private readonly IProcessRunner _processRunner;

  /// <summary>
  /// Initializes a new instance of the <see cref="ScriptExecutionService"/> class.
  /// </summary>
  /// <param name="processRunner">Process runner used to execute scripts.</param>
  public ScriptExecutionService(IProcessRunner processRunner)
  {
    _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
  }

  /// <summary>
  /// Runs scripts in order and stops on first failure.
  /// </summary>
  /// <param name="scripts">Script paths.</param>
  /// <param name="context">Execution context.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Execution summary.</returns>
  public async Task<ScriptExecutionResult> RunAsync(
    IEnumerable<string> scripts,
    ScriptExecutionContext context,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scripts);
    ArgumentNullException.ThrowIfNull(context);

    foreach (var script in scripts)
    {
      if (string.IsNullOrWhiteSpace(script))
      {
        continue;
      }

      var failure = await ExecuteScriptAsync(script, context, cancellationToken).ConfigureAwait(false);
      if (failure is not null)
      {
        return failure;
      }
    }

    return ScriptExecutionResult.Success();
  }

  private async Task<ScriptExecutionResult?> ExecuteScriptAsync(
    string script,
    ScriptExecutionContext context,
    CancellationToken cancellationToken)
  {
    var resolvedPath = ResolvePath(script, context.WorkingDirectory);
    var missingScript = ValidateScriptExists(resolvedPath);
    if (missingScript is not null)
    {
      return missingScript;
    }

    using var scope = context.Logger.BeginScope(new Dictionary<string, object?>
    {
      ["scriptPath"] = resolvedPath,
      ["workingDirectory"] = context.WorkingDirectory
    });

    context.Logger.LogInformation(
      "Starting script {ScriptPath} in {WorkingDirectory}. TimeoutSeconds={TimeoutSeconds}",
      resolvedPath,
      context.WorkingDirectory,
      context.Timeout.TotalSeconds);

    return await TryRunProcessAsync(resolvedPath, context, cancellationToken).ConfigureAwait(false);
  }

  private Task<ProcessRunResult> RunProcessAsync(
    string resolvedPath,
    ScriptExecutionContext context,
    CancellationToken cancellationToken)
  {
    var request = new ProcessRunRequest(
      PowerShellExecutable,
      $"-File \"{resolvedPath}\"",
      context.WorkingDirectory,
      context.Timeout,
      environmentVariables: null);

    return _processRunner.RunAsync(request, cancellationToken);
  }

  private static ScriptExecutionResult? EvaluateRunResult(
    string resolvedPath,
    ProcessRunResult result,
    ScriptExecutionContext context)
  {
    var duration = result.FinishedAt - result.StartedAt;
    if (result.TimedOut)
    {
      var message = $"Script '{resolvedPath}' timed out after {duration.TotalSeconds:N0}s.";
      context.Logger.LogError("Script {ScriptPath} timed out after {DurationSeconds:F0}s", resolvedPath, duration.TotalSeconds);
      LogOutputOnFailure(context.Logger, result, context.LogTruncationLimit);
      return ScriptExecutionResult.Failed(resolvedPath, MetricsReporterExitCode.ValidationError, message, result);
    }

    if (result.ExitCode != 0)
    {
      var message = $"Script '{resolvedPath}' failed with exit code {result.ExitCode} (duration {duration.TotalSeconds:N0}s).";
      context.Logger.LogError(
        "Script {ScriptPath} failed with exit code {ExitCode} after {DurationSeconds:F0}s",
        resolvedPath,
        result.ExitCode,
        duration.TotalSeconds);
      LogOutputOnFailure(context.Logger, result, context.LogTruncationLimit);
      return ScriptExecutionResult.Failed(resolvedPath, MetricsReporterExitCode.ValidationError, message, result);
    }

    return null;
  }

  private static string ResolvePath(string script, string workingDirectory)
  {
    if (Path.IsPathRooted(script))
    {
      return Path.GetFullPath(script);
    }

    return Path.GetFullPath(Path.Combine(workingDirectory, script));
  }

  private static void LogOutputOnFailure(ILogger logger, ProcessRunResult result, int logTruncationLimit)
  {
    if (logTruncationLimit <= 0)
    {
      logTruncationLimit = LogTruncator.DefaultLimit;
    }

    if (!string.IsNullOrWhiteSpace(result.StandardOutput))
    {
      var truncatedStdout = LogTruncator.Truncate(result.StandardOutput, logTruncationLimit);
      logger.LogError("stdout (truncated): {Stdout}", truncatedStdout);
    }

    if (!string.IsNullOrWhiteSpace(result.StandardError))
    {
      var truncatedStderr = LogTruncator.Truncate(result.StandardError, logTruncationLimit);
      logger.LogError("stderr (truncated): {Stderr}", truncatedStderr);
    }
  }

  private static bool IsProcessStartFailure(Exception exception)
    => exception is InvalidOperationException or Win32Exception or IOException;

  private static ScriptExecutionResult? ValidateScriptExists(string resolvedPath)
  {
    if (File.Exists(resolvedPath))
    {
      return null;
    }

    return ScriptExecutionResult.Failed(resolvedPath, MetricsReporterExitCode.ValidationError, $"Script not found: {resolvedPath}");
  }

  private async Task<ScriptExecutionResult?> TryRunProcessAsync(
    string resolvedPath,
    ScriptExecutionContext context,
    CancellationToken cancellationToken)
  {
    ProcessRunResult result;
    try
    {
      result = await RunProcessAsync(resolvedPath, context, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex) when (IsProcessStartFailure(ex))
    {
      context.Logger.LogError(ex, "Failed to start script {ScriptPath}", resolvedPath);
      return ScriptExecutionResult.Failed(resolvedPath, MetricsReporterExitCode.ValidationError, ex.Message);
    }

    var failure = EvaluateRunResult(resolvedPath, result, context);
    if (failure is not null)
    {
      return failure;
    }

    LogSuccess(context, resolvedPath, result);
    return null;
  }

  private static void LogSuccess(ScriptExecutionContext context, string resolvedPath, ProcessRunResult result)
  {
    var duration = result.FinishedAt - result.StartedAt;
    context.Logger.LogInformation(
      "Script {ScriptPath} completed in {DurationSeconds:N0}s with exit code {ExitCode}",
      resolvedPath,
      duration.TotalSeconds,
      result.ExitCode);
  }
}

/// <summary>
/// Describes the execution context for scripts.
/// </summary>
public sealed record ScriptExecutionContext
{
  /// <summary>
  /// Initializes a new instance of the <see cref="ScriptExecutionContext"/> class.
  /// </summary>
  /// <param name="workingDirectory">Working directory.</param>
  /// <param name="timeout">Timeout per script.</param>
  /// <param name="logTruncationLimit">Maximum number of characters to log from stdout/stderr on failure.</param>
  /// <param name="logger">Logger used for diagnostics.</param>
  public ScriptExecutionContext(string workingDirectory, TimeSpan timeout, int logTruncationLimit, ILogger logger)
  {
    WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
      ? Environment.CurrentDirectory
      : workingDirectory;
    Timeout = timeout;
    LogTruncationLimit = logTruncationLimit;
    Logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <summary>
  /// Gets the working directory.
  /// </summary>
  public string WorkingDirectory { get; }

  /// <summary>
  /// Gets the timeout.
  /// </summary>
  public TimeSpan Timeout { get; }

  /// <summary>
  /// Gets the log truncation limit.
  /// </summary>
  public int LogTruncationLimit { get; }

  /// <summary>
  /// Gets the logger instance.
  /// </summary>
  public ILogger Logger { get; }
}

/// <summary>
/// Result of a script execution pipeline.
/// </summary>
public sealed class ScriptExecutionResult
{
  private ScriptExecutionResult(
    bool succeeded,
    string? failedScript,
    MetricsReporterExitCode exitCode,
    string? errorMessage,
    ProcessRunResult? processResult)
  {
    IsSuccess = succeeded;
    FailedScript = failedScript;
    ExitCode = exitCode;
    ErrorMessage = errorMessage;
    ProcessResult = processResult;
  }

  /// <summary>
  /// Gets a value indicating whether all scripts succeeded.
  /// </summary>
  public bool IsSuccess { get; }

  /// <summary>
  /// Gets the script that failed, if any.
  /// </summary>
  public string? FailedScript { get; }

  /// <summary>
  /// Gets the exit code to return to the caller.
  /// </summary>
  public MetricsReporterExitCode ExitCode { get; }

  /// <summary>
  /// Gets the error message describing the failure.
  /// </summary>
  public string? ErrorMessage { get; }

  /// <summary>
  /// Gets the process result for the failed script.
  /// </summary>
  public ProcessRunResult? ProcessResult { get; }

  /// <summary>
  /// Creates a successful result.
  /// </summary>
  /// <returns>A successful result.</returns>
  public static ScriptExecutionResult Success()
    => new(true, null, MetricsReporterExitCode.Success, null, null);

  /// <summary>
  /// Creates a failed result.
  /// </summary>
  /// <param name="script">Script that failed.</param>
  /// <param name="exitCode">Exit code to return.</param>
  /// <param name="errorMessage">Error message.</param>
  /// <param name="result">Process execution details.</param>
  /// <returns>A failed result.</returns>
  public static ScriptExecutionResult Failed(
    string script,
    MetricsReporterExitCode exitCode,
    string errorMessage,
    ProcessRunResult? result = null)
    => new(false, script, exitCode, errorMessage, result);
}

