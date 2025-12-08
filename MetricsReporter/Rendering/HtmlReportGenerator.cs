namespace MetricsReporter.Rendering;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using MetricsReporter.Model;
using MetricsReporter.Serialization;

/// <summary>
/// Generates the HTML dashboard for the metrics report.
/// Coordinates the generation of HTML header, table, styles, and scripts.
/// </summary>
public sealed class HtmlReportGenerator
{
  // Explicit ordering required by the user: altcover -> roslyn -> sarif
  private static readonly MetricIdentifier[] MetricOrder = new[]
  {
        // AltCover: sequence, branch, npath, cyclomatic (cyclomatic placed last in altcover group)
        MetricIdentifier.AltCoverSequenceCoverage,
        MetricIdentifier.AltCoverBranchCoverage,
        MetricIdentifier.AltCoverNPathComplexity,
        MetricIdentifier.AltCoverCyclomaticComplexity,

        // Roslyn: cyclomatic first in roslyn group, then maintainability, coupling, depth, source lines, executable lines
        MetricIdentifier.RoslynCyclomaticComplexity,
        MetricIdentifier.RoslynMaintainabilityIndex,
        MetricIdentifier.RoslynClassCoupling,
        MetricIdentifier.RoslynDepthOfInheritance,
        MetricIdentifier.RoslynSourceLines,
        MetricIdentifier.RoslynExecutableLines,

        // SARIF
        MetricIdentifier.SarifCaRuleViolations,
        MetricIdentifier.SarifIdeRuleViolations
    };

  /// <summary>
  /// Produces HTML markup for the specified report.
  /// The layout is a minimalistic table similar to Visual Studio Code Code Metrics Results.
  /// </summary>
  /// <param name="report">The metrics report to generate HTML for.</param>
  /// <param name="coverageHtmlDir">Optional path to HTML coverage reports directory for generating hyperlinks.</param>
  /// <returns>Complete HTML document as a string.</returns>
  public static string Generate(MetricsReport report, string? coverageHtmlDir = null)
  {
    ArgumentNullException.ThrowIfNull(report);

    var builder = new StringBuilder();

    // HTML document structure
    builder.AppendLine("<!DOCTYPE html>");
    builder.AppendLine("<html lang=\"en\"> ");
    builder.AppendLine("<head>");
    builder.AppendLine("  <meta charset=\"utf-8\"/>");
    builder.AppendLine($"  <title>Metrics Report - {WebUtility.HtmlEncode(report.Solution.Name)}</title>");
    builder.AppendLine("  <style>");
    builder.AppendLine(HtmlStylesGenerator.Generate());
    builder.AppendLine("  </style>");
    builder.AppendLine("</head>");
    builder.AppendLine("<body>");

    AppendBodyContent(builder, report, coverageHtmlDir);

    builder.AppendLine("</body>");
    builder.AppendLine("</html>");
    return builder.ToString();
  }

  private static void AppendBodyContent(StringBuilder builder, MetricsReport report, string? coverageHtmlDir)
  {
    // Header section (title, metadata, legend, controls)
    builder.Append(HtmlHeaderGenerator.Generate(report));

    // Table section
    var metricUnits = BuildMetricUnits(report.Metadata);
    var tableGenerator = new HtmlTableGenerator(MetricOrder, metricUnits);
    builder.Append(tableGenerator.Generate(report, coverageHtmlDir));

    AppendScriptTagIfNotEmpty(builder, "threshold-data", CreateThresholdPayload(report.Metadata));
    AppendScriptTagIfNotEmpty(builder, "rule-descriptions-data", CreateRuleDescriptionsPayload(report.Metadata));
    AppendScriptTagIfNotEmpty(builder, "metric-aliases-data", CreateMetricAliasesPayload(report.Metadata));

    // JavaScript section
    builder.AppendLine("<script>");
    builder.AppendLine(HtmlScriptGenerator.Generate());
    builder.AppendLine("</script>");
  }

