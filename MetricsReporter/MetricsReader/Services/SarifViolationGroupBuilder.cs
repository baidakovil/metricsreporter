namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Builder for creating SARIF violation groups from aggregated violations.
/// </summary>
internal sealed class SarifViolationGroupBuilder
{
  private readonly Dictionary<string, SarifSymbolContribution> _symbolContributions = new(StringComparer.Ordinal);

  /// <summary>
  /// Initializes a new instance of the <see cref="SarifViolationGroupBuilder"/> class.
  /// </summary>
  /// <param name="ruleId">The rule ID for this group.</param>
  /// <param name="shortDescription">Optional short description of the rule.</param>
  /// <param name="metric">The metric that produced the violations.</param>
  public SarifViolationGroupBuilder(string ruleId, string? shortDescription, MetricIdentifier metric)
  {
    RuleId = ruleId;
    ShortDescription = shortDescription;
    Metric = metric;
  }

  /// <summary>
  /// Gets the rule ID for this group.
  /// </summary>
  public string RuleId { get; }

  /// <summary>
  /// Gets the optional short description of the rule.
  /// </summary>
  public string? ShortDescription { get; }

  /// <summary>
  /// Gets the metric that produced the current group.
  /// </summary>
  public MetricIdentifier Metric { get; }

  /// <summary>
  /// Gets or sets the total count of violations in this group.
  /// </summary>
  public int Count { get; private set; }

  /// <summary>
  /// Gets the list of violation records in this group.
  /// </summary>
  public List<SarifViolationRecord> Violations { get; } = [];

  /// <summary>
  /// Builds a SARIF violation group from the accumulated data.
  /// </summary>
  /// <returns>A SARIF violation group.</returns>
  public SarifViolationGroup Build()
    => new(RuleId, ShortDescription, Metric, Count, Violations, _symbolContributions.Values.ToList());

  /// <summary>
  /// Adds violations from a breakdown entry to this group.
  /// </summary>
  /// <param name="count">The count of violations to add.</param>
  /// <param name="violations">The list of violation details to add.</param>
  /// <param name="node">The metrics node these violations belong to.</param>
  public void Add(int count, IReadOnlyList<SarifRuleViolationDetail> violations, MetricsNode node)
  {
    var detailCount = violations?.Count ?? 0;
    if (count > 0)
    {
      Count += count;
    }

    var contributionIncrement = count > 0 ? count : detailCount;
    if (contributionIncrement > 0)
    {
      AddContribution(node, contributionIncrement);
    }

    if (violations is null || violations.Count == 0)
    {
      return;
    }

    var symbol = node.FullyQualifiedName ?? node.Name ?? string.Empty;
    foreach (var violation in violations)
    {
      Violations.Add(new SarifViolationRecord(
        symbol,
        violation.Message,
        violation.Uri,
        violation.StartLine,
        violation.EndLine));
    }
  }

  private void AddContribution(MetricsNode node, int increment)
  {
    var symbol = node.FullyQualifiedName ?? node.Name ?? string.Empty;
    if (!_symbolContributions.TryGetValue(symbol, out var contribution))
    {
      contribution = new SarifSymbolContribution(symbol, node.Kind);
      _symbolContributions[symbol] = contribution;
    }

    contribution.Increment(increment);
  }
}

/// <summary>
/// Represents aggregated contribution information for a symbol within a SARIF group.
/// </summary>
internal sealed class SarifSymbolContribution
{
  public SarifSymbolContribution(string symbol, CodeElementKind kind)
  {
    Symbol = symbol;
    Kind = kind;
  }

  public string Symbol { get; }

  public CodeElementKind Kind { get; }

  public int Count { get; private set; }

  public void Increment(int amount)
  {
    if (amount > 0)
    {
      Count += amount;
    }
  }
}

