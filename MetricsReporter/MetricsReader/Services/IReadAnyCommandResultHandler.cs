namespace MetricsReporter.MetricsReader.Services;
using System.Collections.Generic;
/// <summary>
/// Handles result formatting and output for ReadAny command.
/// </summary>
internal interface IReadAnyCommandResultHandler
{
  /// <summary>
  /// Handles the command results, converting snapshots to DTOs and writing output.
  /// </summary>
  /// <param name="snapshots">The ordered snapshots to process.</param>
  /// <param name="parameters">The result handling parameters.</param>
  void HandleResults(IEnumerable<SymbolMetricSnapshot> snapshots, ReadAnyCommandResultParameters parameters);
}






