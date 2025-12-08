namespace MetricsReporter.MetricsReader.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.Model;

/// <summary>
/// Resolves metric identifiers and configurable aliases to <see cref="MetricIdentifier"/> values.
/// </summary>
internal sealed class MetricIdentifierResolver
{
  private readonly Dictionary<string, MetricIdentifier> _aliasLookup;

  /// <summary>
  /// Gets a resolver instance with no configured aliases.
  /// </summary>
  public static MetricIdentifierResolver Empty { get; } = new(new Dictionary<MetricIdentifier, IReadOnlyList<string>>());

  /// <summary>
  /// Initializes a new instance of the <see cref="MetricIdentifierResolver"/> class.
  /// </summary>
  /// <param name="aliasesByMetric">
  /// Aliases grouped by canonical <see cref="MetricIdentifier"/>. Aliases are normalized by trimming,
  /// removing blanks, and de-duplicating values case-insensitively.
  /// </param>
  /// <exception cref="ArgumentException">
  /// Thrown when the alias map assigns the same alias to multiple metrics.
  /// </exception>
  public MetricIdentifierResolver(IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> aliasesByMetric)
  {
    ArgumentNullException.ThrowIfNull(aliasesByMetric);

    AliasesByMetric = NormalizeAliases(aliasesByMetric);
    _aliasLookup = BuildAliasLookup(AliasesByMetric);
  }

  /// <summary>
  /// Gets the normalized aliases grouped by metric.
  /// </summary>
  public IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> AliasesByMetric { get; }

  /// <summary>
  /// Attempts to resolve an identifier or alias to a <see cref="MetricIdentifier"/>.
  /// </summary>
  /// <param name="value">The identifier or alias provided by the user.</param>
  /// <param name="metric">When successful, the resolved <see cref="MetricIdentifier"/>.</param>
  /// <returns><see langword="true"/> when resolution succeeds; otherwise <see langword="false"/>.</returns>
  public bool TryResolve(string? value, out MetricIdentifier metric)
  {
    metric = default;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    var trimmed = value.Trim();
    if (Enum.TryParse(trimmed, ignoreCase: true, out metric))
    {
      return true;
    }

    return _aliasLookup.TryGetValue(trimmed, out metric);
  }

  /// <summary>
  /// Builds a descriptive error message for unknown metrics, including known identifiers and aliases.
  /// </summary>
  /// <param name="raw">Raw user input that failed resolution.</param>
  /// <returns>Human-friendly error message.</returns>
  public string BuildUnknownMetricMessage(string? raw)
  {
    var knownIdentifiers = string.Join(", ", Enum.GetNames<MetricIdentifier>());
    var aliasDescriptions = AliasesByMetric
      .Where(pair => pair.Value.Count > 0)
      .Select(pair => $"{pair.Key}: {string.Join(", ", pair.Value)}")
      .ToArray();

    var aliasText = aliasDescriptions.Length > 0
      ? $" Known aliases: {string.Join("; ", aliasDescriptions)}."
      : string.Empty;

    var input = string.IsNullOrWhiteSpace(raw) ? "(empty)" : raw.Trim();
    return $"Unknown metric identifier or alias '{input}'. Known identifiers: {knownIdentifiers}.{aliasText}";
  }

  private static IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> NormalizeAliases(
    IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> aliases)
  {
    if (aliases.Count == 0)
    {
      return new Dictionary<MetricIdentifier, IReadOnlyList<string>>();
    }

    var normalized = new Dictionary<MetricIdentifier, IReadOnlyList<string>>();
    foreach (var (identifier, values) in aliases)
    {
      var cleaned = (values ?? Array.Empty<string>())
        .Select(alias => alias?.Trim())
        .Where(alias => !string.IsNullOrWhiteSpace(alias)
                        && !alias.Equals(identifier.ToString(), StringComparison.OrdinalIgnoreCase))
        .Select(alias => alias!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

      if (cleaned.Length > 0)
      {
        normalized[identifier] = cleaned;
      }
    }

    return normalized;
  }

  private static Dictionary<string, MetricIdentifier> BuildAliasLookup(
    IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> aliasesByMetric)
  {
    var lookup = new Dictionary<string, MetricIdentifier>(StringComparer.OrdinalIgnoreCase);
    foreach (var (identifier, aliases) in aliasesByMetric)
    {
      foreach (var alias in aliases)
      {
        if (lookup.TryGetValue(alias, out var existing) && existing != identifier)
        {
          throw new ArgumentException(
            $"Alias '{alias}' is assigned to multiple metrics: '{existing}' and '{identifier}'.",
            nameof(aliasesByMetric));
        }

        lookup[alias] = identifier;
      }
    }

    return lookup;
  }
}


