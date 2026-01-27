using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace MetricsReporter.Configuration;

/// <summary>
/// Builds <see cref="MetricsReporterConfiguration"/> from environment variables following the METRICSREPORTER_* convention.
/// </summary>
public static class EnvironmentConfigurationProvider
{
  private static readonly char[] ListSeparators = [';', ','];

  /// <summary>
  /// Reads environment variables and returns a configuration snapshot.
  /// </summary>
  /// <returns>Configuration populated from environment variables.</returns>
  public static MetricsReporterConfiguration Read()
  {
    return new MetricsReporterConfiguration
    {
      General = new GeneralConfiguration
      {
        RunScripts = ReadBool("METRICSREPORTER_RUN_SCRIPTS"),
        AggregateAfterScripts = ReadBool("METRICSREPORTER_AGGREGATE_AFTER_SCRIPTS"),
        Verbosity = ReadString("METRICSREPORTER_VERBOSITY"),
        TimeoutSeconds = ReadInt("METRICSREPORTER_TIMEOUT_SECONDS"),
        WorkingDirectory = ReadString("METRICSREPORTER_WORKING_DIRECTORY"),
        LogTruncationLimit = ReadInt("METRICSREPORTER_LOG_TRUNCATION_LIMIT")
      },
      Paths = new PathsConfiguration
      {
        MetricsDir = ReadString("METRICSREPORTER_PATHS_METRICS_DIR"),
        SolutionName = ReadString("METRICSREPORTER_PATHS_SOLUTION_NAME"),
        BaselineReference = ReadString("METRICSREPORTER_PATHS_BASELINE_REF"),
        Report = ReadString("METRICSREPORTER_PATHS_REPORT"),
        ReadReport = ReadString("METRICSREPORTER_PATHS_READ_REPORT"),
        Thresholds = ReadString("METRICSREPORTER_PATHS_THRESHOLDS"),
        ThresholdsInline = ReadString("METRICSREPORTER_PATHS_THRESHOLDS_INLINE"),
        OpenCover = ReadList("METRICSREPORTER_PATHS_OPENCOVER"),
        Roslyn = ReadList("METRICSREPORTER_PATHS_ROSLYN"),
        Sarif = ReadList("METRICSREPORTER_PATHS_SARIF"),
        Baseline = ReadString("METRICSREPORTER_PATHS_BASELINE"),
        OutputHtml = ReadString("METRICSREPORTER_PATHS_OUTPUT_HTML"),
        InputJson = ReadString("METRICSREPORTER_PATHS_INPUT_JSON"),
        CoverageHtmlDir = ReadString("METRICSREPORTER_PATHS_COVERAGE_HTML_DIR"),
        BaselineStoragePath = ReadString("METRICSREPORTER_PATHS_BASELINE_STORAGE_PATH"),
        SuppressedSymbols = ReadString("METRICSREPORTER_PATHS_SUPPRESSED_SYMBOLS"),
        SolutionDirectory = ReadString("METRICSREPORTER_PATHS_SOLUTION_DIRECTORY"),
        SourceCodeFolders = ReadList("METRICSREPORTER_PATHS_SOURCE_CODE_FOLDERS"),
        ExcludedMembers = ReadString("METRICSREPORTER_PATHS_EXCLUDED_MEMBERS"),
        ExcludedAssemblies = ReadString("METRICSREPORTER_PATHS_EXCLUDED_ASSEMBLIES"),
        ExcludedTypes = ReadString("METRICSREPORTER_PATHS_EXCLUDED_TYPES"),
        ExcludeMethods = ReadBool("METRICSREPORTER_PATHS_EXCLUDE_METHODS"),
        ExcludeProperties = ReadBool("METRICSREPORTER_PATHS_EXCLUDE_PROPERTIES"),
        ExcludeFields = ReadBool("METRICSREPORTER_PATHS_EXCLUDE_FIELDS"),
        ExcludeEvents = ReadBool("METRICSREPORTER_PATHS_EXCLUDE_EVENTS"),
        AnalyzeSuppressedSymbols = ReadBool("METRICSREPORTER_PATHS_ANALYZE_SUPPRESSED_SYMBOLS"),
        ReplaceBaseline = ReadBool("METRICSREPORTER_PATHS_REPLACE_BASELINE")
      },
      Scripts = new ScriptsConfiguration
      {
        Generate = ReadList("METRICSREPORTER_SCRIPTS_GENERATE"),
        Read = new ReadScriptsConfiguration
        {
          Any = ReadList("METRICSREPORTER_SCRIPTS_READ_ANY"),
          ByMetric = ReadMetricScripts("METRICSREPORTER_SCRIPTS_READ_BYMETRIC")
        },
        Test = new ReadScriptsConfiguration
        {
          Any = ReadList("METRICSREPORTER_SCRIPTS_TEST_ANY"),
          ByMetric = ReadMetricScripts("METRICSREPORTER_SCRIPTS_TEST_BYMETRIC")
        }
      },
      MetricAliases = ReadAliases("METRICSREPORTER_METRIC_ALIASES")
    };
  }

