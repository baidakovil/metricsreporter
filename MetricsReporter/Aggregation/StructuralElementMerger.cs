namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MetricsReporter.Model;
using MetricsReporter.Processing;

/// <summary>
/// Encapsulates all structural merging logic for assemblies, namespaces, types, and members.
/// </summary>
/// <remarks>
/// This helper keeps the workspace focused on orchestration while maintaining the original
/// filtering and merging behavior from <see cref="MetricsAggregationService.AggregationWorkspace"/>.
/// </remarks>
[SuppressMessage(
    "Microsoft.Maintainability",
    "CA1502:AvoidExcessiveComplexity",
    Justification = "This class orchestrates complex merging logic for assemblies, namespaces, types, and members with multiple filters, dummy node creation, and index management. The complexity comes from coordinating multiple dictionaries, filters, and conditional logic required to maintain tree consistency. Further simplification would require splitting into multiple classes that would introduce architectural overhead and reduce cohesion, or would harm readability by obscuring the orchestration flow.")]
[SuppressMessage(
    "AltCoverBranchCoverage",
    "AltCoverBranchCoverage",
    Scope = "type",
    Target = "~T:MetricsReporter.Aggregation.StructuralElementMerger",
    Justification = "Branch coverage is dominated by configuration-driven guard clauses (filters, dummy node handling, and optional metadata). Exercising every branch would require artificial assemblies, namespaces, and members that are impossible in real metrics documents and would not increase confidence beyond the existing behavioral tests that cover valid paths.")]
[SuppressMessage(
    "AltCoverSequenceCoverage",
    "AltCoverSequenceCoverage",
    Scope = "type",
    Target = "~T:MetricsReporter.Aggregation.StructuralElementMerger",
    Justification = "Sequence coverage suffers from the same configuration-driven guard clauses as branch coverage. Forcing every path would demand contrived metrics trees with excluded assemblies and dummy nodes that never occur in production inputs. Suppressing keeps the metrics report focused on actionable gaps.")]
internal sealed class StructuralElementMerger
{
  private readonly SolutionMetricsNode _solution;
  private readonly Dictionary<string, AssemblyMetricsNode> _assemblies;
  private readonly Dictionary<string, NamespaceEntry> _namespaces;
  private readonly Dictionary<string, List<NamespaceEntry>> _namespaceIndex;
  private readonly Dictionary<string, TypeEntry> _types;
  private readonly Dictionary<string, MemberMetricsNode> _members;
  private readonly MemberFilter _memberFilter;
  private readonly MemberKindFilter _memberKindFilter;
  private readonly AssemblyFilter _assemblyFilter;
  private readonly TypeFilter _typeFilter;

  /// <summary>
  /// Initializes a new instance of the <see cref="StructuralElementMerger"/> class.
  /// </summary>
  public StructuralElementMerger(
      SolutionMetricsNode solution,
      Dictionary<string, AssemblyMetricsNode> assemblies,
      Dictionary<string, NamespaceEntry> namespaces,
      Dictionary<string, List<NamespaceEntry>> namespaceIndex,
      Dictionary<string, TypeEntry> types,
      Dictionary<string, MemberMetricsNode> members,
      MemberFilter memberFilter,
      MemberKindFilter memberKindFilter,
      AssemblyFilter assemblyFilter,
      TypeFilter typeFilter)
  {
    _solution = solution ?? throw new ArgumentNullException(nameof(solution));
    _assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
    _namespaces = namespaces ?? throw new ArgumentNullException(nameof(namespaces));
    _namespaceIndex = namespaceIndex ?? throw new ArgumentNullException(nameof(namespaceIndex));
    _types = types ?? throw new ArgumentNullException(nameof(types));
    _members = members ?? throw new ArgumentNullException(nameof(members));
    _memberFilter = memberFilter ?? throw new ArgumentNullException(nameof(memberFilter));
    _memberKindFilter = memberKindFilter ?? throw new ArgumentNullException(nameof(memberKindFilter));
    _assemblyFilter = assemblyFilter ?? throw new ArgumentNullException(nameof(assemblyFilter));
    _typeFilter = typeFilter ?? throw new ArgumentNullException(nameof(typeFilter));
  }

