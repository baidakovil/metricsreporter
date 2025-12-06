namespace MetricsReporter.Services;

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Model;
using MetricsReporter.Serialization;

/// <summary>
/// Persists JSON and HTML reports to disk.
/// </summary>
public sealed class ReportWriter
{
  /// <summary>
  /// Writes the JSON report to disk.
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Microsoft.Maintainability",
      "CA1506:Avoid excessive class coupling",
      Justification = "JSON report writer performs file I/O and JSON serialization; further decomposition would require wrapper methods which are prohibited by refactoring rules.")]
  public static async Task WriteJsonAsync(MetricsReport report, string path, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(report);
    ArgumentException.ThrowIfNullOrWhiteSpace(path);

    EnsureDirectory(path);

    await using var stream = File.Create(path);
    await JsonSerializer.SerializeAsync(stream, report, JsonSerializerOptionsFactory.Create(), cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Writes the HTML representation of the report to disk.
  /// </summary>
  public static async Task WriteHtmlAsync(string html, string path, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(html);
    ArgumentException.ThrowIfNullOrWhiteSpace(path);

    EnsureDirectory(path);
    await File.WriteAllTextAsync(path, html, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
  }

  private static void EnsureDirectory(string path)
  {
    var directory = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(directory))
    {
      Directory.CreateDirectory(directory);
    }
  }
}


