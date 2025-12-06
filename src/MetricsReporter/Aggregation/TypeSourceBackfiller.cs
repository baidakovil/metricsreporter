namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.Model;

/// <summary>
/// Populates missing type source information by inferring it from member metadata.
/// </summary>
internal static class TypeSourceBackfiller
{
  /// <summary>
  /// Ensures every type node with at least one sourced member exposes source metadata.
  /// </summary>
  /// <param name="types">Type entries tracked by the aggregation workspace.</param>
  public static void PopulateMissingSources(IEnumerable<TypeEntry> types)
  {
    ArgumentNullException.ThrowIfNull(types);

    foreach (var entry in types)
    {
      if (entry is null)
      {
        continue;
      }

      var existingSource = entry.Node.Source;
      var hasPath = !string.IsNullOrWhiteSpace(existingSource?.Path);
      var hasCompleteSource = HasValidSource(existingSource);

      if (hasPath && hasCompleteSource)
      {
        continue;
      }

      var preferredGroup = SelectPreferredMemberGroup(entry.Node.Members);
      if (preferredGroup is null)
      {
        continue;
      }

      var path = hasPath
          ? existingSource!.Path!
          : preferredGroup.First().Source!.Path!;

      var candidateStartLine = preferredGroup.Min(member => member.Source!.StartLine!.Value);
      var candidateEndLine = preferredGroup.Max(member => member.Source!.EndLine ?? member.Source!.StartLine!.Value);

      var startLine = existingSource?.StartLine ?? candidateStartLine;
      var endLine = existingSource?.EndLine ?? candidateEndLine;

      entry.Node.Source = new SourceLocation
      {
        Path = path,
        StartLine = startLine,
        EndLine = endLine
      };
    }
  }

  private static IGrouping<string, MemberMetricsNode>? SelectPreferredMemberGroup(IEnumerable<MemberMetricsNode> members)
  {
    ArgumentNullException.ThrowIfNull(members);

    return members
        .Where(member => HasValidSource(member.Source))
        .GroupBy(member => PathNormalizer.Normalize(member.Source!.Path!))
        .OrderByDescending(group => group.Count())
        .ThenBy(group => group.Min(member => member.Source!.StartLine!.Value))
        .FirstOrDefault();
  }

  private static bool HasValidSource(SourceLocation? source)
      => source?.Path is not null && source.StartLine.HasValue;
}


