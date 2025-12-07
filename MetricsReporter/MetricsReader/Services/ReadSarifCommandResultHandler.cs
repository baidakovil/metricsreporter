namespace MetricsReporter.MetricsReader.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.MetricsReader.Output;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;
/// <summary>
/// Handles result formatting and output for ReadSarif command.
/// </summary>
internal sealed class ReadSarifCommandResultHandler : IReadSarifCommandResultHandler
{
  /// <inheritdoc/>
  public void WriteInvalidMetricError(string metricName)
  {
    ArgumentNullException.ThrowIfNull(metricName);
    var dto = new SarifInvalidMetricDto(
      metricName,
      $"Metric '{metricName}' does not expose SARIF rule breakdown data. Use SarifCaRuleViolations or SarifIdeRuleViolations.");
    JsonConsoleWriter.Write(dto);
  }
  /// <inheritdoc/>
  public void WriteNoViolationsFound(string metricName, string @namespace, string symbolKind, string? ruleId)
  {
    ArgumentNullException.ThrowIfNull(metricName);
    ArgumentNullException.ThrowIfNull(@namespace);
    ArgumentNullException.ThrowIfNull(symbolKind);
    var message = BuildNoViolationsMessage(metricName, @namespace, ruleId);
    var dto = new SarifNoViolationsFoundDto(metricName, @namespace, symbolKind, ruleId, message);
    JsonConsoleWriter.Write(dto);
  }
  /// <inheritdoc/>
  public void WriteResponse(SarifMetricSettings settings, IEnumerable<SarifViolationGroup> groups)
  {
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(groups);

    var response = SarifResponseBuilder.Build(settings, groups);
    JsonConsoleWriter.Write(response);
  }
  private static string BuildNoViolationsMessage(string metric, string @namespace, string? ruleId)
  {
    if (string.IsNullOrWhiteSpace(ruleId))
    {
      return $"No SARIF violations for metric '{metric}' were found within namespace '{@namespace}'.";
    }
    return $"No SARIF violations for metric '{metric}' and rule '{ruleId}' were found within namespace '{@namespace}'.";
  }

  private static List<GroupedViolationsGroupDto<SarifViolationDetailDto>> BuildRuleIdGroups(
    List<SarifViolationGroup> groups)
  {
    var result = new List<GroupedViolationsGroupDto<SarifViolationDetailDto>>(groups.Count);
    foreach (var group in groups)
    {
      var dto = new GroupedViolationsGroupDto<SarifViolationDetailDto>
      {
        RuleId = group.RuleId,
        ShortDescription = group.ShortDescription,
        ViolationsCount = group.Count,
        Violations = group.Violations.Select(SarifViolationDetailDto.FromModel).ToList()
      };
      result.Add(dto);
    }

    return result;
  }

  private static List<GroupedViolationsGroupDto<SarifViolationDetailDto>> BuildAggregatedGroups(
    IReadOnlyList<SarifViolationGroup> groups,
    MetricsReaderGroupByOption groupBy)
  {
    var builder = new SarifAggregatedGroupBuilder(groupBy);
    foreach (var group in groups)
    {
      builder.AddGroup(group);
    }

    return builder.Build();
  }

  private static void AssignGroupKey(
    GroupedViolationsGroupDto<SarifViolationDetailDto> dto,
    MetricsReaderGroupByOption option,
    string key)
  {
    switch (option)
    {
      case MetricsReaderGroupByOption.Metric:
        dto.Metric = key;
        break;
      case MetricsReaderGroupByOption.Namespace:
        dto.Namespace = key;
        break;
      case MetricsReaderGroupByOption.Type:
        dto.Type = key;
        break;
      case MetricsReaderGroupByOption.Method:
        dto.Method = key;
        break;
      case MetricsReaderGroupByOption.RuleId:
        dto.RuleId = key;
        break;
    }
  }

  private static string? ResolveSymbolGroupKey(SymbolMetadata metadata, MetricsReaderGroupByOption option)
    => option switch
    {
      MetricsReaderGroupByOption.Namespace => metadata.Namespace,
      MetricsReaderGroupByOption.Type => metadata.TypeName,
      MetricsReaderGroupByOption.Method => metadata.MethodName ?? metadata.TypeName,
      _ => metadata.Symbol
    };

  private static CodeElementKind InferKindFromSymbol(string? symbol)
    => string.IsNullOrWhiteSpace(symbol)
      ? CodeElementKind.Type
      : (symbol.Contains('(') ? CodeElementKind.Member : CodeElementKind.Type);

  /// <summary>
  /// Builds grouped SARIF response payloads.
  /// </summary>
  private static class SarifResponseBuilder
  {
    public static GroupedViolationsResponseDto<GroupedViolationsGroupDto<SarifViolationDetailDto>> Build(
      SarifMetricSettings settings,
      IEnumerable<SarifViolationGroup> groups)
    {
      var grouping = ResolveGroups(groups, settings.EffectiveGroupBy);
      return CreatePayload(settings, grouping);
    }

    private static GroupedViolationsResponseDto<GroupedViolationsGroupDto<SarifViolationDetailDto>> CreatePayload(
      SarifMetricSettings settings,
      SarifGroupingResult grouping)
      => new()
      {
        Metric = settings.EffectiveMetricName,
        Namespace = settings.Namespace.Trim(),
        SymbolKind = settings.SymbolKind.ToString(),
        IncludeSuppressed = settings.IncludeSuppressed,
        GroupBy = grouping.GroupBy.ToWireValue(),
        ViolationsGroupsCount = grouping.Groups.Count,
        ViolationsGroups = grouping.GetLimitedGroups(settings.ShowAll),
        RuleId = settings.RuleId
      };

