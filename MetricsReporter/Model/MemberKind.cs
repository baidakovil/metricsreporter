namespace MetricsReporter.Model;

/// <summary>
/// Represents the specific kind of a member within a type.
/// </summary>
public enum MemberKind
{
  /// <summary>
  /// Member kind is unknown or not provided by the source.
  /// </summary>
  Unknown = 0,

  /// <summary>
  /// A method (including constructors and accessors represented as methods).
  /// </summary>
  Method,

  /// <summary>
  /// A property.
  /// </summary>
  Property,

  /// <summary>
  /// A field.
  /// </summary>
  Field,

  /// <summary>
  /// An event.
  /// </summary>
  Event
}
