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

    // WHY: We prioritize nodes that start exactly at the specified line or one line after it.
    // This handles cases where SARIF reports violations on method declaration lines (line N),
    // but the method index may start at line N+1 due to how Roslyn determines method boundaries.
    // For example, if SARIF reports a violation on line 159 (method declaration), but the method
    // is indexed starting at line 160 (method body start), we should still match it to the method.
    MetricsNode? bestExactStartMatch = null;
    var bestExactStartLength = int.MaxValue;
    MetricsNode? bestNearStartMatch = null;
    var bestNearStartLength = int.MaxValue;
    MetricsNode? bestContainingNode = null;
    var bestContainingLength = int.MaxValue;

    foreach (var node in list)
    {
      var length = node.EndLine - node.StartLine;

      // Prefer exact start line match (e.g., method declaration line)
      if (line == node.StartLine)
      {
        // Among nodes starting at this line, prefer the shortest one
        if (length < bestExactStartLength)
        {
          bestExactStartLength = length;
          bestExactStartMatch = node.Node;
        }
      }
      else if (line == node.StartLine - 1)
      {
        // Handle case where SARIF reports violation on declaration line (N), but method indexed at body start (N+1)
        // This handles the common case where Roslyn indexes method body start, but SARIF reports on declaration.
        // Prefer shorter nodes among those starting one line after the violation.
        if (length < bestNearStartLength)
        {
          bestNearStartLength = length;
          bestNearStartMatch = node.Node;
        }
      }
      else if (line >= node.StartLine && line <= node.EndLine)
      {
        // Track the shortest containing node as fallback
        if (length < bestContainingLength)
        {
          bestContainingLength = length;
          bestContainingNode = node.Node;
        }
      }
    }

    // If no containing node found, handle single-line indexed methods (Roslyn parser limitation)
    // When a method is indexed as StartLine=EndLine (single line), violations after that line
    // may belong to the method if no other method starts before the violation.
    if (bestExactStartMatch is null && bestNearStartMatch is null && bestContainingNode is null)
    {
      return FindNodeForSingleLineIndexedMethod(list, line);
    }

    // Return in priority order: exact start match > near start match > shortest containing node
    return bestExactStartMatch ?? bestNearStartMatch ?? bestContainingNode;
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
    // Find the last method that starts before or at the violation line
    IndexedNode? candidate = null;
    int candidateStartLine = -1;

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
      if (node.StartLine > candidateStartLine)
      {
        candidate = node;
        candidateStartLine = node.StartLine;
      }
    }

    // If we found a candidate, verify no other method starts between it and the violation
    if (candidate.HasValue)
    {
      foreach (var node in sortedList)
      {
        // Check if there's another method between candidate and violation
        if (node.StartLine > candidateStartLine && node.StartLine < line)
        {
          // Another method exists between candidate and violation - don't attribute to candidate
          return null;
        }

        // Stop searching once we've passed the violation line
        if (node.StartLine > line)
        {
          break;
        }
      }

      return candidate.Value.Node;
    }

    return null;
  }

  private readonly record struct IndexedNode(MetricsNode Node, int StartLine, int EndLine);
}


