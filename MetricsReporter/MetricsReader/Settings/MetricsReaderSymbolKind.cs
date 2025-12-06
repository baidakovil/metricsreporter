namespace MetricsReporter.MetricsReader.Settings;

/// <summary>
/// Represents the requested symbol granularity for metrics-reader queries.
/// </summary>
public enum MetricsReaderSymbolKind
{
  /// <summary>
  /// Includes both types and members when querying metrics.
  /// </summary>
  Any,

  /// <summary>
  /// Limits queries to type-level symbols (classes, structs, etc.).
  /// </summary>
  Type,

  /// <summary>
  /// Limits queries to member-level symbols (methods, properties, fields).
  /// </summary>
  Member
}


