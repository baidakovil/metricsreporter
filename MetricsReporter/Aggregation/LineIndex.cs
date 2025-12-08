namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MetricsReporter.Model;

/// <summary>
/// Builds and queries line-based indexes for members and types.
/// </summary>
internal sealed class LineIndex
{
  private readonly Dictionary<string, List<IndexedNode>> _memberLineIndex = new(StringComparer.OrdinalIgnoreCase);
  private readonly Dictionary<string, List<IndexedNode>> _typeLineIndex = new(StringComparer.OrdinalIgnoreCase);
  private readonly Dictionary<string, AssemblyMetricsNode> _fileAssemblyMap = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Adds a member to the member index.
  /// </summary>
  public void AddMember(string normalizedPath, MemberMetricsNode member, int startLine, int endLine)
  {
    var list = GetOrCreateIndexList(_memberLineIndex, normalizedPath);
    list.Add(new IndexedNode(member, startLine, endLine));
  }

  /// <summary>
  /// Adds a type to the type index.
  /// </summary>
  public void AddType(string normalizedPath, TypeMetricsNode type, int startLine, int endLine)
  {
    var list = GetOrCreateIndexList(_typeLineIndex, normalizedPath);
    list.Add(new IndexedNode(type, startLine, endLine));
  }

  /// <summary>
  /// Registers the assembly that owns a file path.
  /// </summary>
  public void RegisterFileAssembly(string normalizedPath, AssemblyMetricsNode assembly)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    _fileAssemblyMap[normalizedPath] = assembly;
  }

  /// <summary>
  /// Sorts all indexes for faster binary search.
  /// </summary>
  public void SortIndexes()
  {
    foreach (var list in _memberLineIndex.Values)
    {
      list.Sort(static (a, b) => a.StartLine.CompareTo(b.StartLine));
    }

    foreach (var list in _typeLineIndex.Values)
    {
      list.Sort(static (a, b) => a.StartLine.CompareTo(b.StartLine));
    }
  }

  /// <summary>
  /// Finds a metrics node that contains the specified line.
  /// </summary>
  /// <remarks>
  /// This method prefers members over types. If a member starts exactly at the specified line,
  /// it will be selected even if a type also contains that line. This ensures that SARIF violations
  /// on method declaration lines are correctly attributed to the method rather than the containing type.
  /// </remarks>
  public MetricsNode? FindNode(string normalizedPath, int line)
  {
    // WHY: We check for members first and prioritize exact start line matches. This ensures that
    // when a SARIF violation is on a method declaration line (e.g., line 159 where the method starts),
    // we correctly map it to the method rather than falling back to the type. Without this prioritization,
    // methods that start at the violation line might be missed if the index lookup doesn't find them first.
    var memberNode = FindNodeInIndex(_memberLineIndex, normalizedPath, line);
    if (memberNode is not null)
    {
      return memberNode;
    }

    return FindNodeInIndex(_typeLineIndex, normalizedPath, line);
  }

  /// <summary>
  /// Tries to lookup the assembly associated with the specified file.
  /// </summary>
  public bool TryGetAssembly(string normalizedPath, [MaybeNullWhen(false)] out AssemblyMetricsNode assembly)
      => _fileAssemblyMap.TryGetValue(normalizedPath, out assembly);

  private static List<IndexedNode> GetOrCreateIndexList(
      Dictionary<string, List<IndexedNode>> index,
      string path)
  {
    if (!index.TryGetValue(path, out var list))
    {
      list = [];
      index[path] = list;
    }

    return list;
  }

  private static MetricsNode? FindNodeInIndex(
      Dictionary<string, List<IndexedNode>> index,
      string path,
      int line)
  {
    if (!index.TryGetValue(path, out var list))
    {
      return null;
    }

    var exactStart = MatchSelection.Create();
    var nearStart = MatchSelection.Create();
    var containing = MatchSelection.Create();

    foreach (var node in list)
    {
      var length = node.EndLine - node.StartLine;

      switch (GetMatchKind(line, node))
      {
        case MatchKind.ExactStart:
          exactStart.Consider(node.Node, length);
          break;
        case MatchKind.NearStart:
          nearStart.Consider(node.Node, length);
          break;
        case MatchKind.Contains:
          containing.Consider(node.Node, length);
          break;
        case MatchKind.None:
        default:
          continue;
      }
    }

    var selectedNode = exactStart.Node ?? nearStart.Node ?? containing.Node;
    if (selectedNode is not null)
    {
      return selectedNode;
    }

    // If no containing node found, handle single-line indexed methods (Roslyn parser limitation)
    // When a method is indexed as StartLine=EndLine (single line), violations after that line
    // may belong to the method if no other method starts before the violation.
    return FindNodeForSingleLineIndexedMethod(list, line);
  }

  private static MatchKind GetMatchKind(int targetLine, IndexedNode node)
  {
    // WHY: We prioritize nodes that start exactly at the specified line or one line after it.
    // This handles cases where SARIF reports violations on method declaration lines (line N),
    // but the method index may start at line N+1 due to how Roslyn determines method boundaries.
    // For example, if SARIF reports a violation on line 159 (method declaration), but the method
    // is indexed starting at line 160 (method body start), we should still match it to the method.
    if (targetLine == node.StartLine)
    {
      return MatchKind.ExactStart;
    }

    if (targetLine == node.StartLine - 1)
    {
      return MatchKind.NearStart;
    }

    if (targetLine >= node.StartLine && targetLine <= node.EndLine)
    {
      return MatchKind.Contains;
    }

    return MatchKind.None;
  }

  /// <summary>
  /// Finds a method when methods are indexed as single lines (StartLine == EndLine).
  /// </summary>
  /// <remarks>
  /// This handles the case where Roslyn Metrics Parser only provides the method declaration line,
  /// not the full method range. When a violation is on a line after the method declaration,
  /// we attribute it to the closest preceding method that doesn't have another method between it and the violation.
  /// </remarks>
  private static MetricsNode? FindNodeForSingleLineIndexedMethod(List<IndexedNode> sortedList, int line)
  {
    var candidate = FindLastSingleLineMethod(sortedList, line);
    if (candidate is null)
    {
      return null;
    }

    return HasInterveningMethod(sortedList, candidate.Value.StartLine, line)
      ? null
      : candidate.Value.Node;
  }

  private static IndexedNode? FindLastSingleLineMethod(List<IndexedNode> sortedList, int line)
  {
    IndexedNode? candidate = null;

    foreach (var node in sortedList)
    {
      // Only consider single-line indexed methods (Roslyn limitation)
      if (node.StartLine != node.EndLine)
      {
        continue;
      }

      // Method must start before or at the violation line
      if (node.StartLine > line)
      {
        break; // List is sorted, so we can stop here
      }

      // Keep track of the method that starts closest to (but not after) the violation line
      if (candidate.HasValue && node.StartLine <= candidate.Value.StartLine)
      {
        continue;
      }

      candidate = node;
    }

    return candidate;
  }

  private static bool HasInterveningMethod(List<IndexedNode> sortedList, int candidateStartLine, int line)
  {
    foreach (var node in sortedList)
    {
      if (node.StartLine <= candidateStartLine)
      {
        continue;
      }

      if (node.StartLine >= line)
      {
        return false;
      }

      return true;
    }

    return false;
  }

  private struct MatchSelection
  {
    private int _bestLength;
    public MetricsNode? Node { get; private set; }

    private MatchSelection(int bestLength, MetricsNode? node)
    {
      _bestLength = bestLength;
      Node = node;
    }

    public static MatchSelection Create() => new(int.MaxValue, null);

    public void Consider(MetricsNode candidate, int length)
    {
      if (length >= _bestLength)
      {
        return;
      }

      _bestLength = length;
      Node = candidate;
    }
  }

  private enum MatchKind
  {
    None,
    ExactStart,
    NearStart,
    Contains
  }

  private readonly record struct IndexedNode(MetricsNode Node, int StartLine, int EndLine);
}


