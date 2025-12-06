namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using MetricsReporter.Model;

/// <summary>
/// Parameters for creating a MetricsReaderContext.
/// </summary>
internal sealed record ContextCreationParameters(
  MetricsReport Report,
  ReadOnlyDictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>> ThresholdsByLevel,
  IReadOnlyDictionary<MetricIdentifier, MetricThresholdDefinition>? OverrideThresholds,
  IEnumerable<SuppressedSymbolInfo> SuppressedSymbols,
  bool IncludeSuppressed);


