namespace MetricsReporter.Services;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using MetricsReporter.Model;
using MetricsReporter.Serialization;

/// <summary>
/// Loads the baseline report from a JSON file.
/// </summary>
public sealed class BaselineLoader
{
  /// <summary>
  /// Loads the baseline report asynchronously.
  /// </summary>
  /// <param name="path">Baseline file path. May be <see langword="null"/>.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Baseline report or <see langword="null"/> when the file does not exist.</returns>
  public static async Task<MetricsReport?> LoadAsync(string? path, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      return null;
    }

    if (!File.Exists(path))
    {
      return null;
    }

    await using var stream = File.OpenRead(path);
    return await JsonSerializer.DeserializeAsync<MetricsReport>(stream, JsonSerializerOptionsFactory.Create(), cancellationToken).ConfigureAwait(false);
  }
}


