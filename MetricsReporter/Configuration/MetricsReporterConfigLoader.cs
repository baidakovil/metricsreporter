using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MetricsReporter.Model;

namespace MetricsReporter.Configuration;

/// <summary>
/// Loads <see cref="MetricsReporterConfiguration"/> from a .metricsreporter.json file located
/// at a user-specified path or discovered by walking parent directories from the working directory.
/// </summary>
public sealed class MetricsReporterConfigLoader
{
  private readonly JsonSerializerOptions _serializerOptions = new()
  {
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
  };
  private static readonly string[] GeneralSectionProperties =
  [
    "runScripts",
    "aggregateAfterScripts",
    "verbosity",
    "timeoutSeconds",
    "workingDirectory",
    "logTruncationLimit"
  ];

  private static readonly string[] PathsSectionProperties =
  [
    "metricsDir",
    "solutionName",
    "baselineReference",
    "report",
    "readReport",
    "thresholds",
    "thresholdsInline",
    "altcover",
    "roslyn",
    "sarif",
    "baseline",
    "outputHtml",
    "inputJson",
    "coverageHtmlDir",
    "baselineStoragePath",
    "suppressedSymbols",
    "solutionDirectory",
    "sourceCodeFolders",
    "excludedMembers",
    "excludedAssemblies",
    "excludedTypes",
    "excludeMethods",
    "excludeProperties",
    "excludeFields",
    "excludeEvents",
    "analyzeSuppressedSymbols",
    "replaceBaseline"
  ];

  /// <summary>
  /// Loads configuration from disk and reports any parsing issues.
  /// </summary>
  /// <param name="requestedPath">Optional explicit path provided via CLI.</param>
  /// <param name="workingDirectory">Working directory used for discovery when <paramref name="requestedPath"/> is <see langword="null"/>.</param>
  /// <returns>Result containing the configuration and any validation errors.</returns>
  public ConfigurationLoadResult Load(string? requestedPath, string workingDirectory)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

    var resolvedPath = ResolveConfigPath(requestedPath, workingDirectory);
    if (resolvedPath is null)
    {
      return ConfigurationLoadResult.NotFound();
    }