  private static Dictionary<MetricIdentifier, string?> BuildMetricUnits(ReportMetadata metadata)
  {
    var result = new Dictionary<MetricIdentifier, string?>();
    foreach (var metric in MetricOrder)
    {
      if (metadata.MetricDescriptors.TryGetValue(metric, out var descriptor))
      {
        result[metric] = descriptor.Unit;
      }
      else
      {
        result[metric] = MetricDescriptorCatalog.TryGetUnit(metric);
      }
    }

    return result;
  }

  /// <summary>
  /// Creates a JSON payload containing threshold data for JavaScript consumption in the HTML report.
  /// </summary>
  /// <param name="metadata">The report metadata containing threshold definitions.</param>
  /// <returns>
  /// A JSON string with threshold data, or <see langword="null"/> if no thresholds are defined.
  /// The JSON is sanitized to prevent script tag injection.
  /// </returns>
  /// <remarks>
  /// The payload structure includes:
  /// - Metric descriptions for tooltip display
  /// - "Higher is better" preference for each metric
  /// - Threshold values (warning/error) for each symbol level
  /// </remarks>
  private static string? CreateThresholdPayload(ReportMetadata metadata)
  {
    if (metadata.ThresholdsByLevel.Count == 0)
    {
      return null;
    }

    var payload = BuildThresholdPayload(metadata);
    var json = SerializeThresholdPayload(payload);
    return SanitizeJsonForScriptTag(json);
  }

  /// <summary>
  /// Builds the threshold payload dictionary structure from report metadata.
  /// </summary>
  /// <param name="metadata">The report metadata containing threshold definitions.</param>
  /// <returns>A dictionary keyed by metric identifier with threshold information.</returns>
  private static Dictionary<string, object?> BuildThresholdPayload(ReportMetadata metadata)
  {
    var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
    foreach (var (identifier, levels) in metadata.ThresholdsByLevel)
    {
      var key = identifier.ToString();
      metadata.ThresholdDescriptions.TryGetValue(identifier, out var description);

      var higherIsBetter = ExtractHigherIsBetterPreference(levels);
      var positiveDeltaNeutral = ExtractPositiveDeltaNeutralPreference(levels);
      var levelEntries = BuildLevelEntries(levels);

      payload[key] = new
      {
        description,
        higherIsBetter,
        positiveDeltaNeutral,
        levels = levelEntries
      };
    }

    return payload;
  }

  /// <summary>
  /// Extracts the "higher is better" preference from threshold levels.
  /// </summary>
  /// <param name="levels">The threshold levels dictionary.</param>
  /// <returns><see langword="true"/> if higher values are better; otherwise, <see langword="false"/>.</returns>
  private static bool ExtractHigherIsBetterPreference(IDictionary<MetricSymbolLevel, MetricThreshold> levels)
  {
    foreach (var threshold in levels.Values)
    {
      return threshold.HigherIsBetter;
    }

    return true;
  }

  private static bool ExtractPositiveDeltaNeutralPreference(IDictionary<MetricSymbolLevel, MetricThreshold> levels)
  {
    foreach (var threshold in levels.Values)
    {
      return threshold.PositiveDeltaNeutral;
    }

    return false;
  }

  /// <summary>
  /// Builds level entries dictionary for all symbol levels, including null entries for missing levels.
  /// </summary>
  /// <param name="levels">The threshold levels dictionary.</param>
  /// <returns>A dictionary mapping symbol level names to threshold objects or null.</returns>
  private static Dictionary<string, object?> BuildLevelEntries(IDictionary<MetricSymbolLevel, MetricThreshold> levels)
  {
    var levelEntries = new Dictionary<string, object?>(StringComparer.Ordinal);
    foreach (var level in Enum.GetValues<MetricSymbolLevel>())
    {
      if (levels.TryGetValue(level, out var threshold))
      {
        levelEntries[level.ToString()] = new
        {
          warning = threshold.Warning,
          error = threshold.Error,
          higherIsBetter = threshold.HigherIsBetter
        };
      }
      else
      {
        levelEntries[level.ToString()] = null;
      }
    }

    return levelEntries;
  }

  /// <summary>
  /// Serializes the threshold payload to JSON.
  /// </summary>
  /// <param name="payload">The payload dictionary to serialize.</param>
  /// <returns>A JSON string representation of the payload.</returns>
  private static string SerializeThresholdPayload(Dictionary<string, object?> payload)
      => JsonSerializer.Serialize(payload, JsonSerializerOptionsFactory.Create());

