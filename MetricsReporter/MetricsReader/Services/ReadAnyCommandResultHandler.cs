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
    var groups = BuildSymbolGroups(snapshots, parameters.GroupBy);
    var totalGroups = groups.Count;
    var limitedGroups = parameters.ShowAll ? groups : groups.Take(1).ToList();

    var response = new GroupedViolationsResponseDto<GroupedViolationsGroupDto<SymbolMetricDto>>
    {
      Metric = parameters.Metric,
      Namespace = parameters.Namespace,
      SymbolKind = parameters.SymbolKind,
      IncludeSuppressed = parameters.IncludeSuppressed,
      GroupBy = parameters.GroupBy.ToWireValue(),
      ViolationsGroupsCount = totalGroups,
      ViolationsGroups = limitedGroups
    };

    JsonConsoleWriter.Write(response);
  }

  private static List<GroupedViolationsGroupDto<SymbolMetricDto>> BuildSymbolGroups(
    IReadOnlyList<SymbolMetricSnapshot> snapshots,
    MetricsReaderGroupByOption groupBy)
  {
    var buckets = new Dictionary<string, SymbolGroupAccumulator>(StringComparer.Ordinal);
    var index = 0;
    foreach (var snapshot in snapshots)
    {
      var metadata = SymbolMetadataParser.Parse(snapshot.Symbol, snapshot.Kind);
      var key = ResolveGroupKey(snapshot, metadata, groupBy);
      if (!buckets.TryGetValue(key, out var accumulator))
      {
        var dto = new GroupedViolationsGroupDto<SymbolMetricDto>();
        AssignGroupKey(dto, groupBy, key);
        accumulator = new SymbolGroupAccumulator(index, dto);
        buckets[key] = accumulator;
      }

      accumulator.Dto.Violations.Add(SymbolMetricDto.FromSnapshot(snapshot));
      accumulator.Dto.ViolationsCount = accumulator.Dto.Violations.Count;
      index++;
    }

    return buckets
      .Values
      .OrderBy(acc => acc.Rank)
      .Select(acc => acc.Dto)
      .ToList();
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
    SymbolMetadata metadata,
    MetricsReaderGroupByOption option)
    => option switch
    {
      MetricsReaderGroupByOption.Metric => snapshot.Metric.ToString(),
      MetricsReaderGroupByOption.Namespace => metadata.Namespace,
      MetricsReaderGroupByOption.Type => metadata.TypeName,
      MetricsReaderGroupByOption.Method => metadata.MethodName ?? metadata.TypeName,
      _ => snapshot.Symbol
    };

  private sealed class SymbolGroupAccumulator
  {
    public SymbolGroupAccumulator(int rank, GroupedViolationsGroupDto<SymbolMetricDto> dto)
    {
      Rank = rank;
      Dto = dto;
    }

    public int Rank { get; }

    public GroupedViolationsGroupDto<SymbolMetricDto> Dto { get; }
  }
}

