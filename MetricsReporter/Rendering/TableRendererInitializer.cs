namespace MetricsReporter.Rendering;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MetricsReporter.Model;

/// <summary>
/// Factory for initializing renderer components for HTML table generation.
/// Encapsulates the creation and dependency wiring of all renderer components,
/// following the Factory pattern to reduce coupling in consuming classes.
/// </summary>
/// <remarks>
/// This class follows the Single Responsibility Principle by being responsible
/// solely for component initialization and dependency resolution. It follows the
/// Open/Closed Principle by being extensible through its helper methods without
/// modifying the main initialization logic. The use of a return value instead of
/// out parameters improves API clarity and testability.
/// </remarks>
internal sealed class TableRendererInitializer
{
  /// <summary>
  /// Initializes all renderer components required for HTML table generation.
  /// </summary>
  /// <param name="metricOrder">The order of metrics to display in columns.</param>
  /// <param name="metricUnits">Units associated with each metric.</param>
  /// <param name="report">The metrics report containing data for index building.</param>
  /// <param name="coverageHtmlDir">Optional path to HTML coverage reports directory.</param>
  /// <returns>
  /// A <see cref="RendererComponents"/> instance containing all initialized renderer components.
  /// </returns>
  /// <remarks>
  /// This method orchestrates the creation of all renderer components and their dependencies:
  /// - Builds indices from the report (suppressed symbols, descendant counts)
  /// - Creates components in the correct order to satisfy dependencies
  /// - Returns an immutable record containing all components for use by the table generator
  /// </remarks>
  [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This method's purpose is to initialize multiple renderer components that naturally depend on many types. The coupling has been reduced by extracting creation logic into separate helper methods (CreateCoverageLinkBuilder, CreateRowStateCalculator, CreateRowAttributeBuilder, CreateMetricCellRenderer). Further reduction would require splitting the method into multiple smaller methods, which would harm readability and make the initialization flow harder to follow. The current structure is clear, maintainable, and follows the Single Responsibility Principle by centralizing renderer initialization.")]
  public static RendererComponents Initialize(
    MetricIdentifier[] metricOrder,
    IReadOnlyDictionary<MetricIdentifier, string?> metricUnits,
    MetricsReport report,
    string? coverageHtmlDir)
  {
    var coverageLinkBuilder = CreateCoverageLinkBuilder(coverageHtmlDir);
    var suppressedIndex = SuppressionIndexBuilder.Build(report);
    var descendantCountIndex = DescendantCountIndexBuilder.Build(report);
    var stateCalculator = CreateRowStateCalculator(metricOrder, suppressedIndex);
    var attributeBuilder = CreateRowAttributeBuilder(stateCalculator, descendantCountIndex);
    var metricCellRenderer = CreateMetricCellRenderer(metricOrder, metricUnits, suppressedIndex);

    return new RendererComponents(
      coverageLinkBuilder,
      suppressedIndex,
      stateCalculator,
      attributeBuilder,
      metricCellRenderer);
  }

  /// <summary>
  /// Creates a coverage link builder if the coverage HTML directory is provided.
  /// </summary>
  /// <param name="coverageHtmlDir">Path to HTML coverage reports directory, or <see langword="null"/>.</param>
  /// <returns>
  /// A <see cref="CoverageLinkBuilder"/> instance if <paramref name="coverageHtmlDir"/> is valid,
  /// otherwise <see langword="null"/>.
  /// </returns>
  private static CoverageLinkBuilder? CreateCoverageLinkBuilder(string? coverageHtmlDir)
    => string.IsNullOrWhiteSpace(coverageHtmlDir) ? null : new CoverageLinkBuilder(coverageHtmlDir);

  /// <summary>
  /// Creates a row state calculator for determining row state flags.
  /// </summary>
  /// <param name="metricOrder">The order of metrics to check.</param>
  /// <param name="suppressedIndex">Optional index of suppressed symbols for lookup.</param>
  /// <returns>A new <see cref="RowStateCalculator"/> instance.</returns>
  private static RowStateCalculator CreateRowStateCalculator(
    MetricIdentifier[] metricOrder,
    Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>? suppressedIndex)
    => new RowStateCalculator(metricOrder, suppressedIndex);

  /// <summary>
  /// Creates a row attribute builder for generating HTML data attributes.
  /// </summary>
  /// <param name="stateCalculator">Calculator for row state flags.</param>
  /// <param name="descendantCountIndex">Index of descendant counts for nodes.</param>
  /// <returns>A new <see cref="RowAttributeBuilder"/> instance.</returns>
  private static RowAttributeBuilder CreateRowAttributeBuilder(
    RowStateCalculator stateCalculator,
    Dictionary<MetricsNode, int> descendantCountIndex)
    => new RowAttributeBuilder(stateCalculator, descendantCountIndex);

  /// <summary>
  /// Creates a metric cell renderer for rendering metric values in table cells.
  /// </summary>
  /// <param name="metricOrder">The order of metrics to display in columns.</param>
  /// <param name="metricUnits">Units associated with each metric.</param>
  /// <param name="suppressedIndex">Optional index of suppressed symbols for lookup.</param>
  /// <returns>A new <see cref="MetricCellRenderer"/> instance.</returns>
  private static MetricCellRenderer CreateMetricCellRenderer(
    MetricIdentifier[] metricOrder,
    IReadOnlyDictionary<MetricIdentifier, string?> metricUnits,
    Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>? suppressedIndex)
    => new MetricCellRenderer(metricOrder, metricUnits, suppressedIndex);
}


