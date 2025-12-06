namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Model;

/// <summary>
/// Loads threshold overrides from JSON files.
/// </summary>
internal sealed class ThresholdsFileLoader : IThresholdsFileLoader
{
  private readonly IThresholdsParser _parser;

  /// <summary>
  /// Initializes a new instance of the <see cref="ThresholdsFileLoader"/> class.
  /// </summary>
  /// <param name="parser">The thresholds parser to use.</param>
  public ThresholdsFileLoader(IThresholdsParser parser)
  {
    _parser = parser ?? throw new System.ArgumentNullException(nameof(parser));
  }

  /// <inheritdoc/>
  public async Task<IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>?> LoadAsync(
    string? thresholdsPath,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(thresholdsPath))
    {
      return null;
    }

    var absolutePath = Path.GetFullPath(thresholdsPath);
    if (!File.Exists(absolutePath))
    {
      throw new FileNotFoundException($"Thresholds override file not found: {absolutePath}", absolutePath);
    }

    var payload = await ReadJsonPayloadAsync(absolutePath, cancellationToken).ConfigureAwait(false);
    return _parser.Parse(payload);
  }

  private static async Task<string> ReadJsonPayloadAsync(string filePath, CancellationToken cancellationToken)
  {
    await using var stream = File.OpenRead(filePath);
    var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    return document.RootElement.GetRawText();
  }
}