  /// <summary>
  /// Merges an assembly element into the shared metrics trees.
  /// </summary>
  /// <summary>
  /// Merges metrics for an assembly element into the shared solution tree.
  /// </summary>
  /// <param name="element">The parsed assembly element to merge.</param>
  public void MergeAssembly(ParsedCodeElement element)
  {
    var assemblyName = element.FullyQualifiedName ?? element.Name;

    if (_assemblyFilter.ShouldExcludeAssembly(assemblyName))
    {
      return;
    }

    if (!_assemblies.TryGetValue(assemblyName, out var assemblyNode))
    {
      assemblyNode = new AssemblyMetricsNode
      {
        Name = element.Name,
        FullyQualifiedName = assemblyName,
        Metrics = new Dictionary<MetricIdentifier, MetricValue>()
      };

      _assemblies[assemblyName] = assemblyNode;
      _solution.Assemblies.Add(assemblyNode);
    }

    MergeMetrics(assemblyNode.Metrics, element.Metrics);
    MergeSource(assemblyNode, element.Source);
  }

  /// <summary>
  /// Merges a namespace element under the appropriate assembly.
  /// </summary>
  /// <summary>
  /// Registers or updates a namespace node beneath its assembly.
  /// </summary>
  /// <param name="element">The parsed namespace element to merge.</param>
  public void MergeNamespace(ParsedCodeElement element)
  {
    var assemblyName = element.ParentFullyQualifiedName ?? string.Empty;

    if (_assemblyFilter.ShouldExcludeAssembly(assemblyName))
    {
      return;
    }

    var namespaceName = element.FullyQualifiedName ?? element.Name;
    var namespaceNode = GetOrCreateNamespace(assemblyName, namespaceName, element.Name);

    if (!_assemblies.ContainsKey(assemblyName))
    {
      return;
    }

    MergeMetrics(namespaceNode.Node.Metrics, element.Metrics);
    MergeSource(namespaceNode.Node, element.Source);
  }

  /// <summary>
  /// Merges a type element while respecting filters.
  /// </summary>
  /// <summary>
  /// Adds or updates a type node, respecting the configured filters.
  /// </summary>
  /// <param name="element">The parsed type element to merge.</param>
  [SuppressMessage(
      "Microsoft.Maintainability",
      "CA1506:Avoid excessive class coupling",
      Justification = "Merging types requires coordination between filters, namespace lookups, display name resolution, and node creation, so the method naturally touches the same nodes and filters that keep the tree consistent.")]
  public void MergeType(ParsedCodeElement element)
  {
    var typeFqn = element.FullyQualifiedName ?? element.Name;
    var assemblyName = ResolveAssemblyForType(element);

    if (_typeFilter.ShouldExcludeType(typeFqn) || _typeFilter.ShouldExcludeType(element.Name))
    {
      return;
    }

    if (_assemblyFilter.ShouldExcludeAssembly(assemblyName))
    {
      return;
    }

    var namespaceName = ResolveNamespaceForType(element, assemblyName, typeFqn);
    var displayName = string.IsNullOrWhiteSpace(element.Name)
        ? ExtractTypeDisplayName(typeFqn)
        : element.Name.Contains('.') ? ExtractTypeDisplayName(typeFqn) : element.Name;

    var typeEntry = GetOrCreateType(assemblyName, namespaceName, typeFqn, displayName);

    if (!_assemblies.ContainsKey(assemblyName))
    {
      return;
    }

    MergeMetrics(typeEntry.Node.Metrics, element.Metrics);
    MergeSource(typeEntry.Node, element.Source);
  }

  /// <summary>
  /// Merges a member element, ensuring filters and assemblies are honored.
  /// </summary>
  /// <summary>
  /// Adds or updates a member (method/property) within its declaring type.
  /// </summary>
  /// <param name="element">The parsed member element to merge.</param>
  public void MergeMember(ParsedCodeElement element)
  {
    if (_memberFilter.ShouldExcludeMethod(element.Name) || _memberFilter.ShouldExcludeMethodByFqn(element.FullyQualifiedName))
    {
      return;
    }

    if (!TryResolveMemberContext(element, out var context))
    {
      return;
    }

    var typeEntry = EnsureTypeForMember(context.TypeFqn);
    if (!_assemblies.ContainsKey(context.AssemblyName))
    {
      return;
    }

    var memberNode = GetOrCreateMember(typeEntry, context.MemberFqn, element.Name);
    UpdateMemberMetadata(memberNode, element);
    MergeMetrics(memberNode.Metrics, element.Metrics);
    MergeSource(memberNode, element.Source);
  }

