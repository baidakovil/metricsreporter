namespace MetricsReporter.MetricsReader.Settings;

/// <summary>
/// Defines grouping modes supported by metrics-reader commands.
/// </summary>
internal enum MetricsReaderGroupByOption
{
  /// <summary>
  /// No grouping – keeps the legacy linear output format.
  /// </summary>
  None = 0,

  /// <summary>
  /// Groups violations by metric identifier.
  /// </summary>
  Metric,

  /// <summary>
  /// Groups violations by declaring namespace.
  /// </summary>
  Namespace,

  /// <summary>
  /// Groups violations by declaring type.
  /// </summary>
  Type,

  /// <summary>
  /// Groups violations by method/member fully qualified name.
  /// </summary>
  Method,

  /// <summary>
  /// Groups SARIF violations by rule identifier.
  /// </summary>
  RuleId
}

