namespace MetricsReporter.MetricsReader.Services;
using System;
using System.Collections.Generic;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;
/// <summary>
/// Executes queries for problematic symbols.
/// </summary>
internal sealed class SymbolQueryService : ISymbolQueryService
{
  /// <inheritdoc/>
  public IEnumerable<SymbolMetricSnapshot> GetProblematicSymbols(
    MetricsReaderEngine engine,
    string @namespace,
    MetricIdentifier metric,
    MetricsReaderSymbolKind symbolKind,
    bool includeSuppressed)
  {
    ArgumentNullException.ThrowIfNull(engine);
    if (string.IsNullOrWhiteSpace(@namespace))
    {
      throw new ArgumentException("Namespace cannot be null or empty.", nameof(@namespace));
    }
    var trimmedNamespace = @namespace.Trim();
    var filter = new SymbolFilter(trimmedNamespace, metric, symbolKind, includeSuppressed);
    return engine.GetProblematicSymbols(filter);
  }
}






