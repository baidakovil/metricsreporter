namespace MetricsReporter.MetricsReader.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.MetricsReader.Output;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Orders symbol metric snapshots according to specified criteria.
/// </summary>
internal sealed class SymbolSnapshotOrderer : ISymbolSnapshotOrderer
{
  /// <inheritdoc/>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Sorting method uses LINQ operations with multiple model types (CodeElementKind, ThresholdStatus, StringComparer) which are necessary for ordering logic; decomposition would fragment sorting logic without benefit.")]
  public IOrderedEnumerable<SymbolMetricSnapshot> Order(IEnumerable<SymbolMetricSnapshot> snapshots, SymbolSnapshotOrderingParameters parameters)
  {
    ArgumentNullException.ThrowIfNull(snapshots);
    ArgumentNullException.ThrowIfNull(parameters);

    // When SymbolKind is Any, prioritize types before members to match readsarif behavior
    if (parameters.SymbolKind == MetricsReaderSymbolKind.Any)
    {
      return snapshots
        .OrderBy(snapshot => snapshot.Kind == CodeElementKind.Type ? 0 : 1)
        .ThenByDescending(snapshot => snapshot.Status == ThresholdStatus.Error ? 2 : 1)
        .ThenByDescending(snapshot => snapshot.Magnitude ?? 0m)
        .ThenBy(snapshot => snapshot.Symbol, StringComparer.Ordinal);
    }

    return snapshots
      .OrderByDescending(snapshot => snapshot.Status == ThresholdStatus.Error ? 2 : 1)
      .ThenByDescending(snapshot => snapshot.Magnitude ?? 0m)
      .ThenBy(snapshot => snapshot.Symbol, StringComparer.Ordinal);
  }
}