    try
    {
      var payload = File.ReadAllText(resolvedPath);
      var validationError = ValidateRawJson(payload);
      if (validationError is not null)
      {
        return ConfigurationLoadResult.Failure(resolvedPath, validationError);
      }

      var configuration = JsonSerializer.Deserialize<MetricsReporterConfiguration>(payload, _serializerOptions)
                         ?? new MetricsReporterConfiguration();
      return ConfigurationLoadResult.Success(resolvedPath, configuration);
    }
    catch (JsonException jsonEx)
    {
      return ConfigurationLoadResult.Failure(resolvedPath, $"Failed to parse configuration: {jsonEx.Message}");
    }
    catch (IOException ioEx)
    {
      return ConfigurationLoadResult.Failure(resolvedPath, $"Failed to read configuration: {ioEx.Message}");
    }
    catch (UnauthorizedAccessException unauthorizedEx)
    {
      return ConfigurationLoadResult.Failure(resolvedPath, $"Failed to read configuration: {unauthorizedEx.Message}");
    }
  }

  /// <summary>
  /// Resolves the configuration path using explicit path when provided or by walking up from the working directory.
  /// </summary>
  /// <param name="requestedPath">Optional path explicitly provided by the user.</param>
  /// <param name="workingDirectory">Working directory used for discovery.</param>
  /// <returns>Absolute path to the configuration file, or <see langword="null"/> if not found.</returns>
  public static string? ResolveConfigPath(string? requestedPath, string workingDirectory)
  {
    if (!string.IsNullOrWhiteSpace(requestedPath))
    {
      return Path.GetFullPath(requestedPath);
    }

    var directory = new DirectoryInfo(Path.GetFullPath(workingDirectory));
    while (directory is not null)
    {
      var candidate = Path.Combine(directory.FullName, ".metricsreporter.json");
      if (File.Exists(candidate))
      {
        return candidate;
      }

      directory = directory.Parent;
    }

    return null;
  }

  private static string? ValidateRawJson(string json)
  {
    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;

    return ValidateRootElement(root)
           ?? ValidateRequiredSections(root)
           ?? ValidateSectionProperties(root, "general", GeneralSectionProperties)
           ?? ValidateSectionProperties(root, "paths", PathsSectionProperties)
           ?? ValidateMetricAliases(root)
           ?? ValidateScriptsSection(root);
  }

  private static string? ValidateRootElement(JsonElement root)
  {
    if (root.ValueKind != JsonValueKind.Object)
    {
      return "Configuration root must be a JSON object.";
    }

    var allowedRoot = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "general",
      "paths",
      "scripts",
      "metricAliases"
    };
    foreach (var property in root.EnumerateObject())
    {
      if (!allowedRoot.Contains(property.Name))
      {
        return $"Unknown root property '{property.Name}' in configuration.";
      }
    }

    return null;
  }

  private static string? ValidateRequiredSections(JsonElement root)
  {
    var requiredSections = new[] { "general", "paths", "scripts" };
    foreach (var section in requiredSections)
    {
      if (!root.TryGetProperty(section, out _))
      {
        return $"Missing required section '{section}' in configuration.";
      }
    }

    return null;
  }

  private static string? ValidateSectionProperties(JsonElement root, string sectionName, IEnumerable<string> allowedProperties)
  {
    return ValidateSection(root, sectionName, allowedProperties)
      ? null
      : $"Invalid property in '{sectionName}' section.";
  }

  private static string? ValidateScriptsSection(JsonElement root)
  {
    if (!root.TryGetProperty("scripts", out var scriptsElement) || scriptsElement.ValueKind != JsonValueKind.Object)
    {
      return "Missing required section 'scripts' in configuration.";
    }

    var allowedScriptProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "generate", "read", "test" };
    foreach (var property in scriptsElement.EnumerateObject())
    {
      if (!allowedScriptProperties.Contains(property.Name))
      {
        return $"Unknown property 'scripts.{property.Name}' in configuration.";
      }
    }

    return ValidateScriptGroup(scriptsElement, "read")
           ?? ValidateScriptGroup(scriptsElement, "test");
  }

  private static string? ValidateScriptGroup(JsonElement scriptsElement, string groupName)
  {
    if (!scriptsElement.TryGetProperty(groupName, out var groupElement))
    {
      return null;
    }

    if (groupElement.ValueKind != JsonValueKind.Object)
    {
      return $"scripts.{groupName} must be an object.";
    }

    var allowedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "any", "byMetric" };
    foreach (var property in groupElement.EnumerateObject())
    {
      if (!allowedProperties.Contains(property.Name))
      {
        return $"Unknown property 'scripts.{groupName}.{property.Name}' in configuration.";
      }
    }

    if (groupElement.TryGetProperty("byMetric", out var byMetricElement) && byMetricElement.ValueKind == JsonValueKind.Array)
    {
      return ValidateByMetricEntries(byMetricElement, groupName);
    }

    return null;
  }

  private static string? ValidateMetricAliases(JsonElement root)
  {
    if (!root.TryGetProperty("metricAliases", out var aliasesElement))
    {
      return null;
    }

    if (aliasesElement.ValueKind != JsonValueKind.Object)
    {
      return "metricAliases must be an object whose keys are MetricIdentifier values.";
    }

    var aliasToMetric = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var property in aliasesElement.EnumerateObject())
    {
      var validationResult = ValidateMetricAliasEntry(property, aliasToMetric);
      if (validationResult is not null)
      {
        return validationResult;
      }
    }

    return null;
  }

  private static string? ValidateMetricAliasEntry(JsonProperty property, IDictionary<string, string> aliasToMetric)
  {
    if (!Enum.TryParse<MetricIdentifier>(property.Name, ignoreCase: true, out _))
    {
      return $"Unknown metric identifier '{property.Name}' in metricAliases.";
    }

    if (property.Value.ValueKind != JsonValueKind.Array || property.Value.GetArrayLength() == 0)
    {
      return $"metricAliases.{property.Name} must be a non-empty array of strings.";
    }

    foreach (var aliasElement in property.Value.EnumerateArray())
    {
      var validationResult = ValidateMetricAliasValue(property.Name, aliasElement, aliasToMetric);
      if (validationResult is not null)
      {
        return validationResult;
      }
    }

    return null;
  }

  private static string? ValidateMetricAliasValue(
      string metricName,
      JsonElement aliasElement,
      IDictionary<string, string> aliasToMetric)
  {
    if (aliasElement.ValueKind != JsonValueKind.String)
    {
      return $"metricAliases.{metricName} must contain only strings.";
    }

    var alias = aliasElement.GetString();
    if (string.IsNullOrWhiteSpace(alias))
    {
      return $"metricAliases.{metricName} must contain non-empty strings.";
    }

    var trimmed = alias.Trim();
    if (aliasToMetric.TryGetValue(trimmed, out var existingMetric) &&
        !string.Equals(existingMetric, metricName, StringComparison.OrdinalIgnoreCase))
    {
      return $"Alias '{trimmed}' is assigned to multiple metrics ({existingMetric}, {metricName}).";
    }

    aliasToMetric[trimmed] = metricName;
    return null;
  }

  private static string? ValidateByMetricEntries(JsonElement byMetricElement, string groupName)
  {
    var metricToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var item in byMetricElement.EnumerateArray())
    {
      if (item.ValueKind != JsonValueKind.Object)
      {
        return $"scripts.{groupName}.byMetric items must be objects.";
      }

      if (!item.TryGetProperty("metrics", out var metricsElement) || metricsElement.ValueKind != JsonValueKind.Array || metricsElement.GetArrayLength() == 0)
      {
        return $"scripts.{groupName}.byMetric items must contain non-empty 'metrics' array.";
      }

      if (!item.TryGetProperty("path", out var pathElement) || pathElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(pathElement.GetString()))
      {
        return $"scripts.{groupName}.byMetric items must contain a non-empty 'path' string.";
      }

      var pathValue = pathElement.GetString()!;
      foreach (var metric in metricsElement.EnumerateArray())
      {
        if (metric.ValueKind != JsonValueKind.String)
        {
          return $"scripts.{groupName}.byMetric metrics must be strings.";
        }

        var metricName = metric.GetString() ?? string.Empty;
        if (metricToPath.TryGetValue(metricName, out var existingPath) && !string.Equals(existingPath, pathValue, StringComparison.OrdinalIgnoreCase))
        {
          return $"Duplicate metric '{metricName}' in scripts.{groupName}.byMetric with different scripts.";
        }

        metricToPath[metricName] = pathValue;
      }
    }

    return null;
  }

  private static bool ValidateSection(JsonElement root, string sectionName, IEnumerable<string> allowedProperties)
  {
    if (!root.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Object)
    {
      return false;
    }

    var allowed = new HashSet<string>(allowedProperties, StringComparer.OrdinalIgnoreCase);
    foreach (var property in section.EnumerateObject())
    {
      if (!allowed.Contains(property.Name))
      {
        return false;
      }
    }

    return true;
  }
}

