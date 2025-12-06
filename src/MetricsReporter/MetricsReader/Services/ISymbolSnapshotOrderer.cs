namespace MetricsReporter.MetricsReader.Services;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// Orders symbol metric snapshots according to specified criteria.
/// </summary>
internal interface ISymbolSnapshotOrderer
{
  /// <summary>
  /// Orders the provided symbol snapshots according to the specified parameters.
  /// </summary>
  /// <param name="snapshots">The snapshots to order.</param>
  /// <param name="parameters">The ordering parameters.</param>
  /// <returns>An ordered enumeration of snapshots.</returns>
  IOrderedEnumerable<SymbolMetricSnapshot> Order(IEnumerable<SymbolMetricSnapshot> snapshots, SymbolSnapshotOrderingParameters parameters);
}






