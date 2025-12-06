namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Parses threshold definitions from JSON payloads.
/// </summary>
internal interface IThresholdsParser
{
  /// <summary>
  /// Parses the JSON payload and returns metric threshold definitions grouped by symbol level.
  /// </summary>
  /// <param name="input">JSON payload with thresholds. May be <see langword="null"/>.</param>
  /// <returns>Dictionary with threshold definitions.</returns>
  IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition> Parse(string? input);
}


