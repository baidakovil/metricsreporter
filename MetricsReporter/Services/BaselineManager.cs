namespace MetricsReporter.Services;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Logging;

/// <summary>
/// Manages baseline file operations: creating baseline from previous report and replacing baseline with new report (including archiving old baselines).
/// </summary>
public sealed class BaselineManager : IBaselineManager
{
  /// <summary>
  /// Creates baseline from previous report if baseline doesn't exist.
  /// </summary>
  /// <param name="previousReportPath">Path to the previous metrics report JSON file that will become the baseline.</param>
  /// <param name="baselinePath">Path to the baseline JSON file that will be created.</param>
  /// <param name="logger">Logger instance for recording operations.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <returns>
  /// <see langword="true"/> if baseline was created successfully; <see langword="false"/> if baseline already exists, report file doesn't exist, or operation failed.
  /// </returns>
  /// <remarks>
  /// This method creates baseline from previous report only if baseline doesn't exist.
  /// This allows new report to be generated with deltas calculated against the previous report.
  /// </remarks>
  public async Task<bool> CreateBaselineFromPreviousReportAsync(
      string previousReportPath,
      string baselinePath,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(previousReportPath);
    ArgumentException.ThrowIfNullOrWhiteSpace(baselinePath);
    ArgumentNullException.ThrowIfNull(logger);

    var parameters = new Services.DTO.BaselineCreationParameters(previousReportPath, baselinePath);
    return await CreateBaselineFromPreviousReportInternalAsync(parameters, logger, cancellationToken).ConfigureAwait(false);
  }

  private static async Task<bool> CreateBaselineFromPreviousReportInternalAsync(
      Services.DTO.BaselineCreationParameters parameters,
      ILogger logger,
      CancellationToken cancellationToken)
  {

    // Don't create baseline if it already exists
    if (File.Exists(parameters.BaselinePath))
    {
      logger.LogInformation($"Baseline already exists at: {parameters.BaselinePath}. Skipping creation from previous report.");
      return false;
    }

    if (!File.Exists(parameters.PreviousReportPath))
    {
      logger.LogInformation($"Previous report file not found: {parameters.PreviousReportPath}. Baseline will not be created.");
      return false;
    }

    try
    {
      // Ensure baseline directory exists
      var baselineDir = Path.GetDirectoryName(parameters.BaselinePath);
      if (!string.IsNullOrWhiteSpace(baselineDir) && !Directory.Exists(baselineDir))
      {
        Directory.CreateDirectory(baselineDir);
        logger.LogInformation($"Created baseline directory: {baselineDir}");
      }

      // Copy previous report to baseline location
      await CopyFileAsync(parameters.PreviousReportPath, parameters.BaselinePath, cancellationToken).ConfigureAwait(false);
      logger.LogInformation($"Baseline created from previous report: {parameters.BaselinePath} <- {parameters.PreviousReportPath}");

      return true;
    }
    catch (Exception ex)
    {
      logger.LogError($"Failed to create baseline from previous report: {ex.Message}", ex);
      return false;
    }
  }

  /// <summary>
  /// Replaces the baseline file by archiving the old baseline (if exists) and copying the new report to baseline location.
  /// </summary>
  /// <param name="reportPath">Path to the metrics report JSON file that will become the baseline.</param>
  /// <param name="baselinePath">Path to the baseline JSON file that will be created or replaced.</param>
  /// <param name="storagePath">Directory path where the old baseline will be archived with a timestamp (if baseline exists).</param>
  /// <param name="logger">Logger instance for recording operations.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <returns>
  /// <see langword="true"/> if baseline was created or replaced successfully; <see langword="false"/> if report file doesn't exist or operation failed.
  /// </returns>
  /// <remarks>
  /// This method performs the following steps:
  /// 1. Validates that the report file exists (returns false if not).
  /// 2. If old baseline exists, it is moved to storage directory with a timestamp suffix for unique filename.
  /// 3. The new report file is copied (not moved) to the baseline location to preserve the original report.
  /// 4. All operations are logged for traceability.
  /// 
  /// Note: This method does not compare files. The report is always copied to baseline location if it exists.
  /// </remarks>
  public async Task<bool> ReplaceBaselineAsync(
      string reportPath,
      string baselinePath,
      string? storagePath,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);
    ArgumentException.ThrowIfNullOrWhiteSpace(baselinePath);
    ArgumentNullException.ThrowIfNull(logger);

