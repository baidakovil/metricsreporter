namespace MetricsReporter.Rendering;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using MetricsReporter.Model;
using MetricsReporter.Serialization;

/// <summary>
/// Generates the HTML header section for the metrics report.
/// Includes title, metadata, legend, and action controls.
/// </summary>
internal static class HtmlHeaderGenerator
{
  /// <summary>
  /// Generates the HTML header section with title, metadata, legend, and controls.
  /// </summary>
  /// <param name="report">The metrics report.</param>
  /// <returns>HTML markup for the header section.</returns>
  public static string Generate(MetricsReport report)
  {
    var builder = new StringBuilder();

    // Title
    builder.AppendLine($"<h1>Metrics Report for {WebUtility.HtmlEncode(report.Solution.Name)}</h1>");

    // Metadata
    builder.AppendLine("<section class=\"meta\"> ");
    // Convert UTC time to local time for display
    // WHY: Users expect to see time in their local timezone, not UTC, for better readability
    var localTime = report.Metadata.GeneratedAtUtc.ToLocalTime();
    builder.AppendLine($"  <p class=\"meta-summary\"><strong>Generated at:</strong> {localTime:yyyy-MM-dd HH:mm:ss}<span class=\"meta-toggle\"> ▶</span></p>");

    var excludedAssemblyNames = string.IsNullOrWhiteSpace(report.Metadata.ExcludedAssemblyNames)
        ? "(none)"
        : WebUtility.HtmlEncode(report.Metadata.ExcludedAssemblyNames);
    var excludedTypePatterns = string.IsNullOrWhiteSpace(report.Metadata.ExcludedTypeNamePatterns)
        ? "(none)"
        : WebUtility.HtmlEncode(report.Metadata.ExcludedTypeNamePatterns);
    var excludedMemberPatterns = string.IsNullOrWhiteSpace(report.Metadata.ExcludedMemberNamesPatterns)
        ? "(none)"
        : WebUtility.HtmlEncode(report.Metadata.ExcludedMemberNamesPatterns);

    builder.AppendLine("  <div class=\"meta-details\" style=\"display: none;\">");
    if (!string.IsNullOrWhiteSpace(report.Metadata.BaselineReference))
    {
      builder.AppendLine($"    <p><strong>Baseline:</strong> {WebUtility.HtmlEncode(report.Metadata.BaselineReference)}</p>");
    }

    var thresholdsPath = report.Metadata.Paths.Thresholds;
    var thresholdsDisplay = string.IsNullOrWhiteSpace(thresholdsPath)
        ? "(inline thresholds)"
        : thresholdsPath;

    builder.AppendLine("    <div class=\"meta-section\">");
    builder.AppendLine("      <p class=\"section-title\">Paths</p>");
    builder.AppendLine($"      <p><strong>Metrics JSON:</strong> {WebUtility.HtmlEncode(report.Metadata.Paths.Report)}</p>");
    if (!string.IsNullOrWhiteSpace(report.Metadata.Paths.Baseline))
    {
      builder.AppendLine($"      <p><strong>Baseline JSON:</strong> {WebUtility.HtmlEncode(report.Metadata.Paths.Baseline)}</p>");
    }
    builder.AppendLine($"      <p><strong>Thresholds JSON:</strong> {WebUtility.HtmlEncode(thresholdsDisplay)}</p>");
    builder.AppendLine("    </div>");

    // builder.AppendLine("    <div class=\"meta-section-divider\" aria-hidden=\"true\"></div>");

    builder.AppendLine("    <div class=\"meta-section\">");
    builder.AppendLine("      <p class=\"section-title\">Excluded from report</p>");
    builder.AppendLine($"      <p><strong>Assemblies:</strong> {excludedAssemblyNames}</p>");
    builder.AppendLine($"      <p><strong>Types:</strong> {excludedTypePatterns}</p>");
    builder.AppendLine($"      <p><strong>Members:</strong> {excludedMemberPatterns}</p>");
    builder.AppendLine("    </div>");

    // builder.AppendLine("    <div class=\"meta-section-divider\" aria-hidden=\"true\"></div>");

    var stats = CalculateStats(report);
    builder.AppendLine("    <div class=\"meta-section\">");
    builder.AppendLine("      <p class=\"section-title\">Stats</p>");
    builder.AppendLine($"      <p><strong>{FormatCount(stats.Total)}</strong> overall symbols (rows)</p>");
    builder.AppendLine($"      <p><strong>{FormatCount(stats.NoMetric)}</strong> no-metric symbols ({FormatPercent(stats.NoMetric, stats.Total)}{FormatDelta(stats.NoMetric, stats.BaselineNoMetric, stats.Total, stats.BaselineTotal)})</p>");
    builder.AppendLine($"      <p><strong>{FormatCount(stats.Clear)}</strong> clear symbols ({FormatPercent(stats.Clear, stats.Total)}{FormatDelta(stats.Clear, stats.BaselineClear, stats.Total, stats.BaselineTotal)})</p>");
    builder.AppendLine($"      <p><strong>{FormatCount(stats.Warning)}</strong> warning symbols ({FormatPercent(stats.Warning, stats.Total)}{FormatDelta(stats.Warning, stats.BaselineWarning, stats.Total, stats.BaselineTotal)})</p>");
    builder.AppendLine($"      <p><strong>{FormatCount(stats.Error)}</strong> error symbols ({FormatPercent(stats.Error, stats.Total)}{FormatDelta(stats.Error, stats.BaselineError, stats.Total, stats.BaselineTotal)})</p>");
    builder.AppendLine("    </div>");
    builder.AppendLine("    <div class=\"meta-section hotkeys-summary\">");
    builder.AppendLine("      <p class=\"section-title\">Hotkeys</p>");
    builder.AppendLine("      <p><span class=\"hotkey-pair\"><strong>Filter to New</strong> N</span><span class=\"hotkey-pair\"><strong>Filter to Changes</strong> C</span><span class=\"hotkey-pair\"><strong>Detail</strong> ↑ D ↓ S</span><span class=\"hotkey-pair\"><strong>Awareness</strong> ↑ A ↓ Z</span><span class=\"hotkey-pair\"><strong>Focus on Filter</strong> F</span><span class=\"hotkey-pair\"><strong>Apply Filter</strong> Enter</span><span class=\"hotkey-pair\"><strong>Clear Filter</strong> X</span><span class=\"hotkey-pair\"><strong>Reset</strong> Q</span><span class=\"hotkey-pair\"><strong>Expand/Collapse</strong> ↑ E ↓ R</span></p>");
    builder.AppendLine("    </div>");
    builder.AppendLine("  </div>");
    builder.AppendLine("</section>");

    return builder.ToString();
  }

