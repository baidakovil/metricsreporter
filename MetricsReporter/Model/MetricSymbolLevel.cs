namespace MetricsReporter.Model;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the hierarchical level of a metrics node within the report.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MetricSymbolLevel
{
  /// <summary>
  /// The entire solution aggregate.
  /// </summary>
  Solution,

  /// <summary>
  /// A compiled assembly within the solution.
  /// </summary>
  Assembly,

  /// <summary>
  /// A namespace within an assembly.
  /// </summary>
  Namespace,

  /// <summary>
  /// A type (class, struct, record, interface, etc.).
  /// </summary>
  Type,

  /// <summary>
  /// A member (method, property, field, event) within a type.
  /// </summary>
  Member
}



