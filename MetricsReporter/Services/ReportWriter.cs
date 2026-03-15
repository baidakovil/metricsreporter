namespace MetricsReporter.Services;

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Model;
using MetricsReporter.Rendering;
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
  /// Generates an HTML report from the specified metrics report and writes it to disk.
  /// </summary>
  /// <param name="report">The metrics report to render as HTML.</param>
  /// <param name="path">The file path where the HTML report will be written.</param>
  /// <param name="coverageHtmlDir">Optional path to HTML coverage reports directory for generating hyperlinks.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  public static async Task WriteHtmlReportAsync(
      MetricsReport report,
      string path,
      string? coverageHtmlDir,
      CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(report);
    ArgumentException.ThrowIfNullOrWhiteSpace(path);

    var html = HtmlReportGenerator.Generate(report, coverageHtmlDir);
    await WriteHtmlAsync(html, path, cancellationToken).ConfigureAwait(false);
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


