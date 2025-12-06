namespace MetricsReporter.MetricsReader.Output;

using System.Collections.Generic;

/// <summary>
/// Represents the grouped response envelope shared by metrics-reader commands.
/// </summary>
internal sealed class GroupedViolationsResponseDto<TGroup>
{
  public string Metric { get; init; } = string.Empty;

  public string Namespace { get; init; } = string.Empty;

  public string SymbolKind { get; init; } = string.Empty;

  public bool IncludeSuppressed { get; init; }

  public string GroupBy { get; init; } = string.Empty;

  public int ViolationsGroupsCount { get; init; }

  public List<TGroup> ViolationsGroups { get; init; } = [];

  public string? RuleId { get; init; }
}

/// <summary>
/// Represents a single group inside a grouped violations response.
/// </summary>
internal sealed class GroupedViolationsGroupDto<TViolationDto>
{
  public string? Metric { get; set; }

  public string? Namespace { get; set; }

  public string? Type { get; set; }

  public string? Method { get; set; }

  public string? RuleId { get; set; }

  public string? ShortDescription { get; set; }

  public int ViolationsCount { get; set; }

  public List<TViolationDto> Violations { get; set; } = [];
}