    var parameters = new Services.DTO.BaselineReplacementParameters(reportPath, baselinePath, storagePath);
    return await ReplaceBaselineInternalAsync(parameters, logger, cancellationToken).ConfigureAwait(false);
  }

  private static async Task<bool> ReplaceBaselineInternalAsync(
      Services.DTO.BaselineReplacementParameters parameters,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    if (!File.Exists(parameters.ReportPath))
    {
      logger.LogError($"Report file not found for baseline replacement: {parameters.ReportPath}");
      return false;
    }

    try
    {
      // Archive old baseline if it exists
      if (File.Exists(parameters.BaselinePath))
      {
        await ArchiveOldBaselineAsync(parameters.BaselinePath, parameters.StoragePath, logger, cancellationToken).ConfigureAwait(false);
      }

      // Ensure baseline directory exists
      var baselineDir = Path.GetDirectoryName(parameters.BaselinePath);
      if (!string.IsNullOrWhiteSpace(baselineDir) && !Directory.Exists(baselineDir))
      {
        Directory.CreateDirectory(baselineDir);
        logger.LogInformation($"Created baseline directory: {baselineDir}");
      }

      // Copy new report to baseline location (copy to preserve original report)
      await CopyFileAsync(parameters.ReportPath, parameters.BaselinePath, cancellationToken).ConfigureAwait(false);
      logger.LogInformation($"Baseline replaced: {parameters.BaselinePath} <- {parameters.ReportPath}");

      return true;
    }
    catch (Exception ex)
    {
      logger.LogError($"Failed to replace baseline: {ex.Message}", ex);
      return false;
    }
  }

  /// <summary>
  /// Archives the old baseline file to storage directory with a timestamp suffix.
  /// </summary>
  /// <param name="baselinePath">Path to the old baseline file.</param>
  /// <param name="storagePath">Directory where the archived baseline will be stored.</param>
  /// <param name="logger">Logger instance for recording operations.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <remarks>
  /// The archived file name format: metrics-baseline-YYYYMMDD-HHMMSS.json
  /// Uses local time (not UTC) as specified in requirements.
  /// </remarks>
  private static Task ArchiveOldBaselineAsync(
      string baselinePath,
      string? storagePath,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(storagePath))
    {
      logger.LogInformation("Storage path not specified, skipping baseline archive.");
      return Task.CompletedTask;
    }

    cancellationToken.ThrowIfCancellationRequested();

    try
    {
      // Ensure storage directory exists
      if (!Directory.Exists(storagePath))
      {
        Directory.CreateDirectory(storagePath);
        logger.LogInformation($"Created storage directory: {storagePath}");
      }

      cancellationToken.ThrowIfCancellationRequested();

      // Generate timestamp using local time (not UTC)
      var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
      var baselineFileName = Path.GetFileName(baselinePath);
      var baselineNameWithoutExt = Path.GetFileNameWithoutExtension(baselineFileName);
      var baselineExt = Path.GetExtension(baselineFileName);

      var archivedFileName = $"{baselineNameWithoutExt}-{timestamp}{baselineExt}";
      var archivedPath = Path.Combine(storagePath, archivedFileName);

      // Move (not copy) the old baseline to archive location
      File.Move(baselinePath, archivedPath);
      logger.LogInformation($"Old baseline archived: {baselinePath} -> {archivedPath}");
      return Task.CompletedTask;
    }
    catch (Exception ex)
    {
      logger.LogError($"Failed to archive old baseline: {ex.Message}", ex);
      throw;
    }
  }

  /// <summary>
  /// Copies a file from source to destination asynchronously.
  /// </summary>
  /// <param name="sourcePath">Path to the source file.</param>
  /// <param name="destinationPath">Path to the destination file.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  private static async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
  {
    await using var sourceStream = File.OpenRead(sourcePath);
    await using var destinationStream = File.Create(destinationPath);

    await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
  }
}


