namespace MetricsReporter.Processing;

using System;
using MetricsReporter.Model;

/// <summary>
/// Filters members by their declared <see cref="MemberKind"/>.
/// </summary>
public sealed class MemberKindFilter
{
  private readonly bool _excludeMethods;
  private readonly bool _excludeProperties;
  private readonly bool _excludeFields;
  private readonly bool _excludeEvents;

  /// <summary>
  /// Gets a value indicating whether methods are excluded.
  /// </summary>
  public bool ExcludeMethods => _excludeMethods;

  /// <summary>
  /// Gets a value indicating whether properties are excluded.
  /// </summary>
  public bool ExcludeProperties => _excludeProperties;

  /// <summary>
  /// Gets a value indicating whether fields are excluded.
  /// </summary>
  public bool ExcludeFields => _excludeFields;

  /// <summary>
  /// Gets a value indicating whether events are excluded.
  /// </summary>
  public bool ExcludeEvents => _excludeEvents;

  /// <summary>
  /// Initializes a new instance of the <see cref="MemberKindFilter"/> class.
  /// </summary>
  /// <param name="excludeMethods">Exclude methods.</param>
  /// <param name="excludeProperties">Exclude properties.</param>
  /// <param name="excludeFields">Exclude fields.</param>
  /// <param name="excludeEvents">Exclude events.</param>
  public MemberKindFilter(
      bool excludeMethods,
      bool excludeProperties,
      bool excludeFields,
      bool excludeEvents)
  {
    _excludeMethods = excludeMethods;
    _excludeProperties = excludeProperties;
    _excludeFields = excludeFields;
    _excludeEvents = excludeEvents;
  }

  /// <summary>
  /// Determines whether a member should be excluded based on kind.
  /// </summary>
  /// <param name="kind">Member kind.</param>
  /// <param name="hasSarifViolations">If true, the member is kept even when excluded.</param>
  /// <returns>True when the member should be excluded.</returns>
  public bool ShouldExclude(MemberKind kind, bool hasSarifViolations)
  {
    if (hasSarifViolations)
    {
      return false;
    }

    return kind switch
    {
      MemberKind.Method => _excludeMethods,
      MemberKind.Property => _excludeProperties,
      MemberKind.Field => _excludeFields,
      MemberKind.Event => _excludeEvents,
      _ => false
    };
  }

  /// <summary>
  /// Creates a filter from explicit flags.
  /// </summary>
  public static MemberKindFilter Create(
      bool excludeMethods,
      bool excludeProperties,
      bool excludeFields,
      bool excludeEvents)
      => new(excludeMethods, excludeProperties, excludeFields, excludeEvents);
}