  private static Dictionary<string, string[]>? ReadAliases(string name)
  {
    var value = Environment.GetEnvironmentVariable(name);
    return MetricAliasJsonParser.TryParse(value);
  }

  private static string? ReadString(string name)
    => Environment.GetEnvironmentVariable(name);

  private static int? ReadInt(string name)
  {
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
  }

  private static bool? ReadBool(string name)
  {
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    return bool.TryParse(value, out var parsed) ? parsed : null;
  }

  private static string[]? ReadList(string name)
  {
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    return value
      .Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries)
      .Select(item => item.Trim())
      .Where(item => item.Length > 0)
      .ToArray();
  }

  private static IReadOnlyList<MetricScript> ReadMetricScripts(string name)
  {
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
    {
      return Array.Empty<MetricScript>();
    }

    var entries = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
    var scripts = new List<MetricScript>();
    foreach (var entry in entries)
    {
      var parts = entry.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length != 2)
      {
        continue;
      }

      var metrics = parts[0]
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(m => m.Trim())
        .Where(m => m.Length > 0)
        .ToArray();

      var scriptPath = parts[1].Trim();
      if (metrics.Length == 0 || scriptPath.Length == 0)
      {
        continue;
      }

      scripts.Add(new MetricScript
      {
        Metrics = metrics,
        Path = scriptPath
      });
    }

    return scripts;
  }

  private static class MetricAliasJsonParser
  {
    public static Dictionary<string, string[]>? TryParse(string? json)
    {
      if (string.IsNullOrWhiteSpace(json))
      {
        return null;
      }

      try
      {
        using var document = JsonDocument.Parse(json);
        return ParseObject(document.RootElement);
      }
      catch (JsonException)
      {
        return null;
      }
    }

    private static Dictionary<string, string[]>? ParseObject(JsonElement root)
    {
      if (root.ValueKind != JsonValueKind.Object)
      {
        return null;
      }

      var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
      foreach (var property in root.EnumerateObject())
      {
        TryAddAliases(aliases, property);
      }

      return aliases.Count == 0 ? null : aliases;
    }

    private static void TryAddAliases(Dictionary<string, string[]> aliases, JsonProperty property)
    {
      if (property.Value.ValueKind != JsonValueKind.Array)
      {
        return;
      }

      var uniqueAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var item in property.Value.EnumerateArray())
      {
        var alias = ExtractAlias(item);
        if (alias is null)
        {
          continue;
        }

        uniqueAliases.Add(alias);
      }

      if (uniqueAliases.Count == 0)
      {
        return;
      }

      var aliasesCopy = new string[uniqueAliases.Count];
      uniqueAliases.CopyTo(aliasesCopy);
      aliases[property.Name] = aliasesCopy;
    }

    private static string? ExtractAlias(JsonElement item)
    {
      if (item.ValueKind != JsonValueKind.String)
      {
        return null;
      }

      var alias = item.GetString();
      if (string.IsNullOrWhiteSpace(alias))
      {
        return null;
      }

      return alias.Trim();
    }
  }
}

