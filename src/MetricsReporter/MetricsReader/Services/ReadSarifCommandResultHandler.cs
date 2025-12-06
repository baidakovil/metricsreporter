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

    var groupList = groups.ToList();
    var groupBy = settings.EffectiveGroupBy;
    var groupedDtos = groupBy == MetricsReaderGroupByOption.RuleId
      ? BuildRuleIdGroups(groupList)
      : BuildAggregatedGroups(groupList, groupBy);

    var totalGroups = groupedDtos.Count;
    var limitedGroups = settings.ShowAll
      ? groupedDtos
      : groupedDtos.Take(1).ToList();

    var response = new GroupedViolationsResponseDto<GroupedViolationsGroupDto<SarifViolationDetailDto>>
    {
      Metric = settings.EffectiveMetricName,
      Namespace = settings.Namespace.Trim(),
      SymbolKind = settings.SymbolKind.ToString(),
      IncludeSuppressed = settings.IncludeSuppressed,
      GroupBy = groupBy.ToWireValue(),
      ViolationsGroupsCount = totalGroups,
      ViolationsGroups = limitedGroups,
      RuleId = settings.RuleId
    };

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
    IReadOnlyList<SarifViolationGroup> groups)
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
    var buckets = new Dictionary<string, SarifGroupedAccumulator>(StringComparer.Ordinal);
    var rankSeed = 0;

    foreach (var group in groups)
    {
      switch (groupBy)
      {
        case MetricsReaderGroupByOption.Metric:
          AccumulateMetricBucket(buckets, group, ref rankSeed);
          break;
        case MetricsReaderGroupByOption.Namespace:
        case MetricsReaderGroupByOption.Type:
        case MetricsReaderGroupByOption.Method:
          AccumulateSymbolBuckets(buckets, group, groupBy, ref rankSeed);
          break;
        default:
          throw new InvalidOperationException($"Grouping '{groupBy}' is not supported for readsarif.");
      }
    }

    foreach (var accumulator in buckets.Values)
    {
      accumulator.Dto.ViolationsCount = accumulator.ViolationCount;
    }

    return buckets
      .Values
      .OrderBy(acc => acc.Rank)
      .Select(acc => acc.Dto)
      .ToList();
  }

  private static void AccumulateMetricBucket(
    Dictionary<string, SarifGroupedAccumulator> buckets,
    SarifViolationGroup group,
    ref int rankSeed)
  {
    var key = group.Metric.ToString();
    var accumulator = GetOrCreateAccumulator(buckets, key, MetricsReaderGroupByOption.Metric, ref rankSeed);
    accumulator.AddCount(group.Count);

    if (group.Violations.Count > 0)
    {
      accumulator.Dto.Violations.AddRange(group.Violations.Select(SarifViolationDetailDto.FromModel));
    }
  }

  private static void AccumulateSymbolBuckets(
    Dictionary<string, SarifGroupedAccumulator> buckets,
    SarifViolationGroup group,
    MetricsReaderGroupByOption groupBy,
    ref int rankSeed)
  {
    foreach (var contribution in group.SymbolContributions)
    {
      var metadata = SymbolMetadataParser.Parse(contribution.Symbol, contribution.Kind);
      var key = ResolveSymbolGroupKey(metadata, groupBy);
      if (string.IsNullOrWhiteSpace(key))
      {
        continue;
      }

      var accumulator = GetOrCreateAccumulator(buckets, key, groupBy, ref rankSeed);
      accumulator.AddCount(contribution.Count);
    }

    if (group.Violations.Count == 0)
    {
      return;
    }

    foreach (var violation in group.Violations)
    {
      var kind = InferKindFromSymbol(violation.Symbol);
      var metadata = SymbolMetadataParser.Parse(violation.Symbol, kind);
      var key = ResolveSymbolGroupKey(metadata, groupBy);
      if (string.IsNullOrWhiteSpace(key))
      {
        continue;
      }

      if (buckets.TryGetValue(key, out var accumulator))
      {
        accumulator.Dto.Violations.Add(SarifViolationDetailDto.FromModel(violation));
      }
    }
  }

  private static SarifGroupedAccumulator GetOrCreateAccumulator(
    Dictionary<string, SarifGroupedAccumulator> buckets,
    string key,
    MetricsReaderGroupByOption groupBy,
    ref int rankSeed)
  {
    if (buckets.TryGetValue(key, out var accumulator))
    {
      return accumulator;
    }

    var dto = new GroupedViolationsGroupDto<SarifViolationDetailDto>();
    AssignGroupKey(dto, groupBy, key);
    accumulator = new SarifGroupedAccumulator(rankSeed++, dto);
    buckets[key] = accumulator;
    return accumulator;
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