  private static Stats CalculateStats(MetricsReport report)
  {
    if (report.Solution is null)
    {
      return Stats.Empty;
    }

    var current = SummarizeSolution(report.Solution);
    var baseline = LoadBaselineSummary(report.Metadata.Paths.Baseline);

    return new Stats(
        current.Total,
        current.NoMetric,
        current.Clear,
        current.Warning,
        current.Error,
        baseline.Total,
        baseline.NoMetric,
        baseline.Clear,
        baseline.Warning,
        baseline.Error);
  }

  private static bool AllValuesUnknown(IDictionary<MetricIdentifier, MetricValue> metrics)
      => metrics.Values.All(static v => v is null || !v.Value.HasValue);

  private static IEnumerable<MetricsNode> EnumerateSymbols(SolutionMetricsNode solution)
  {
    foreach (var assembly in solution.Assemblies)
    {
      yield return assembly;
      foreach (var @namespace in assembly.Namespaces)
      {
        yield return @namespace;
        foreach (var type in @namespace.Types)
        {
          yield return type;
          foreach (var member in type.Members)
          {
            yield return member;
          }
        }
      }
    }
  }

  private static SummaryCounts SummarizeSolution(SolutionMetricsNode solution)
  {
    var summary = SummaryCounts.Empty;
    foreach (var node in EnumerateSymbols(solution))
    {
      summary = summary with { Total = summary.Total + 1 };
      var metrics = node.Metrics;

      if (metrics.Count == 0 || AllValuesUnknown(metrics))
      {
        summary = summary with { NoMetric = summary.NoMetric + 1 };
        continue;
      }

      var hasError = metrics.Values.Any(static v => v is { Status: ThresholdStatus.Error });
      if (hasError)
      {
        summary = summary with { Error = summary.Error + 1 };
        continue;
      }

      var hasWarning = metrics.Values.Any(static v => v is { Status: ThresholdStatus.Warning });
      if (hasWarning)
      {
        summary = summary with { Warning = summary.Warning + 1 };
        continue;
      }

      summary = summary with { Clear = summary.Clear + 1 };
    }

    return summary;
  }

  private static SummaryCounts LoadBaselineSummary(string? baselinePath)
  {
    if (string.IsNullOrWhiteSpace(baselinePath))
    {
      return SummaryCounts.Empty;
    }

    try
    {
      if (!File.Exists(baselinePath))
      {
        return SummaryCounts.Empty;
      }

      using var stream = File.OpenRead(baselinePath);
      var baselineReport = JsonSerializer.Deserialize<MetricsReport>(stream, JsonSerializerOptionsFactory.Create());

      if (baselineReport?.Solution is null)
      {
        return SummaryCounts.Empty;
      }

      return SummarizeSolution(baselineReport.Solution);
    }
    catch
    {
      return SummaryCounts.Empty;
    }
  }

  private static string FormatPercent(int count, int total)
  {
    var rounded = RoundPercent(count, total);
    return $"<strong>{rounded}%</strong>";
  }

  private static string FormatCount(int value)
      => value.ToString(CultureInfo.InvariantCulture);

  private static int RoundPercent(int count, int total)
  {
    if (total == 0)
    {
      return 0;
    }

    var percentage = (double)count / total * 100d;
    return (int)Math.Round(percentage, MidpointRounding.AwayFromZero);
  }

  private static string FormatDelta(int currentCount, int baselineCount, int currentTotal, int baselineTotal)
  {
    if (baselineTotal == 0)
    {
      return string.Empty;
    }

    var currentPercent = RoundPercent(currentCount, currentTotal);
    var baselinePercent = RoundPercent(baselineCount, baselineTotal);
    var delta = currentPercent - baselinePercent;

    if (delta == 0)
    {
      return string.Empty;
    }

    var sign = delta > 0 ? "+" : string.Empty;
    var cssClass = delta > 0 ? "delta-positive" : "delta-negative";
    return $"<sup class=\"{cssClass}\">{sign}{delta}%</sup>";
  }

  private readonly record struct SummaryCounts(int Total, int NoMetric, int Clear, int Warning, int Error)
  {
    public static SummaryCounts Empty { get; } = new(0, 0, 0, 0, 0);
  }

  private readonly record struct Stats(
      int Total,
      int NoMetric,
      int Clear,
      int Warning,
      int Error,
      int BaselineTotal,
      int BaselineNoMetric,
      int BaselineClear,
      int BaselineWarning,
      int BaselineError)
  {
    public static Stats Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
  }
}

