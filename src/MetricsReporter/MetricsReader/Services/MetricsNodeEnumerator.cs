namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using System.Linq;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Enumerates metrics nodes from a metrics report based on filter criteria.
/// </summary>
internal sealed class MetricsNodeEnumerator : IMetricsNodeEnumerator
{
  private readonly MetricsReport _report;

  /// <summary>
  /// Initializes a new instance of the <see cref="MetricsNodeEnumerator"/> class.
  /// </summary>
  /// <param name="report">The metrics report to enumerate nodes from.</param>
  public MetricsNodeEnumerator(MetricsReport report)
  {
    _report = report ?? throw new System.ArgumentNullException(nameof(report));
  }

  /// <inheritdoc/>
  public IEnumerable<TypeMetricsNode> EnumerateTypeNodes()
  {
    foreach (var assembly in _report.Solution.Assemblies)
    {
      foreach (var ns in assembly.Namespaces)
      {
        foreach (var type in ns.Types)
        {
          yield return type;
        }
      }
    }
  }

  /// <inheritdoc/>
  public IEnumerable<MemberMetricsNode> EnumerateMemberNodes()
  {
    foreach (var type in EnumerateTypeNodes())
    {
      foreach (var member in type.Members)
      {
        yield return member;
      }
    }
  }

  /// <inheritdoc/>
  public IEnumerable<MetricsNode> EnumerateNodes(SymbolFilter filter)
  {
    return filter.SymbolKind switch
    {
      MetricsReaderSymbolKind.Type => EnumerateTypeNodes()
        .Where(type => NamespaceMatcher.Matches(type.FullyQualifiedName, filter.Namespace))
        .Cast<MetricsNode>(),
      MetricsReaderSymbolKind.Member => EnumerateMemberNodes()
        .Where(member => NamespaceMatcher.Matches(member.FullyQualifiedName, filter.Namespace))
        .Cast<MetricsNode>(),
      MetricsReaderSymbolKind.Any => EnumerateTypeNodes()
        .Where(type => NamespaceMatcher.Matches(type.FullyQualifiedName, filter.Namespace))
        .Cast<MetricsNode>()
        .Concat(EnumerateMemberNodes()
          .Where(member => NamespaceMatcher.Matches(member.FullyQualifiedName, filter.Namespace))
          .Cast<MetricsNode>()),
      _ => Enumerable.Empty<MetricsNode>()
    };
  }
}


