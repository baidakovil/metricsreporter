namespace MetricsReporter.Rendering;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using MetricsReporter.Model;

/// <summary>
/// Builds HTML data attributes for metrics table rows.
/// </summary>
internal sealed class RowAttributeBuilder
{
  private readonly Dictionary<MetricsNode, int>? _descendantCountIndex;
  private readonly RowStateCalculator _stateCalculator;

  /// <summary>
  /// Initializes a new instance of the <see cref="RowAttributeBuilder"/> class.
  /// </summary>
  /// <param name="stateCalculator">Calculator for row state flags.</param>
  /// <param name="descendantCountIndex">Optional index of descendant counts for nodes.</param>
  public RowAttributeBuilder(
    RowStateCalculator stateCalculator,
    Dictionary<MetricsNode, int>? descendantCountIndex)
  {
    _stateCalculator = stateCalculator ?? throw new ArgumentNullException(nameof(stateCalculator));
    _descendantCountIndex = descendantCountIndex;
  }

  /// <summary>
  /// Builds all data attributes for a metrics row.
  /// </summary>
  /// <param name="node">The metrics node to build attributes for.</param>
  /// <returns>A concatenated string of all data attributes.</returns>
  public string BuildAllAttributes(MetricsNode node)
  {
    var sourceDataAttributes = BuildSourceDataAttributes(node);
    var rowStateAttributes = BuildRowStateAttributes(_stateCalculator.Calculate(node));
    var filterAttributes = BuildFilterAttributes(node);
    var virtualizationAttributes = BuildDescendantAttribute(node);
    var defaultVisibilityAttributes = BuildVisibilityAttributes();
    return string.Concat(
      sourceDataAttributes,
      rowStateAttributes,
      filterAttributes,
      virtualizationAttributes,
      defaultVisibilityAttributes);
  }

  private static string BuildSourceDataAttributes(MetricsNode node)
  {
    if (node.Source?.Path is null || !node.Source.StartLine.HasValue)
    {
      return string.Empty;
    }

    var encodedPath = WebUtility.HtmlEncode(node.Source.Path);
    var startLine = node.Source.StartLine.Value.ToString(CultureInfo.InvariantCulture);
    var endLineAttribute = node.Source.EndLine.HasValue
        ? $" data-source-end-line=\"{node.Source.EndLine.Value.ToString(CultureInfo.InvariantCulture)}\""
        : string.Empty;

    return $" data-source-path=\"{encodedPath}\" data-source-line=\"{startLine}\"{endLineAttribute}";
  }

  private static string BuildRowStateAttributes(RowStateCalculator.RowState state)
  {
    var error = state.HasError ? "true" : "false";
    var warning = state.HasWarning ? "true" : "false";
    var suppressed = state.HasSuppressed ? "true" : "false";
    var delta = state.HasDelta ? "true" : "false";
    return $" data-has-error=\"{error}\" data-has-warning=\"{warning}\" data-has-suppressed=\"{suppressed}\" data-has-delta=\"{delta}\"";
  }

  private static string BuildFilterAttributes(MetricsNode node)
  {
    var filterSource = string.IsNullOrWhiteSpace(node.FullyQualifiedName)
        ? node.Name
        : node.FullyQualifiedName!;

    if (string.IsNullOrWhiteSpace(filterSource))
    {
      return " data-filter-key=\"\"";
    }

    var normalized = filterSource.ToLowerInvariant();
    return $" data-filter-key=\"{WebUtility.HtmlEncode(normalized)}\"";
  }

  private static string BuildVisibilityAttributes()
    => " data-hidden-by-detail=\"false\" data-hidden-by-filter=\"false\" data-hidden-by-awareness=\"false\" data-hidden-by-state=\"false\" data-expanded=\"true\"";

  private string BuildDescendantAttribute(MetricsNode node)
  {
    if (_descendantCountIndex is null || !_descendantCountIndex.TryGetValue(node, out var count) || count <= 0)
    {
      return string.Empty;
    }

    return $" data-descendant-count=\"{count}\"";
  }
}

