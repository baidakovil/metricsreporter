namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Enumerates metrics nodes from a metrics report based on filter criteria.
/// </summary>
internal interface IMetricsNodeEnumerator
{
  /// <summary>
  /// Enumerates type nodes from the report.
  /// </summary>
  /// <returns>An enumeration of type nodes.</returns>
  IEnumerable<TypeMetricsNode> EnumerateTypeNodes();

  /// <summary>
  /// Enumerates member nodes from the report.
  /// </summary>
  /// <returns>An enumeration of member nodes.</returns>
  IEnumerable<MemberMetricsNode> EnumerateMemberNodes();

  /// <summary>
  /// Enumerates nodes matching the filter criteria.
  /// </summary>
  /// <param name="filter">The filter to apply.</param>
  /// <returns>An enumeration of matching nodes.</returns>
  IEnumerable<MetricsNode> EnumerateNodes(SymbolFilter filter);
}


