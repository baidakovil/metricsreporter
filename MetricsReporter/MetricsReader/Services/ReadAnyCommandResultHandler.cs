namespace MetricsReporter.MetricsReader.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.MetricsReader.Output;

/// <summary>
/// Handles result formatting and output for ReadAny command.
/// </summary>
internal sealed class ReadAnyCommandResultHandler : IReadAnyCommandResultHandler
{
  /// <inheritdoc/>
  public void HandleResults(IEnumerable<SymbolMetricSnapshot> snapshots, ReadAnyCommandResultParameters parameters)
  {
    ArgumentNullException.ThrowIfNull(snapshots);
    ArgumentNullException.ThrowIfNull(parameters);

    var snapshotList = snapshots.ToList();
    if (snapshotList.Count == 0)
    {
      var noViolationsDto = new NoViolationsFoundDto(
        parameters.Metric,
        parameters.Namespace,
        parameters.SymbolKind,
        $"No violations were found for metric '{parameters.Metric}' in namespace '{parameters.Namespace}'.");
      JsonConsoleWriter.Write(noViolationsDto);
      return;
    }

    if (parameters.GroupBy == MetricsReaderGroupByOption.None)
    {
      WriteLegacyResponse(snapshotList, parameters);
    }
    else
    {
      WriteGroupedResponse(snapshotList, parameters);
    }
  }

  private static void WriteLegacyResponse(IReadOnlyList<SymbolMetricSnapshot> snapshots, ReadAnyCommandResultParameters parameters)
  {
    var dtos = snapshots.Select(SymbolMetricDto.FromSnapshot).ToList();

    if (parameters.ShowAll)
    {
      JsonConsoleWriter.Write(dtos);
      return;
    }

    JsonConsoleWriter.Write(dtos.First());
  }

  private static void WriteGroupedResponse(IReadOnlyList<SymbolMetricSnapshot> snapshots, ReadAnyCommandResultParameters parameters)
  {
    var response = SymbolResponseBuilder.Build(snapshots, parameters);
    JsonConsoleWriter.Write(response);
  }

  private static List<GroupedViolationsGroupDto<SymbolMetricDto>> BuildSymbolGroups(
    IReadOnlyList<SymbolMetricSnapshot> snapshots,
    MetricsReaderGroupByOption groupBy)
  {
    var buckets = new GroupedViolationsCollection(groupBy);
    foreach (var snapshot in snapshots)
    {
      buckets.Add(snapshot);
    }

    return buckets.ToList();
  }

  private static void AssignGroupKey(
    GroupedViolationsGroupDto<SymbolMetricDto> dto,
    MetricsReaderGroupByOption option,
    string value)
  {
    switch (option)
    {
      case MetricsReaderGroupByOption.Metric:
        dto.Metric = value;
        break;
      case MetricsReaderGroupByOption.Method:
        dto.Method = value;
        break;
      case MetricsReaderGroupByOption.Type:
        dto.Type = value;
        break;
      case MetricsReaderGroupByOption.Namespace:
        dto.Namespace = value;
        break;
      default:
        break;
    }
  }

  private static string ResolveGroupKey(
    SymbolMetricSnapshot snapshot,
    MetricsReaderGroupByOption option)
  {
    if (option == MetricsReaderGroupByOption.Metric)
    {
      return snapshot.Metric.ToString();
    }

    var metadata = SymbolMetadataParser.Parse(snapshot.Symbol, snapshot.Kind);

    return option switch
    {
      MetricsReaderGroupByOption.Namespace => metadata.Namespace,
      MetricsReaderGroupByOption.Type => metadata.TypeName,
      MetricsReaderGroupByOption.Method => metadata.MethodName ?? metadata.TypeName,
      _ => snapshot.Symbol
    };
  }

  private static class SymbolResponseBuilder
  {
    public static GroupedViolationsResponseDto<GroupedViolationsGroupDto<SymbolMetricDto>> Build(
      IReadOnlyList<SymbolMetricSnapshot> snapshots,
      ReadAnyCommandResultParameters parameters)
    {
      var groups = BuildSymbolGroups(snapshots, parameters.GroupBy);
      return new GroupedViolationsResponseDto<GroupedViolationsGroupDto<SymbolMetricDto>>
      {
        Metric = parameters.Metric,
        Namespace = parameters.Namespace,
        SymbolKind = parameters.SymbolKind,
        IncludeSuppressed = parameters.IncludeSuppressed,
        GroupBy = parameters.GroupBy.ToWireValue(),
        ViolationsGroupsCount = groups.Count,
        ViolationsGroups = LimitGroups(groups, parameters.ShowAll)
      };
    }

    private static List<GroupedViolationsGroupDto<SymbolMetricDto>> LimitGroups(
      List<GroupedViolationsGroupDto<SymbolMetricDto>> groups,
      bool includeAll)
      => includeAll ? groups : groups.Take(1).ToList();
  }

  /// <summary>
  /// Maintains grouped violation DTOs while preserving the order in which groups appear.
  /// </summary>
  private sealed class GroupedViolationsCollection
  {
    private readonly MetricsReaderGroupByOption _groupBy;
    private readonly Dictionary<string, GroupedViolationsGroupDto<SymbolMetricDto>> _buckets;
    private readonly List<GroupedViolationsGroupDto<SymbolMetricDto>> _orderedGroups;

    public GroupedViolationsCollection(MetricsReaderGroupByOption groupBy)
    {
      _groupBy = groupBy;
      _buckets = new Dictionary<string, GroupedViolationsGroupDto<SymbolMetricDto>>(StringComparer.Ordinal);
      _orderedGroups = new List<GroupedViolationsGroupDto<SymbolMetricDto>>();
    }

    public void Add(SymbolMetricSnapshot snapshot)
    {
      var group = GetOrCreateGroup(snapshot);
      group.Violations.Add(SymbolMetricDto.FromSnapshot(snapshot));
      group.ViolationsCount = group.Violations.Count;
    }

    public List<GroupedViolationsGroupDto<SymbolMetricDto>> ToList()
      => new(_orderedGroups);

    private GroupedViolationsGroupDto<SymbolMetricDto> GetOrCreateGroup(SymbolMetricSnapshot snapshot)
    {
      var key = ResolveGroupKey(snapshot, _groupBy);
      if (_buckets.TryGetValue(key, out var existing))
      {
        return existing;
      }

      var dto = new GroupedViolationsGroupDto<SymbolMetricDto>();
      AssignGroupKey(dto, _groupBy, key);
      _buckets[key] = dto;
      _orderedGroups.Add(dto);
      return dto;
    }
  }
}

