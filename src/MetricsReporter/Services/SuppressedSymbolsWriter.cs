namespace MetricsReporter.Services;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Model;
using MetricsReporter.Serialization;

/// <summary>
/// Persists <see cref="SuppressedSymbolsReport"/> instances to JSON files.
/// </summary>
internal static class SuppressedSymbolsWriter
{
  /// <summary>
  /// Writes the specified suppressed symbols report to disk using the standard
  /// JSON serialization settings for the Metrics Reporter.
  /// </summary>
  /// <param name="report">Report to serialize. Cannot be null.</param>
  /// <param name="path">Destination file path. Cannot be null or empty.</param>
  /// <param name="cancellationToken">Cancellation token for I/O operations.</param>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Microsoft.Maintainability",
      "CA1506:Avoid excessive class coupling",
      Justification = "Method performs JSON serialization and file I/O; further decomposition would require wrapper methods which are prohibited by refactoring rules.")]
  public static async Task WriteAsync(
      SuppressedSymbolsReport report,
      string path,
      CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(report);
    if (string.IsNullOrWhiteSpace(path))
    {
      throw new ArgumentException("Suppressed symbols path must be a non-empty string.", nameof(path));
    }

    var options = JsonSerializerOptionsFactory.Create();

    await using var stream = File.Create(path);
    await System.Text.Json.JsonSerializer.SerializeAsync(stream, report, options, cancellationToken)
        .ConfigureAwait(false);
  }
}



