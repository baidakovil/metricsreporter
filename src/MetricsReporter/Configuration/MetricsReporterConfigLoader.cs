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
  public string? ResolveConfigPath(string? requestedPath, string workingDirectory)
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