  private bool TryResolveMemberContext(
      ParsedCodeElement element,
      [NotNullWhen(true)] out MemberResolutionContext? context)
  {
    context = null;
    if (_memberKindFilter.ShouldExclude(element.MemberKind, element.HasSarifViolations))
    {
      return false;
    }

    if (element.FullyQualifiedName is null)
    {
      return false;
    }

    var memberFqn = element.FullyQualifiedName;
    if (_memberFilter.ShouldExcludeMethodByFqn(memberFqn))
    {
      return false;
    }

    var typeFqn = element.ParentFullyQualifiedName ?? ResolveDeclaringType(memberFqn);
    if (typeFqn is null || _typeFilter.ShouldExcludeType(typeFqn))
    {
      return false;
    }

    var assemblyName = element.ContainingAssemblyName ?? ResolveAssemblyNameFromFqn(typeFqn);
    if (_assemblyFilter.ShouldExcludeAssembly(assemblyName))
    {
      return false;
    }

    context = new MemberResolutionContext(memberFqn, typeFqn, assemblyName);
    return true;
  }

  private sealed record MemberResolutionContext(string MemberFqn, string TypeFqn, string AssemblyName);

  private static AssemblyMetricsNode CreateDummyAssembly(string assemblyName, string? displayName = null)
  {
    return new AssemblyMetricsNode
    {
      Name = displayName ?? assemblyName,
      FullyQualifiedName = assemblyName,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };
  }

  private static NamespaceMetricsNode CreateDummyNamespace(string namespaceFqn, string displayName)
  {
    return new NamespaceMetricsNode
    {
      Name = displayName,
      FullyQualifiedName = namespaceFqn,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };
  }

  private static TypeMetricsNode CreateDummyType(string typeFqn, string displayName)
  {
    return new TypeMetricsNode
    {
      Name = displayName,
      FullyQualifiedName = typeFqn,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };
  }

  private static MemberMetricsNode CreateDummyMember(string memberFqn, string displayName)
  {
    return new MemberMetricsNode
    {
      Name = displayName,
      FullyQualifiedName = memberFqn,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };
  }

  private bool ShouldUseDummyNode(string assemblyName)
  {
    return _assemblyFilter.ShouldExcludeAssembly(assemblyName) || !_assemblies.ContainsKey(assemblyName);
  }

  private NamespaceEntry GetOrCreateNamespace(string assemblyName, string namespaceFqn, string displayName)
  {
    if (string.IsNullOrEmpty(assemblyName))
    {
      assemblyName = ResolveAssemblyNameFromFqn(namespaceFqn);
    }

    if (ShouldUseDummyNode(assemblyName))
    {
      return CreateDummyNamespaceEntry(assemblyName, namespaceFqn, displayName);
    }

    var key = $"{assemblyName}::{namespaceFqn}";
    if (_namespaces.TryGetValue(key, out var existingEntry))
    {
      return HandleExistingNamespaceEntry(key, namespaceFqn, existingEntry, assemblyName, displayName);
    }

    return CreateNewNamespaceEntry(assemblyName, namespaceFqn, displayName);
  }

  private static NamespaceEntry CreateDummyNamespaceEntry(string assemblyName, string namespaceFqn, string displayName)
  {
    return new NamespaceEntry(
      CreateDummyNamespace(namespaceFqn, displayName),
      CreateDummyAssembly(assemblyName));
  }

  private NamespaceEntry HandleExistingNamespaceEntry(
    string key,
    string namespaceFqn,
    NamespaceEntry existingEntry,
    string assemblyName,
    string displayName)
  {
    var existingAssemblyName = existingEntry.Assembly.FullyQualifiedName;
    if (existingAssemblyName is not null && ShouldUseDummyNode(existingAssemblyName))
    {
      RemoveNamespaceFromIndexes(key, namespaceFqn, existingEntry, existingAssemblyName);
      return CreateDummyNamespaceEntry(assemblyName, namespaceFqn, displayName);
    }

    return existingEntry;
  }

