using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

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
    if (document.RootElement.ValueKind != JsonValueKind.Object)
    {
      return "Configuration root must be a JSON object.";
    }

    var root = document.RootElement;
    var requiredRoot = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general", "paths", "scripts" };
    foreach (var property in root.EnumerateObject())
    {
      if (!requiredRoot.Contains(property.Name))
      {
        return $"Unknown root property '{property.Name}' in configuration.";
      }
    }

    foreach (var required in requiredRoot)
    {
      if (!root.TryGetProperty(required, out _))
      {
        return $"Missing required section '{required}' in configuration.";
      }
    }

    if (!ValidateSection(root, "general", GeneralSectionProperties))
    {
      return "Invalid property in 'general' section.";
    }

    if (!ValidateSection(root, "paths", PathsSectionProperties))
    {
      return "Invalid property in 'paths' section.";
    }

    if (!root.TryGetProperty("scripts", out var scriptsElement) || scriptsElement.ValueKind != JsonValueKind.Object)
    {
      return "Missing required section 'scripts' in configuration.";
    }

    foreach (var property in scriptsElement.EnumerateObject())
    {
      if (!property.NameEquals("generate") && !property.NameEquals("read"))
      {
        return $"Unknown property 'scripts.{property.Name}' in configuration.";
      }
    }

    if (scriptsElement.TryGetProperty("read", out var readElement))
    {
      if (readElement.ValueKind != JsonValueKind.Object)
      {
        return "scripts.read must be an object.";
      }

      foreach (var property in readElement.EnumerateObject())
      {
        if (!property.NameEquals("any") && !property.NameEquals("byMetric"))
        {
          return $"Unknown property 'scripts.read.{property.Name}' in configuration.";
        }
      }

      if (readElement.TryGetProperty("byMetric", out var byMetricElement) && byMetricElement.ValueKind == JsonValueKind.Array)
      {
        var metricToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in byMetricElement.EnumerateArray())
        {
          if (item.ValueKind != JsonValueKind.Object)
          {
            return "scripts.read.byMetric items must be objects.";
          }

          if (!item.TryGetProperty("metrics", out var metricsElement) || metricsElement.ValueKind != JsonValueKind.Array || metricsElement.GetArrayLength() == 0)
          {
            return "scripts.read.byMetric items must contain non-empty 'metrics' array.";
          }

          if (!item.TryGetProperty("path", out var pathElement) || pathElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(pathElement.GetString()))
          {
            return "scripts.read.byMetric items must contain a non-empty 'path' string.";
          }

          foreach (var metric in metricsElement.EnumerateArray())
          {
            if (metric.ValueKind != JsonValueKind.String)
            {
              return "scripts.read.byMetric metrics must be strings.";
            }

            var metricName = metric.GetString() ?? string.Empty;
            if (metricToPath.TryGetValue(metricName, out var existingPath) && !string.Equals(existingPath, pathElement.GetString(), StringComparison.OrdinalIgnoreCase))
            {
              return $"Duplicate metric '{metricName}' in scripts.read.byMetric with different scripts.";
            }

            metricToPath[metricName] = pathElement.GetString()!;
          }
        }
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

