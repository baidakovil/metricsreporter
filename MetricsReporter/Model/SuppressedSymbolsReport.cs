namespace MetricsReporter.Model;

using System;
using System.Collections.Generic;

/// <summary>
/// Root model for the <c>SuppressedSymbols.json</c> artefact produced by
/// the suppressed symbol analysis step.
/// </summary>
/// <remarks>
/// The schema is intentionally minimal and versionless:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="GeneratedAtUtc"/> records when the analysis was executed.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="SuppressedSymbols"/> contains a flat list of suppressed symbol
/// entries without additional nesting or migration infrastructure.
/// </description>
/// </item>
/// </list>
/// Keeping the schema small and stable makes it easy for both humans and tools
/// to inspect the file while avoiding long-term migration overhead.
/// </remarks>
public sealed class SuppressedSymbolsReport
{
  /// <summary>
  /// UTC timestamp indicating when the suppressed symbol analysis was performed.
  /// </summary>
  public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;

  /// <summary>
  /// Collection of suppressed symbols discovered during analysis.
  /// </summary>
  public List<SuppressedSymbolInfo> SuppressedSymbols { get; init; }

  /// <summary>
  /// Initializes a new instance of the <see cref="SuppressedSymbolsReport"/> class.
  /// </summary>
  public SuppressedSymbolsReport()
  {
    SuppressedSymbols = [];
  }
}



