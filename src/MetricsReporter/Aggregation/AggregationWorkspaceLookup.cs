namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.Processing;
using MetricsReporter.Model;

/// <summary>
/// Provides assembly, namespace, and type lookups that were formerly embedded inside the aggregation workspace.
/// </summary>
internal sealed class AggregationWorkspaceLookup
{
  private readonly Dictionary<string, AssemblyMetricsNode> _assemblies;
  private readonly Dictionary<string, List<NamespaceEntry>> _namespaceIndex;
  private readonly Dictionary<string, TypeEntry> _types;
  private readonly AssemblyFilter _assemblyFilter;

  public AggregationWorkspaceLookup(
      Dictionary<string, AssemblyMetricsNode> assemblies,
      Dictionary<string, List<NamespaceEntry>> namespaceIndex,
      Dictionary<string, TypeEntry> types,
      AssemblyFilter assemblyFilter)
  {
    _assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
    _namespaceIndex = namespaceIndex ?? throw new ArgumentNullException(nameof(namespaceIndex));
    _types = types ?? throw new ArgumentNullException(nameof(types));
    _assemblyFilter = assemblyFilter ?? throw new ArgumentNullException(nameof(assemblyFilter));
  }

  public AssemblyMetricsNode? ResolveMemberAssemblyNode(MemberMetricsNode member)
  {
    if (member.FullyQualifiedName is null)
    {
      return null;
    }

    var memberTypeFqn = ResolveDeclaringType(member.FullyQualifiedName);
    if (memberTypeFqn is not null && _types.TryGetValue(memberTypeFqn, out var entry))
    {
      return entry.Assembly;
    }

    return null;
  }

  public bool ShouldExcludeMember(MemberMetricsNode member)
  {
    if (member.FullyQualifiedName is null)
    {
      return false;
    }

    var assemblyName = ResolveMemberAssemblyName(member);
    return assemblyName is not null && ShouldExcludeAssembly(assemblyName);
  }

  public bool ShouldExcludeType(TypeEntry typeEntry)
  {
    var assemblyName = typeEntry.Assembly.FullyQualifiedName;
    return ShouldExcludeAssembly(assemblyName);
  }

  public bool ShouldExcludeAssembly(string? assemblyName)
      => string.IsNullOrWhiteSpace(assemblyName)
          ? false
          : _assemblyFilter.ShouldExcludeAssembly(assemblyName) || !_assemblies.ContainsKey(assemblyName);

  private string? ResolveMemberAssemblyName(MemberMetricsNode member)
  {
    var memberTypeFqn = ResolveDeclaringType(member.FullyQualifiedName!);
    if (memberTypeFqn is not null && _types.TryGetValue(memberTypeFqn, out var memberTypeEntry))
    {
      return memberTypeEntry.Assembly.FullyQualifiedName;
    }

    return ResolveAssemblyNameFromFqn(member.FullyQualifiedName!);
  }

  private string ResolveAssemblyNameFromFqn(string typeFqn)
  {
    var namespaceName = ResolveNamespaceFromIndexesOrFqn(typeFqn);
    var assembly = TryResolveAssembly(namespaceName);
    if (assembly is not null)
    {
      return assembly.FullyQualifiedName ?? assembly.Name;
    }

    if (!string.IsNullOrWhiteSpace(typeFqn) && _assemblyFilter.ShouldExcludeAssembly(typeFqn))
    {
      return typeFqn;
    }

    if (!string.IsNullOrWhiteSpace(namespaceName) && !string.Equals(namespaceName, "<global>", StringComparison.Ordinal)
        && _assemblyFilter.ShouldExcludeAssembly(namespaceName))
    {
      return namespaceName;
    }

    var rootNamespace = ExtractRootNamespace(namespaceName);
    if (!string.IsNullOrWhiteSpace(rootNamespace) && _assemblyFilter.ShouldExcludeAssembly(rootNamespace))
    {
      return rootNamespace;
    }

    return _assemblies.Keys.FirstOrDefault() ?? string.Empty;
  }

  private AssemblyMetricsNode? TryResolveAssembly(string namespaceFqn)
  {
    if (!_namespaceIndex.TryGetValue(namespaceFqn, out var entries) || entries.Count == 0)
    {
      return null;
    }

    return entries[0].Assembly;
  }

  private string ResolveNamespaceFromIndexesOrFqn(string typeFqn)
  {
    var knownNamespace = NamespaceResolutionHelper.FindKnownNamespace(typeFqn, _namespaceIndex);
    if (!string.IsNullOrWhiteSpace(knownNamespace))
    {
      return knownNamespace;
    }

    return NamespaceResolutionHelper.ExtractNamespaceFromTypeFqn(typeFqn);
  }

  private static string ExtractRootNamespace(string namespaceName)
  {
    if (string.IsNullOrWhiteSpace(namespaceName) || string.Equals(namespaceName, "<global>", StringComparison.Ordinal))
    {
      return string.Empty;
    }

    var separatorIndex = namespaceName.IndexOf('.');
    return separatorIndex < 0 ? namespaceName : namespaceName[..separatorIndex];
  }

  private static string ResolveDeclaringType(string memberFqn)
  {
    if (string.IsNullOrWhiteSpace(memberFqn))
    {
      return memberFqn;
    }

    var paramStart = memberFqn.IndexOf('(');
    var searchEnd = paramStart >= 0 ? paramStart : memberFqn.Length;
    var lastDot = memberFqn.LastIndexOf('.', searchEnd - 1);
    return lastDot < 0 ? memberFqn : memberFqn[..lastDot];
  }
}


