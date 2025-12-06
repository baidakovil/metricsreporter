namespace MetricsReporter.Aggregation;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Encapsulates line index population logic so the workspace can focus on orchestration.
/// </summary>
internal sealed class LineIndexBuilder
{
  /// <summary>
  /// Builds the line indexes for members and types, using the provided lookup for filtering.
  /// </summary>
  public static void Build(
      LineIndex lineIndex,
      IEnumerable<MemberMetricsNode> members,
      IEnumerable<TypeEntry> types,
      AggregationWorkspaceLookup lookup)
  {
    foreach (var member in members)
    {
      if (!HasValidSource(member.Source))
      {
        continue;
      }

      if (lookup.ShouldExcludeMember(member))
      {
        continue;
      }

      var start = member.Source!.StartLine!.Value;
      var end = member.Source.EndLine ?? start;
      var normalizedPath = PathNormalizer.Normalize(member.Source.Path!);

      lineIndex.AddMember(normalizedPath, member, start, end);

      if (lookup.ResolveMemberAssemblyNode(member) is AssemblyMetricsNode memberAssembly)
      {
        lineIndex.RegisterFileAssembly(normalizedPath, memberAssembly);
      }
    }

    foreach (var typeEntry in types)
    {
      if (!HasValidSource(typeEntry.Node.Source))
      {
        continue;
      }

      if (lookup.ShouldExcludeType(typeEntry))
      {
        continue;
      }

      var start = typeEntry.Node.Source!.StartLine!.Value;
      var end = typeEntry.Node.Source.EndLine ?? start;
      var normalizedPath = PathNormalizer.Normalize(typeEntry.Node.Source.Path!);

      lineIndex.AddType(normalizedPath, typeEntry.Node, start, end);
      lineIndex.RegisterFileAssembly(normalizedPath, typeEntry.Assembly);
    }

    lineIndex.SortIndexes();
  }

  private static bool HasValidSource(SourceLocation? source)
      => source?.Path is not null && source.StartLine.HasValue;
}


