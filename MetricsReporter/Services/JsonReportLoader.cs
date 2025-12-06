namespace MetricsReporter.Services;

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Model;
using MetricsReporter.Serialization;

/// <summary>
/// Loads metrics reports from JSON files.
/// </summary>
public sealed class JsonReportLoader
{
  /// <summary>
  /// Loads a metrics report from a JSON file.
  /// </summary>
  /// <param name="jsonPath">Path to the JSON file containing the metrics report.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <returns>The loaded metrics report, or <see langword="null"/> if deserialization failed.</returns>
  /// <exception cref="FileNotFoundException">Thrown when the JSON file does not exist.</exception>
  /// <exception cref="JsonException">Thrown when the JSON content is invalid.</exception>
  public static async Task<MetricsReport?> LoadAsync(string jsonPath, CancellationToken cancellationToken)
  {
    ValidatePath(jsonPath);
    await using var stream = OpenFile(jsonPath);
    return await DeserializeReportAsync(stream, cancellationToken).ConfigureAwait(false);
  }

  private static void ValidatePath(string jsonPath)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(jsonPath);
    if (!File.Exists(jsonPath))
    {
      throw new FileNotFoundException($"JSON file not found: {jsonPath}", jsonPath);
    }
  }

  private static FileStream OpenFile(string jsonPath)
  {
    return File.OpenRead(jsonPath);
  }

  private static async Task<MetricsReport?> DeserializeReportAsync(FileStream stream, CancellationToken cancellationToken)
  {
    return await JsonSerializer.DeserializeAsync<MetricsReport>(
        stream,
        JsonSerializerOptionsFactory.Create(),
        cancellationToken).ConfigureAwait(false);
  }
}