  /// <summary>
  /// Sanitizes JSON string to prevent script tag injection when embedded in HTML.
  /// </summary>
  /// <param name="json">The JSON string to sanitize.</param>
  /// <returns>A sanitized JSON string with escaped script tags.</returns>
  private static string SanitizeJsonForScriptTag(string json)
      => json.Replace("</script>", "<\\/script>", StringComparison.Ordinal);

  /// <summary>
  /// Creates a JSON payload containing rule descriptions for JavaScript consumption in the HTML report.
  /// </summary>
  /// <param name="metadata">The report metadata containing rule descriptions.</param>
  /// <returns>
  /// A JSON string with rule descriptions, or <see langword="null"/> if no rule descriptions are defined.
  /// The JSON is sanitized to prevent script tag injection.
  /// </returns>
  private static string? CreateRuleDescriptionsPayload(ReportMetadata metadata)
  {
    if (metadata.RuleDescriptions.Count == 0)
    {
      return null;
    }

    var payload = BuildRuleDescriptionsPayload(metadata);
    var json = SerializeRuleDescriptionsPayload(payload);
    return SanitizeJsonForScriptTag(json);
  }

  /// <summary>
  /// Creates a JSON payload containing metric aliases for JavaScript consumption in the HTML report.
  /// </summary>
  /// <param name="metadata">The report metadata containing metric aliases.</param>
  /// <returns>A sanitized JSON string or <see langword="null"/> when no aliases are present.</returns>
  private static string? CreateMetricAliasesPayload(ReportMetadata metadata)
  {
    if (metadata.MetricAliases.Count == 0)
    {
      return null;
    }

    var payload = BuildMetricAliasesPayload(metadata);
    var json = SerializeMetricAliasesPayload(payload);
    return SanitizeJsonForScriptTag(json);
  }

  private static Dictionary<string, IReadOnlyList<string>> BuildMetricAliasesPayload(ReportMetadata metadata)
  {
    var payload = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
    foreach (var (identifier, aliases) in metadata.MetricAliases)
    {
      payload[identifier.ToString()] = aliases;
    }

    return payload;
  }

  private static string SerializeMetricAliasesPayload(Dictionary<string, IReadOnlyList<string>> payload)
    => JsonSerializer.Serialize(payload, JsonSerializerOptionsFactory.Create());

  /// <summary>
  /// Builds the rule descriptions payload dictionary structure from report metadata.
  /// </summary>
  /// <param name="metadata">The report metadata containing rule descriptions.</param>
  /// <returns>A dictionary keyed by rule ID with rule description information.</returns>
  private static Dictionary<string, object?> BuildRuleDescriptionsPayload(ReportMetadata metadata)
  {
    var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
    foreach (var (ruleId, description) in metadata.RuleDescriptions)
    {
      payload[ruleId] = new
      {
        shortDescription = description.ShortDescription,
        fullDescription = description.FullDescription,
        helpUri = description.HelpUri,
        category = description.Category
      };
    }

    return payload;
  }

  /// <summary>
  /// Serializes the rule descriptions payload to JSON.
  /// </summary>
  /// <param name="payload">The payload dictionary to serialize.</param>
  /// <returns>A JSON string representation of the payload.</returns>
  private static string SerializeRuleDescriptionsPayload(Dictionary<string, object?> payload)
      => JsonSerializer.Serialize(payload, JsonSerializerOptionsFactory.Create());

  /// <summary>
  /// Appends a script tag with JSON data to the string builder if the payload is not empty.
  /// </summary>
  /// <param name="builder">The string builder to append to.</param>
  /// <param name="scriptId">The ID attribute value for the script tag.</param>
  /// <param name="payload">The JSON payload to embed, or <see langword="null"/> or empty to skip.</param>
  private static void AppendScriptTagIfNotEmpty(StringBuilder builder, string scriptId, string? payload)
  {
    if (string.IsNullOrEmpty(payload))
    {
      return;
    }

    builder.AppendLine($"<script id=\"{scriptId}\" type=\"application/json\">");
    builder.AppendLine(payload);
    builder.AppendLine("</script>");
  }
}



