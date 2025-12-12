namespace MetricsReporter.Services;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Manages baseline file operations: creating baseline from previous report and replacing baseline with new report.
/// </summary>
internal interface IBaselineManager
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
  Task<bool> CreateBaselineFromPreviousReportAsync(
      string previousReportPath,
      string baselinePath,
      ILogger logger,
      CancellationToken cancellationToken);

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
  Task<bool> ReplaceBaselineAsync(
      string reportPath,
      string baselinePath,
      string? storagePath,
      ILogger logger,
      CancellationToken cancellationToken);
}