/// <summary>
/// Represents the outcome of a configuration load operation.
/// </summary>
public sealed record ConfigurationLoadResult
{
  private ConfigurationLoadResult(string? path, MetricsReporterConfiguration configuration, IReadOnlyList<string> errors)
  {
    Path = path;
    Configuration = configuration;
    Errors = errors;
  }

  /// <summary>
  /// Gets the resolved configuration file path, if any.
  /// </summary>
  public string? Path { get; }

  /// <summary>
  /// Gets the loaded configuration (empty when not found).
  /// </summary>
  public MetricsReporterConfiguration Configuration { get; }

  /// <summary>
  /// Gets validation or parsing errors associated with the load.
  /// </summary>
  public IReadOnlyList<string> Errors { get; }

  /// <summary>
  /// Gets a value indicating whether configuration was loaded successfully.
  /// </summary>
  public bool IsSuccess => Errors.Count == 0;

  /// <summary>
  /// Creates a successful result.
  /// </summary>
  /// <param name="path">Path to the configuration file.</param>
  /// <param name="configuration">Deserialized configuration.</param>
  /// <returns>A successful result.</returns>
  public static ConfigurationLoadResult Success(string path, MetricsReporterConfiguration configuration)
    => new(path, configuration, Array.Empty<string>());

  /// <summary>
  /// Creates a result indicating that no configuration file was found.
  /// </summary>
  /// <returns>A not-found result.</returns>
  public static ConfigurationLoadResult NotFound()
    => new(null, new MetricsReporterConfiguration(), Array.Empty<string>());

  /// <summary>
  /// Creates a failed result with errors.
  /// </summary>
  /// <param name="path">Path that was attempted.</param>
  /// <param name="message">Error message.</param>
  /// <returns>A failed result.</returns>
  public static ConfigurationLoadResult Failure(string path, string message)
    => new(path, new MetricsReporterConfiguration(), new[] { message });
}