    private static SarifGroupingResult ResolveGroups(
      IEnumerable<SarifViolationGroup> groups,
      MetricsReaderGroupByOption groupBy)
    {
      var materialized = groups.ToList();
      var groupedDtos = groupBy == MetricsReaderGroupByOption.RuleId
        ? BuildRuleIdGroups(materialized)
        : BuildAggregatedGroups(materialized, groupBy);
      return new SarifGroupingResult(groupBy, groupedDtos);
    }

    private sealed record SarifGroupingResult(
      MetricsReaderGroupByOption GroupBy,
      List<GroupedViolationsGroupDto<SarifViolationDetailDto>> Groups)
    {
      public List<GroupedViolationsGroupDto<SarifViolationDetailDto>> GetLimitedGroups(bool includeAll)
        => includeAll ? Groups : Groups.Take(1).ToList();
    }
  }

  /// <summary>
  /// Aggregates SARIF violation groups into DTOs for grouping modes other than rule id.
  /// </summary>
  private sealed class SarifAggregatedGroupBuilder
  {
    private readonly MetricsReaderGroupByOption _groupBy;
    private readonly Dictionary<string, SarifGroupedAccumulator> _buckets;
    private int _rankSeed;

    public SarifAggregatedGroupBuilder(MetricsReaderGroupByOption groupBy)
    {
      _groupBy = groupBy;
      _buckets = new Dictionary<string, SarifGroupedAccumulator>(StringComparer.Ordinal);
    }

    public void AddGroup(SarifViolationGroup group)
    {
      switch (_groupBy)
      {
        case MetricsReaderGroupByOption.Metric:
          AccumulateMetricGroup(group);
          break;
        case MetricsReaderGroupByOption.Namespace:
        case MetricsReaderGroupByOption.Type:
        case MetricsReaderGroupByOption.Method:
          AccumulateSymbolGroup(group);
          break;
        default:
          throw new InvalidOperationException($"Grouping '{_groupBy}' is not supported for readsarif.");
      }
    }

    public List<GroupedViolationsGroupDto<SarifViolationDetailDto>> Build()
    {
      foreach (var accumulator in _buckets.Values)
      {
        accumulator.Dto.ViolationsCount = accumulator.ViolationCount;
      }

      return _buckets
        .Values
        .OrderBy(acc => acc.Rank)
        .Select(acc => acc.Dto)
        .ToList();
    }

    private void AccumulateMetricGroup(SarifViolationGroup group)
    {
      var accumulator = GetOrCreate(group.Metric.ToString());
      SarifMetricBucketAppender.Append(accumulator, group);
    }

    private void AccumulateSymbolGroup(SarifViolationGroup group)
    {
      foreach (var contribution in group.SymbolContributions)
      {
        var key = SarifSymbolGroupKey.FromContribution(contribution, _groupBy);
        if (string.IsNullOrWhiteSpace(key))
        {
          continue;
        }

        var accumulator = GetOrCreate(key);
        accumulator.AddCount(contribution.Count);
      }

      if (group.Violations.Count == 0)
      {
        return;
      }

      foreach (var violation in group.Violations)
      {
        var key = SarifSymbolGroupKey.FromViolation(violation, _groupBy);
        if (string.IsNullOrWhiteSpace(key) || !_buckets.TryGetValue(key, out var accumulator))
        {
          continue;
        }

        accumulator.Dto.Violations.Add(SarifViolationDetailDto.FromModel(violation));
      }
    }

    private SarifGroupedAccumulator GetOrCreate(string key)
    {
      if (_buckets.TryGetValue(key, out var accumulator))
      {
        return accumulator;
      }

      var dto = new GroupedViolationsGroupDto<SarifViolationDetailDto>();
      AssignGroupKey(dto, _groupBy, key);
      accumulator = new SarifGroupedAccumulator(_rankSeed++, dto);
      _buckets[key] = accumulator;
      return accumulator;
    }
  }

  private static class SarifSymbolGroupKey
  {
    public static string? FromContribution(SarifSymbolContribution contribution, MetricsReaderGroupByOption option)
    {
      var metadata = SymbolMetadataParser.Parse(contribution.Symbol, contribution.Kind);
      return ResolveSymbolGroupKey(metadata, option);
    }

    public static string? FromViolation(SarifViolationRecord violation, MetricsReaderGroupByOption option)
    {
      var kind = InferKindFromSymbol(violation.Symbol);
      var metadata = SymbolMetadataParser.Parse(violation.Symbol, kind);
      return ResolveSymbolGroupKey(metadata, option);
    }
  }

  private static class SarifMetricBucketAppender
  {
    public static void Append(SarifGroupedAccumulator accumulator, SarifViolationGroup group)
    {
      accumulator.AddCount(group.Count);

      if (group.Violations.Count == 0)
      {
        return;
      }

      accumulator.Dto.Violations.AddRange(group.Violations.Select(SarifViolationDetailDto.FromModel));
    }
  }

  private sealed class SarifGroupedAccumulator
  {
    public SarifGroupedAccumulator(int rank, GroupedViolationsGroupDto<SarifViolationDetailDto> dto)
    {
      Rank = rank;
      Dto = dto;
    }

    public int Rank { get; }

    public GroupedViolationsGroupDto<SarifViolationDetailDto> Dto { get; }

    public int ViolationCount { get; private set; }

    public void AddCount(int amount)
    {
      if (amount > 0)
      {
        ViolationCount += amount;
      }
    }
  }
}

