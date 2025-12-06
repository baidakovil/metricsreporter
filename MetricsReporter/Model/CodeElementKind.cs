namespace MetricsReporter.Model;

using System.Text.Json.Serialization;

/// <summary>
/// Describes the node level inside the aggregated metrics hierarchy.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CodeElementKind
{
  /// <summary>
  /// The root solution node.
  /// </summary>
  Solution,

  /// <summary>
  /// An assembly (MSBuild project) level.
  /// </summary>
  Assembly,

  /// <summary>
  /// A namespace level node.
  /// </summary>
  Namespace,

  /// <summary>
  /// A type-level node (class, struct, record, etc.).
  /// </summary>
  Type,

  /// <summary>
  /// A member of a type (method, property, field, accessor, etc.).
  /// </summary>
  Member,
}