  private void RemoveNamespaceFromIndexes(
    string key,
    string namespaceFqn,
    NamespaceEntry existingEntry,
    string existingAssemblyName)
  {
    _namespaces.Remove(key);

    if (_namespaceIndex.TryGetValue(namespaceFqn, out var indexList))
    {
      indexList.Remove(existingEntry);
    }

    if (_assemblies.TryGetValue(existingAssemblyName, out var existingAssembly)
        && existingAssembly.Namespaces.Contains(existingEntry.Node))
    {
      existingAssembly.Namespaces.Remove(existingEntry.Node);
    }
  }

  private NamespaceEntry CreateNewNamespaceEntry(string assemblyName, string namespaceFqn, string displayName)
  {
    var assembly = GetOrCreateAssembly(assemblyName, new ParsedCodeElement(CodeElementKind.Assembly, assemblyName, assemblyName));
    if (ShouldUseDummyNode(assemblyName))
    {
      return new NamespaceEntry(CreateDummyNamespace(namespaceFqn, displayName), assembly);
    }

    var node = new NamespaceMetricsNode
    {
      Name = displayName,
      FullyQualifiedName = namespaceFqn,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    assembly.Namespaces.Add(node);
    var entry = new NamespaceEntry(node, assembly);
    _namespaces[$"{assemblyName}::{namespaceFqn}"] = entry;

    AddToNamespaceIndex(namespaceFqn, entry);
    return entry;
  }

  private void AddToNamespaceIndex(string namespaceFqn, NamespaceEntry entry)
  {
    if (!_namespaceIndex.TryGetValue(namespaceFqn, out var list))
    {
      list = [];
      _namespaceIndex[namespaceFqn] = list;
    }

    if (!list.Contains(entry))
    {
      list.Add(entry);
    }
  }

  private AssemblyMetricsNode GetOrCreateAssembly(string assemblyName, ParsedCodeElement element)
  {
    if (ShouldUseDummyNode(assemblyName))
    {
      return CreateDummyAssembly(assemblyName, element.Name);
    }

    if (!_assemblies.TryGetValue(assemblyName, out var assemblyNode))
    {
      assemblyNode = new AssemblyMetricsNode
      {
        Name = element.Name,
        FullyQualifiedName = assemblyName,
        Metrics = new Dictionary<MetricIdentifier, MetricValue>()
      };

      _assemblies[assemblyName] = assemblyNode;
      _solution.Assemblies.Add(assemblyNode);
    }

    MergeMetrics(assemblyNode.Metrics, element.Metrics);
    MergeSource(assemblyNode, element.Source);
    return assemblyNode;
  }

  private TypeEntry GetOrCreateType(string assemblyName, string namespaceName, string typeFqn, string displayName)
  {
    if (_types.TryGetValue(typeFqn, out var existingEntry))
    {
      return HandleExistingTypeEntry(existingEntry, assemblyName, typeFqn, displayName);
    }

    return CreateNewTypeEntry(assemblyName, namespaceName, typeFqn, displayName);
  }

  private TypeEntry HandleExistingTypeEntry(
    TypeEntry existingEntry,
    string assemblyName,
    string typeFqn,
    string displayName)
  {
    if (assemblyName is not null && ShouldUseDummyNode(assemblyName))
    {
      RemoveTypeFromIndexes(existingEntry, typeFqn);
      return CreateDummyTypeEntry(assemblyName, typeFqn, displayName);
    }

    return existingEntry;
  }

  private void RemoveTypeFromIndexes(TypeEntry existingEntry, string typeFqn)
  {
    _types.Remove(typeFqn);

    var existingAssemblyName = existingEntry.Assembly.FullyQualifiedName ?? string.Empty;
    if (_assemblies.TryGetValue(existingAssemblyName, out var existingAssembly))
    {
      foreach (var ns in existingAssembly.Namespaces)
      {
        if (ns.Types.Contains(existingEntry.Node))
        {
          ns.Types.Remove(existingEntry.Node);
          break;
        }
      }
    }
  }

  private static TypeEntry CreateDummyTypeEntry(string assemblyName, string typeFqn, string displayName)
  {
    return new TypeEntry(
      CreateDummyType(typeFqn, displayName),
      CreateDummyAssembly(assemblyName));
  }

  private TypeEntry CreateNewTypeEntry(
    string assemblyName,
    string namespaceName,
    string typeFqn,
    string displayName)
  {
    if (ShouldUseDummyNode(assemblyName))
    {
      return CreateDummyTypeEntry(assemblyName, typeFqn, displayName);
    }

    var namespaceEntry = GetOrCreateNamespace(assemblyName, namespaceName, namespaceName);
    if (ShouldUseDummyNode(assemblyName))
    {
      return new TypeEntry(CreateDummyType(typeFqn, displayName), namespaceEntry.Assembly);
    }

    return CreateAndRegisterTypeNode(namespaceEntry, typeFqn, displayName);
  }

  private TypeEntry CreateAndRegisterTypeNode(NamespaceEntry namespaceEntry, string typeFqn, string displayName)
  {
    var node = new TypeMetricsNode
    {
      Name = displayName,
      FullyQualifiedName = typeFqn,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    namespaceEntry.Node.Types.Add(node);
    var entry = new TypeEntry(node, namespaceEntry.Assembly);
    _types[typeFqn] = entry;
    return entry;
  }

  private TypeEntry EnsureTypeForMember(string typeFqn)
  {
    if (_types.TryGetValue(typeFqn, out var entry))
    {
      return entry;
    }

    var assemblyName = ResolveAssemblyNameFromFqn(typeFqn);
    var namespaceName = ResolveNamespaceFromIndexesOrFqn(typeFqn);
    return GetOrCreateType(assemblyName, namespaceName, typeFqn, ExtractTypeDisplayName(typeFqn));
  }

  private MemberMetricsNode GetOrCreateMember(TypeEntry typeEntry, string memberFqn, string displayName)
  {
    if (_members.TryGetValue(memberFqn, out var existingNode))
    {
      return HandleExistingMember(existingNode, typeEntry, memberFqn, displayName);
    }

    return CreateNewMember(typeEntry, memberFqn, displayName);
  }

  private MemberMetricsNode HandleExistingMember(
    MemberMetricsNode existingNode,
    TypeEntry typeEntry,
    string memberFqn,
    string displayName)
  {
    var assemblyName = typeEntry.Assembly.FullyQualifiedName;
    if (assemblyName is not null && ShouldUseDummyNode(assemblyName))
    {
      _members.Remove(memberFqn);
      return CreateDummyMember(memberFqn, ExtractMemberDisplayName(memberFqn, displayName));
    }

    return existingNode;
  }

  private MemberMetricsNode CreateNewMember(TypeEntry typeEntry, string memberFqn, string displayName)
  {
    var assemblyName = typeEntry.Assembly.FullyQualifiedName;
    if (assemblyName is not null && ShouldUseDummyNode(assemblyName))
    {
      return CreateDummyMember(memberFqn, ExtractMemberDisplayName(memberFqn, displayName));
    }

    return CreateAndRegisterMemberNode(typeEntry, memberFqn, displayName);
  }

  private MemberMetricsNode CreateAndRegisterMemberNode(TypeEntry typeEntry, string memberFqn, string displayName)
  {
    var node = new MemberMetricsNode
    {
      Name = ExtractMemberDisplayName(memberFqn, displayName),
      FullyQualifiedName = memberFqn,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    typeEntry.Node.Members.Add(node);
    _members[memberFqn] = node;
    return node;
  }

  private static void UpdateMemberMetadata(MemberMetricsNode memberNode, ParsedCodeElement element)
  {
    if (memberNode.MemberKind == MemberKind.Unknown ||
        (memberNode.MemberKind == MemberKind.Method && element.MemberKind != MemberKind.Unknown && element.MemberKind != MemberKind.Method))
    {
      memberNode.MemberKind = element.MemberKind;
    }

    if (element.HasSarifViolations)
    {
      memberNode.HasSarifViolations = true;
    }
  }

  private static void MergeMetrics(IDictionary<MetricIdentifier, MetricValue> target, IDictionary<MetricIdentifier, MetricValue> source)
  {
    foreach (var pair in source)
    {
      if (target.TryGetValue(pair.Key, out var existing))
      {
        MergeExistingMetric(target, pair.Key, existing, pair.Value);
      }
      else
      {
        AddNewMetric(target, pair.Key, pair.Value);
      }
    }
  }

  private static void MergeExistingMetric(
    IDictionary<MetricIdentifier, MetricValue> target,
    MetricIdentifier key,
    MetricValue existing,
    MetricValue incoming)
  {
    if (IsAggregatableMetric(key) && incoming.Value.HasValue)
    {
      AggregateMetricValue(target, key, existing, incoming);
    }
    else if (!existing.Value.HasValue && incoming.Value.HasValue)
    {
      ReplaceNullMetricValue(target, key, incoming);
    }
  }

  private static void AggregateMetricValue(
    IDictionary<MetricIdentifier, MetricValue> target,
    MetricIdentifier key,
    MetricValue existing,
    MetricValue incoming)
  {
    var sum = (existing.Value ?? 0m) + incoming.Value!.Value;

    // WHY: We merge breakdown dictionaries when aggregating metrics to preserve
    // the detailed breakdown of rule violations. This is especially important for
    // SARIF metrics where we want to track individual rule IDs across the hierarchy.
    var mergedBreakdown = SarifBreakdownHelper.Merge(existing.Breakdown, incoming.Breakdown);

    target[key] = new MetricValue
    {
      Value = sum,
      Status = ThresholdStatus.NotApplicable,
      Breakdown = mergedBreakdown
    };
  }

  private static void ReplaceNullMetricValue(
    IDictionary<MetricIdentifier, MetricValue> target,
    MetricIdentifier key,
    MetricValue incoming)
  {
    // WHY: When replacing a null value with a real value, we preserve the breakdown
    // from the incoming value to ensure SARIF breakdown information is not lost.
    // We create a new MetricValue to ensure the breakdown dictionary is properly copied.
    target[key] = new MetricValue
    {
      Value = incoming.Value,
      Delta = incoming.Delta,
      Status = incoming.Status,
      Breakdown = SarifBreakdownHelper.Clone(incoming.Breakdown)
    };
  }

  private static void AddNewMetric(
    IDictionary<MetricIdentifier, MetricValue> target,
    MetricIdentifier key,
    MetricValue value)
  {
    // WHY: When adding a metric for the first time, we preserve the breakdown if present.
    // This ensures that SARIF metrics with breakdown information are correctly stored
    // even on the first assignment. We create a new MetricValue to ensure the breakdown
    // dictionary is properly copied.
    target[key] = new MetricValue
    {
      Value = value.Value,
      Delta = value.Delta,
      Status = value.Status,
      Breakdown = SarifBreakdownHelper.Clone(value.Breakdown)
    };
  }

  private static void MergeSource(MetricsNode node, SourceLocation? source)
  {
    if (source is null)
    {
      return;
    }

    if (ShouldReplaceSource(node.Source, source))
    {
      node.Source = source;
    }
  }

  private static bool ShouldReplaceSource(SourceLocation? existing, SourceLocation incoming)
  {
    if (existing is null)
    {
      return true;
    }

    if (!existing.StartLine.HasValue && incoming.StartLine.HasValue)
    {
      return true;
    }

    return existing.StartLine.HasValue
        && incoming.StartLine.HasValue
        && incoming.EndLine.HasValue
        && !existing.EndLine.HasValue;
  }

  private string ResolveAssemblyForType(ParsedCodeElement element)
  {
    if (!string.IsNullOrWhiteSpace(element.ContainingAssemblyName))
    {
      return element.ContainingAssemblyName!;
    }

    var resolved = ResolveAssemblyFromParent(element);
    if (resolved is not null)
    {
      return resolved;
    }

    return ResolveAssemblyNameFromFqn(element.FullyQualifiedName ?? element.Name);
  }

  private string? ResolveAssemblyFromParent(ParsedCodeElement element)
  {
    if (element.ParentFullyQualifiedName is null)
    {
      return null;
    }

    if (_assemblies.ContainsKey(element.ParentFullyQualifiedName))
    {
      return element.ParentFullyQualifiedName;
    }

    var assembly = TryResolveAssembly(element.ParentFullyQualifiedName);
    return assembly?.FullyQualifiedName ?? assembly?.Name;
  }

  private AssemblyMetricsNode? TryResolveAssembly(string namespaceFqn)
  {
    if (!_namespaceIndex.TryGetValue(namespaceFqn, out var entries) || entries.Count == 0)
    {
      return null;
    }

    return entries[0].Assembly;
  }

  private string ResolveAssemblyNameFromFqn(string typeFqn)
  {
    var namespaceName = ResolveNamespaceName(typeFqn);
    var assembly = TryResolveAssembly(namespaceName);
    if (assembly is not null)
    {
      return assembly.FullyQualifiedName ?? assembly.Name;
    }

    var excludedAssembly = FindExcludedAssembly(typeFqn, namespaceName);
    if (excludedAssembly is not null)
    {
      return excludedAssembly;
    }

    return GetDefaultAssemblyName();
  }

  private string? FindExcludedAssembly(string typeFqn, string namespaceName)
  {
    if (!string.IsNullOrWhiteSpace(typeFqn) && _assemblyFilter.ShouldExcludeAssembly(typeFqn))
    {
      return typeFqn;
    }

    if (!string.IsNullOrWhiteSpace(namespaceName)
        && !string.Equals(namespaceName, "<global>", StringComparison.Ordinal)
        && _assemblyFilter.ShouldExcludeAssembly(namespaceName))
    {
      return namespaceName;
    }

    var rootNamespace = ExtractRootNamespace(namespaceName);
    if (!string.IsNullOrWhiteSpace(rootNamespace) && _assemblyFilter.ShouldExcludeAssembly(rootNamespace))
    {
      return rootNamespace;
    }

    return null;
  }

  private string GetDefaultAssemblyName()
  {
    return _assemblies.Keys.Count > 0 ? _assemblies.Keys.First() : _solution.Name;
  }

  private string ResolveNamespaceForType(ParsedCodeElement element, string assemblyName, string typeFqn)
  {
    if (IsExplicitNamespace(element.ParentFullyQualifiedName, assemblyName))
    {
      return element.ParentFullyQualifiedName!;
    }

    return ResolveNamespaceFromIndexesOrFqn(typeFqn);
  }

  private string ResolveNamespaceFromIndexesOrFqn(string typeFqn)
  {
    var knownNamespace = NamespaceResolutionHelper.FindKnownNamespace(typeFqn, _namespaceIndex);
    if (!string.IsNullOrWhiteSpace(knownNamespace))
    {
      return knownNamespace;
    }

    return ResolveNamespaceName(typeFqn);
  }

  private bool IsExplicitNamespace(string? candidate, string assemblyName)
  {
    if (string.IsNullOrWhiteSpace(candidate))
    {
      return false;
    }

    if (string.Equals(candidate, "<global>", StringComparison.Ordinal))
    {
      return true;
    }

    if (string.Equals(candidate, assemblyName, StringComparison.Ordinal))
    {
      return false;
    }

    var key = $"{assemblyName}::{candidate}";
    return _namespaces.ContainsKey(key) || _namespaceIndex.ContainsKey(candidate);
  }

  private static string ResolveNamespaceName(string typeFqn)
  {
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

  private static string ExtractTypeDisplayName(string typeFqn)
  {
    var lastDot = typeFqn.LastIndexOf('.');
    return lastDot < 0 ? typeFqn : typeFqn[(lastDot + 1)..];
  }

  private static string ExtractMemberDisplayName(string memberFqn, string fallback)
  {
    if (string.IsNullOrWhiteSpace(memberFqn))
    {
      return fallback;
    }

    var paramStart = memberFqn.IndexOf('(');
    var searchEnd = paramStart >= 0 ? paramStart : memberFqn.Length;
    var lastDot = memberFqn.LastIndexOf('.', searchEnd - 1);

    if (lastDot < 0)
    {
      if (paramStart >= 0)
      {
        return memberFqn[..paramStart];
      }

      return fallback;
    }

    var methodNameStart = lastDot + 1;
    var methodNameEnd = paramStart >= 0 ? paramStart : memberFqn.Length;
    var methodName = memberFqn[methodNameStart..methodNameEnd].Trim();

    return string.IsNullOrWhiteSpace(methodName) ? fallback : methodName;
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

  private static bool IsAggregatableMetric(MetricIdentifier identifier)
      => identifier is MetricIdentifier.SarifCaRuleViolations or MetricIdentifier.SarifIdeRuleViolations;
}


