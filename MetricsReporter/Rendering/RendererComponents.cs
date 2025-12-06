namespace MetricsReporter.Rendering;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Container for all renderer components required for HTML table generation.
/// Encapsulates the initialized components to avoid multiple out parameters.
/// </summary>
/// <param name="CoverageLinkBuilder">Optional builder for coverage HTML links, or <see langword="null"/> if not needed.</param>
/// <param name="SuppressedIndex">Index of suppressed symbols for lookup, or <see langword="null"/> if none.</param>
/// <param name="StateCalculator">Calculator for row state flags based on metrics and suppressions.</param>
/// <param name="AttributeBuilder">Builder for HTML data attributes for table rows.</param>
/// <param name="MetricCellRenderer">Renderer for metric cells in the table.</param>
internal sealed record RendererComponents(
  CoverageLinkBuilder? CoverageLinkBuilder,
  Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>? SuppressedIndex,
  RowStateCalculator StateCalculator,
  RowAttributeBuilder AttributeBuilder,
  MetricCellRenderer MetricCellRenderer);


