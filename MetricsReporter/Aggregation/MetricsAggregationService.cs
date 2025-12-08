namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MetricsReporter.Model;
using MetricsReporter.Processing;

/// <summary>
/// Builds the consolidated report from parsed metrics sources.
/// </summary>
public sealed class MetricsAggregationService
{
  private readonly MemberFilter _memberFilter;
  private readonly AssemblyFilter _assemblyFilter;
  private readonly TypeFilter _typeFilter;
  private readonly IAggregationWorkspaceFactory _workspaceFactory;
  private readonly IReportMetadataComposer _metadataComposer;

  /// <summary>
  /// Initializes a new instance of the <see cref="MetricsAggregationService"/> class with default filters.
  /// </summary>
  public MetricsAggregationService()
      : this(new MemberFilter(), new AssemblyFilter(), new TypeFilter())
  {
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="MetricsAggregationService"/> class with the specified filters.
  /// </summary>
  /// <param name="memberFilter">The member filter to use for excluding methods. Cannot be null.</param>
  /// <param name="assemblyFilter">The assembly filter to use for excluding assemblies. Cannot be null.</param>
  /// <param name="typeFilter">The type filter to use for excluding types. Cannot be null.</param>
  /// <exception cref="ArgumentNullException">Thrown when any of the filters are null.</exception>
  public MetricsAggregationService(MemberFilter memberFilter, AssemblyFilter assemblyFilter, TypeFilter typeFilter)
      : this(memberFilter, assemblyFilter, typeFilter, null, null)
  {
  }

  private MetricsAggregationService(
      MemberFilter memberFilter,
      AssemblyFilter assemblyFilter,
      TypeFilter typeFilter,
      IAggregationWorkspaceFactory? workspaceFactory,
      IReportMetadataComposer? metadataComposer)
  {
    ArgumentNullException.ThrowIfNull(memberFilter);
    ArgumentNullException.ThrowIfNull(assemblyFilter);
    ArgumentNullException.ThrowIfNull(typeFilter);

    _memberFilter = memberFilter;
    _assemblyFilter = assemblyFilter;
    _typeFilter = typeFilter;
    _workspaceFactory = workspaceFactory ?? new AggregationWorkspaceFactory(memberFilter, assemblyFilter, typeFilter);
    _metadataComposer = metadataComposer ?? new ReportMetadataComposerAdapter(memberFilter, assemblyFilter, typeFilter);
  }

  /// <summary>
  /// Creates the final metrics report.
  /// </summary>
  /// <param name="input">Aggregation input data.</param>
  /// <returns>Composed metrics report.</returns>
  public MetricsReport BuildReport(MetricsAggregationInput input)
  {
    ArgumentNullException.ThrowIfNull(input);

    var workspace = _workspaceFactory.Create(input);
    BindSuppressedSymbols(workspace.Solution, input.SuppressedSymbols);
    var metadata = ComposeMetadata(input, workspace.Solution);

    return CreateMetricsReport(metadata, workspace.Solution);
  }

  private static void BindSuppressedSymbols(SolutionMetricsNode solution, List<SuppressedSymbolInfo>? suppressedSymbols)
  {
    if (suppressedSymbols is null || suppressedSymbols.Count == 0)
    {
      return;
    }

    SuppressedSymbolMetricBinder.Bind(solution, suppressedSymbols);
  }

  private ReportMetadata ComposeMetadata(MetricsAggregationInput input, SolutionMetricsNode solution)
  {
    var usedRuleIds = CollectUsedRuleIds(solution);
    var filteredRuleIds = usedRuleIds.Count == 0 ? null : usedRuleIds;
    return _metadataComposer.Compose(input, filteredRuleIds);
  }

  private static MetricsReport CreateMetricsReport(ReportMetadata metadata, SolutionMetricsNode solution)
  {
    return new MetricsReport
    {
      Metadata = metadata,
      Solution = solution
    };
  }

  private interface IAggregationWorkspaceFactory
  {
    AggregationWorkspace Create(MetricsAggregationInput input);
  }

  private sealed class AggregationWorkspaceFactory : IAggregationWorkspaceFactory
  {
    private readonly MemberFilter _memberFilter;
    private readonly AssemblyFilter _assemblyFilter;
    private readonly TypeFilter _typeFilter;

    public AggregationWorkspaceFactory(MemberFilter memberFilter, AssemblyFilter assemblyFilter, TypeFilter typeFilter)
    {
      _memberFilter = memberFilter ?? throw new ArgumentNullException(nameof(memberFilter));
      _assemblyFilter = assemblyFilter ?? throw new ArgumentNullException(nameof(assemblyFilter));
      _typeFilter = typeFilter ?? throw new ArgumentNullException(nameof(typeFilter));
    }

    public AggregationWorkspace Create(MetricsAggregationInput input)
    {
      var workspace = new AggregationWorkspace(input.SolutionName, _memberFilter, _assemblyFilter, _typeFilter);
      workspace.PrepareReport(input);
      return workspace;
    }
  }

  private interface IReportMetadataComposer
  {
    ReportMetadata Compose(
        MetricsAggregationInput input,
        HashSet<string>? usedRuleIds);
  }

  private sealed class ReportMetadataComposerAdapter : IReportMetadataComposer
  {
    private readonly MemberFilter _memberFilter;
    private readonly AssemblyFilter _assemblyFilter;
    private readonly TypeFilter _typeFilter;

    public ReportMetadataComposerAdapter(
        MemberFilter memberFilter,
        AssemblyFilter assemblyFilter,
        TypeFilter typeFilter)
    {
      _memberFilter = memberFilter ?? throw new ArgumentNullException(nameof(memberFilter));
      _assemblyFilter = assemblyFilter ?? throw new ArgumentNullException(nameof(assemblyFilter));
      _typeFilter = typeFilter ?? throw new ArgumentNullException(nameof(typeFilter));
    }

    public ReportMetadata Compose(
        MetricsAggregationInput input,
        HashSet<string>? usedRuleIds)
    {
      var metadataInput = ReportMetadataComposer.CreateInput(
          input,
          _memberFilter,
          _assemblyFilter,
          _typeFilter,
          usedRuleIds);
      return ReportMetadataComposer.Compose(metadataInput);
    }
  }

  /// <summary>
  /// Recursively collects all rule IDs from breakdown dictionaries in SARIF metrics across the entire metrics tree.
  /// </summary>
  /// <param name="solution">The root solution node to traverse.</param>
  /// <returns>A set of rule IDs that are actually used in breakdown.</returns>
  private static HashSet<string> CollectUsedRuleIds(SolutionMetricsNode solution)
  {
    var usedRuleIds = new HashSet<string>(StringComparer.Ordinal);
    RuleIdCollector.CollectRecursive(solution, usedRuleIds);
    return usedRuleIds;
  }

  private sealed class AggregationWorkspace
  {
    private readonly AggregationWorkspaceState _state;
    private readonly AggregationWorkspaceWorkflow _workflow;

    public AggregationWorkspace(string solutionName, MemberFilter memberFilter, AssemblyFilter assemblyFilter, TypeFilter typeFilter)
    {
      _state = new AggregationWorkspaceState(solutionName);
      _workflow = CreateWorkflow(_state, memberFilter, assemblyFilter, typeFilter);
    }

    public SolutionMetricsNode Solution => _state.Solution;

    public void MergeStructuralElements(ParsedMetricsDocument document) => _workflow.MergeStructuralElements(document);

    public void ProcessDocuments(MetricsAggregationInput input) => _workflow.ProcessDocuments(input);

    public void BuildLineIndex() => _workflow.BuildLineIndex();

    public void PrepareReport(MetricsAggregationInput input) => _workflow.PrepareReport(input);

    public void ApplySarifDocument(ParsedMetricsDocument document) => _workflow.ApplySarifDocument(document);

    public void ApplyBaselineAndThresholds(
        MetricsReport? baseline,
        IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds)
        => _workflow.ApplyBaselineAndThresholds(baseline, thresholds);

    public void ReconcileIteratorStateMachineMetrics() => _workflow.ReconcileIteratorStateMachineMetrics();

    public void ReconcilePlainNestedTypeMetrics() => _workflow.ReconcilePlainNestedTypeMetrics();

    private static AggregationWorkspaceWorkflow CreateWorkflow(
        AggregationWorkspaceState state,
        MemberFilter memberFilter,
        AssemblyFilter assemblyFilter,
        TypeFilter typeFilter)
    {
      var processors = CreateProcessors(state, memberFilter, assemblyFilter, typeFilter);
      return AssembleWorkflow(state, processors);
    }

    private static WorkflowProcessors CreateProcessors(
        AggregationWorkspaceState state,
        MemberFilter memberFilter,
        AssemblyFilter assemblyFilter,
        TypeFilter typeFilter)
    {
      return new WorkflowProcessors(
          CreateDocumentProcessor(state, memberFilter, assemblyFilter, typeFilter),
          CreateLineIndexProcessor(state, assemblyFilter),
          CreateSarifProcessor(state, assemblyFilter),
          CreateBaselineProcessor(state),
          CreateReconciliationProcessor(state),
          CreateTypeBranchCoverageApplicabilityProcessor(state));
    }

    private static AggregationDocumentProcessor CreateDocumentProcessor(
        AggregationWorkspaceState state,
        MemberFilter memberFilter,
        AssemblyFilter assemblyFilter,
        TypeFilter typeFilter)
        => new AggregationDocumentProcessor(state, memberFilter, assemblyFilter, typeFilter);

    private static AggregationLineIndexProcessor CreateLineIndexProcessor(
        AggregationWorkspaceState state,
        AssemblyFilter assemblyFilter)
        => new AggregationLineIndexProcessor(state, assemblyFilter);

    private static AggregationSarifProcessor CreateSarifProcessor(
        AggregationWorkspaceState state,
        AssemblyFilter assemblyFilter)
        => new AggregationSarifProcessor(state, assemblyFilter);

    private static AggregationBaselineAndThresholdProcessor CreateBaselineProcessor(AggregationWorkspaceState state)
        => new AggregationBaselineAndThresholdProcessor(state);

    private static AggregationReconciliationProcessor CreateReconciliationProcessor(AggregationWorkspaceState state)
        => new AggregationReconciliationProcessor(state);

    private static TypeBranchCoverageApplicabilityProcessor CreateTypeBranchCoverageApplicabilityProcessor(
        AggregationWorkspaceState state)
        => new TypeBranchCoverageApplicabilityProcessor(state);

    private static AggregationWorkspaceWorkflow AssembleWorkflow(
        AggregationWorkspaceState state,
        WorkflowProcessors processors)
    {
      return WorkflowFactory.Create(state, processors);
    }

    private static class WorkflowFactory
    {
      /// <summary>
      /// Creates an <see cref="AggregationWorkspaceWorkflow"/> from the processor collection.
      /// </summary>
      /// <param name="state">The aggregation workspace state.</param>
      /// <param name="processors">The workflow processors.</param>
      /// <returns>A new workflow instance.</returns>
      [SuppressMessage(
          "Microsoft.Maintainability",
          "CA1506:AvoidExcessiveClassCoupling",
          Justification = "This factory method must access all six processor properties from the WorkflowProcessors record to construct the AggregationWorkspaceWorkflow. The coupling is inherent to the factory pattern's responsibility of coordinating multiple dependencies. Further reduction would require either dummy wrapper methods or scattering the workflow construction logic, which would harm maintainability.")]
      public static AggregationWorkspaceWorkflow Create(AggregationWorkspaceState state, WorkflowProcessors processors)
      {
        return new AggregationWorkspaceWorkflow(
            state,
            processors.DocumentProcessor,
            processors.LineIndexProcessor,
            processors.SarifProcessor,
            processors.BaselineProcessor,
            processors.ReconciliationProcessor,
            processors.BranchCoverageProcessor);
      }
    }

    private sealed record WorkflowProcessors(
        AggregationDocumentProcessor DocumentProcessor,
        AggregationLineIndexProcessor LineIndexProcessor,
        AggregationSarifProcessor SarifProcessor,
        AggregationBaselineAndThresholdProcessor BaselineProcessor,
        AggregationReconciliationProcessor ReconciliationProcessor,
        TypeBranchCoverageApplicabilityProcessor BranchCoverageProcessor);
  }

  private sealed class AggregationWorkspaceState
  {
    public AggregationWorkspaceState(string solutionName)
    {
      Solution = new SolutionMetricsNode
      {
        Name = solutionName,
        FullyQualifiedName = solutionName,
        Metrics = new Dictionary<MetricIdentifier, MetricValue>()
      };
      Assemblies = new Dictionary<string, AssemblyMetricsNode>(StringComparer.OrdinalIgnoreCase);
      Namespaces = new Dictionary<string, NamespaceEntry>(StringComparer.Ordinal);
      NamespaceIndex = new Dictionary<string, List<NamespaceEntry>>(StringComparer.Ordinal);
      Types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal);
      Members = new Dictionary<string, MemberMetricsNode>(StringComparer.Ordinal);
      LineIndex = new LineIndex();
    }

    public SolutionMetricsNode Solution { get; }

    public Dictionary<string, AssemblyMetricsNode> Assemblies { get; }

    public Dictionary<string, NamespaceEntry> Namespaces { get; }

    public Dictionary<string, List<NamespaceEntry>> NamespaceIndex { get; }

    public Dictionary<string, TypeEntry> Types { get; }

    public Dictionary<string, MemberMetricsNode> Members { get; }

    public LineIndex LineIndex { get; }
  }

  private interface IAggregationDocumentProcessor
  {
    void MergeStructuralElements(ParsedMetricsDocument document);
  }

  private interface IAggregationLineIndexProcessor
  {
    void BuildLineIndex();
  }

  private interface IAggregationSarifProcessor
  {
    void ApplySarifDocument(ParsedMetricsDocument document);
  }

  private interface IAggregationBaselineAndThresholdProcessor
  {
    void ApplyBaselineAndThresholds(
        MetricsReport? baseline,
        IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds);
  }

  private interface IAggregationReconciliationProcessor
  {
    void ReconcileIteratorStateMachineMetrics();

    void ReconcilePlainNestedTypeMetrics();
  }

  private interface ITypeBranchCoverageApplicabilityProcessor
  {
    void ReconcileTypeBranchCoverageApplicability();
  }

  private sealed class AggregationDocumentProcessor : IAggregationDocumentProcessor
  {
    private readonly StructuralElementMerger _structuralMerger;

    public AggregationDocumentProcessor(
        AggregationWorkspaceState state,
        MemberFilter memberFilter,
        AssemblyFilter assemblyFilter,
        TypeFilter typeFilter)
    {
      ArgumentNullException.ThrowIfNull(state);
      ArgumentNullException.ThrowIfNull(memberFilter);
      ArgumentNullException.ThrowIfNull(assemblyFilter);
      ArgumentNullException.ThrowIfNull(typeFilter);

      _structuralMerger = new StructuralElementMerger(
          state.Solution,
          state.Assemblies,
          state.Namespaces,
          state.NamespaceIndex,
          state.Types,
          state.Members,
          memberFilter,
          assemblyFilter,
          typeFilter);
    }

    public void MergeStructuralElements(ParsedMetricsDocument document)
    {
      if (document.Elements.Count == 0)
      {
        return;
      }

      foreach (var element in document.Elements)
      {
        switch (element.Kind)
        {
          case CodeElementKind.Assembly:
            _structuralMerger.MergeAssembly(element);
            break;
          case CodeElementKind.Namespace:
            _structuralMerger.MergeNamespace(element);
            break;
          case CodeElementKind.Type:
            _structuralMerger.MergeType(element);
            break;
          case CodeElementKind.Member:
            _structuralMerger.MergeMember(element);
            break;
          default:
            break;
        }
      }
    }
  }

  private sealed class AggregationLineIndexProcessor : IAggregationLineIndexProcessor
  {
    private readonly AggregationWorkspaceState _state;
    private readonly AggregationWorkspaceLookup _lookup;

    public AggregationLineIndexProcessor(
        AggregationWorkspaceState state,
        AssemblyFilter assemblyFilter)
    {
      _state = state ?? throw new ArgumentNullException(nameof(state));
      ArgumentNullException.ThrowIfNull(assemblyFilter);

      _lookup = new AggregationWorkspaceLookup(
          _state.Assemblies,
          _state.NamespaceIndex,
          _state.Types,
          assemblyFilter);
    }

    public void BuildLineIndex()
    {
      TypeSourceBackfiller.PopulateMissingSources(_state.Types.Values);
      LineIndexBuilder.Build(_state.LineIndex, _state.Members.Values, _state.Types.Values, _lookup);
    }
  }

  private sealed class AggregationSarifProcessor : IAggregationSarifProcessor
  {
    private readonly AggregationWorkspaceState _state;
    private readonly SarifMetricsApplier _sarifApplier;

    public AggregationSarifProcessor(
        AggregationWorkspaceState state,
        AssemblyFilter assemblyFilter)
    {
      _state = state ?? throw new ArgumentNullException(nameof(state));
      ArgumentNullException.ThrowIfNull(assemblyFilter);

      _sarifApplier = new SarifMetricsApplier(
          _state.LineIndex,
          assemblyFilter,
          (node, identifier, value) => MergeMetric(node, identifier, value, aggregate: true));
    }

    public void ApplySarifDocument(ParsedMetricsDocument document)
        => _sarifApplier.Apply(document, _state.Solution);
  }

  private sealed class AggregationBaselineAndThresholdProcessor : IAggregationBaselineAndThresholdProcessor
  {
    private readonly AggregationWorkspaceState _state;
    private readonly BaselineEvaluator _baselineEvaluator;

    public AggregationBaselineAndThresholdProcessor(AggregationWorkspaceState state)
    {
      _state = state ?? throw new ArgumentNullException(nameof(state));
      _baselineEvaluator = new BaselineEvaluator();
    }

    public void ApplyBaselineAndThresholds(
        MetricsReport? baseline,
        IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds)
        => _baselineEvaluator.Apply(_state.Solution, baseline?.Solution, thresholds);
  }

  private sealed class AggregationReconciliationProcessor : IAggregationReconciliationProcessor
  {
    private readonly AggregationWorkspaceState _state;

    public AggregationReconciliationProcessor(AggregationWorkspaceState state)
    {
      _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public void ReconcileIteratorStateMachineMetrics()
        => IteratorCoverageReconciler.Reconcile(_state.Types, RemoveIteratorTypeFromHierarchy);

    public void ReconcilePlainNestedTypeMetrics()
        => PlainNestedTypeCoverageReconciler.Reconcile(
            _state.Types,
            _state.Members,
            RemoveIteratorTypeFromHierarchy);

    private void RemoveIteratorTypeFromHierarchy(string iteratorTypeKey, TypeEntry iteratorTypeEntry)
    {
      _state.Types.Remove(iteratorTypeKey);

      var iteratorTypeNode = iteratorTypeEntry.Node;
      var assembly = iteratorTypeEntry.Assembly;

      foreach (var ns in assembly.Namespaces)
      {
        if (ns.Types.Remove(iteratorTypeNode))
        {
          break;
        }
      }
    }

  }

  private sealed class AggregationWorkspaceWorkflow
  {
    private readonly AggregationWorkspaceState _state;
    private readonly IAggregationDocumentProcessor _documentProcessor;
    private readonly IAggregationLineIndexProcessor _lineIndexProcessor;
    private readonly IAggregationSarifProcessor _sarifProcessor;
    private readonly IAggregationBaselineAndThresholdProcessor _baselineProcessor;
    private readonly IAggregationReconciliationProcessor _reconciliationProcessor;
    private readonly ITypeBranchCoverageApplicabilityProcessor _branchCoverageProcessor;

    public AggregationWorkspaceWorkflow(
        AggregationWorkspaceState state,
        IAggregationDocumentProcessor documentProcessor,
        IAggregationLineIndexProcessor lineIndexProcessor,
        IAggregationSarifProcessor sarifProcessor,
        IAggregationBaselineAndThresholdProcessor baselineProcessor,
        IAggregationReconciliationProcessor reconciliationProcessor,
        ITypeBranchCoverageApplicabilityProcessor branchCoverageProcessor)
    {
      _state = state ?? throw new ArgumentNullException(nameof(state));
      _documentProcessor = documentProcessor ?? throw new ArgumentNullException(nameof(documentProcessor));
      _lineIndexProcessor = lineIndexProcessor ?? throw new ArgumentNullException(nameof(lineIndexProcessor));
      _sarifProcessor = sarifProcessor ?? throw new ArgumentNullException(nameof(sarifProcessor));
      _baselineProcessor = baselineProcessor ?? throw new ArgumentNullException(nameof(baselineProcessor));
      _reconciliationProcessor = reconciliationProcessor ?? throw new ArgumentNullException(nameof(reconciliationProcessor));
      _branchCoverageProcessor = branchCoverageProcessor ?? throw new ArgumentNullException(nameof(branchCoverageProcessor));
    }

    public void MergeStructuralElements(ParsedMetricsDocument document)
        => _documentProcessor.MergeStructuralElements(document);

    public void ProcessDocuments(MetricsAggregationInput input)
    {
      ArgumentNullException.ThrowIfNull(input);

      foreach (var document in input.AltCoverDocuments)
      {
        _documentProcessor.MergeStructuralElements(document);
      }

      foreach (var document in input.RoslynDocuments)
      {
        _documentProcessor.MergeStructuralElements(document);
      }

      _lineIndexProcessor.BuildLineIndex();

      foreach (var document in input.SarifDocuments)
      {
        _sarifProcessor.ApplySarifDocument(document);
      }
    }

    public void PrepareReport(MetricsAggregationInput input)
    {
      ArgumentNullException.ThrowIfNull(input);

      ProcessDocuments(input);
      _reconciliationProcessor.ReconcileIteratorStateMachineMetrics();
      _reconciliationProcessor.ReconcilePlainNestedTypeMetrics();
      _branchCoverageProcessor.ReconcileTypeBranchCoverageApplicability();
      _baselineProcessor.ApplyBaselineAndThresholds(input.Baseline, input.Thresholds);
    }

    public void BuildLineIndex()
        => _lineIndexProcessor.BuildLineIndex();

    public void ApplySarifDocument(ParsedMetricsDocument document)
        => _sarifProcessor.ApplySarifDocument(document);

    public void ApplyBaselineAndThresholds(
        MetricsReport? baseline,
        IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds)
        => _baselineProcessor.ApplyBaselineAndThresholds(baseline, thresholds);

    public void ReconcileIteratorStateMachineMetrics()
        => _reconciliationProcessor.ReconcileIteratorStateMachineMetrics();

    public void ReconcilePlainNestedTypeMetrics()
        => _reconciliationProcessor.ReconcilePlainNestedTypeMetrics();

    public void ReconcileTypeBranchCoverageApplicability()
        => _branchCoverageProcessor.ReconcileTypeBranchCoverageApplicability();
  }

  private static void MergeMetric(MetricsNode node, MetricIdentifier identifier, MetricValue value, bool aggregate)
  {
    if (!node.Metrics.TryGetValue(identifier, out var existing))
    {
      // WHY: When adding a metric for the first time, we preserve the breakdown if present.
      // This ensures that SARIF metrics with breakdown information are correctly stored
      // even on the first assignment, which is important for metrics applied via LineIndex.
      // We create a new MetricValue to ensure the breakdown dictionary is properly copied.
      node.Metrics[identifier] = new MetricValue
      {
        Value = value.Value,
        Delta = value.Delta,
        Status = value.Status,
        Breakdown = SarifBreakdownHelper.Clone(value.Breakdown)
      };
      return;
    }

    if (aggregate && value.Value.HasValue)
    {
      var sum = (existing.Value ?? 0m) + value.Value.Value;

      // WHY: We merge breakdown dictionaries when aggregating SARIF metrics to preserve
      // the detailed breakdown of rule violations. This allows the report to show which
      // specific rules are violated at each level of the hierarchy (Member, Type, etc.).
      // If both values have breakdowns, we sum the counts for each rule ID.
      var mergedBreakdown = SarifBreakdownHelper.Merge(existing.Breakdown, value.Breakdown);

      node.Metrics[identifier] = new MetricValue
      {
        Value = sum,
        Status = ThresholdStatus.NotApplicable,
        Breakdown = mergedBreakdown
      };
    }
    else if (!existing.Value.HasValue && value.Value.HasValue)
    {
      // WHY: When replacing a null value with a real value, we preserve the breakdown
      // from the incoming value to ensure SARIF breakdown information is not lost.
      // We create a new MetricValue to ensure the breakdown dictionary is properly copied.
      node.Metrics[identifier] = new MetricValue
      {
        Value = value.Value,
        Delta = value.Delta,
        Status = value.Status,
        Breakdown = SarifBreakdownHelper.Clone(value.Breakdown)
      };
    }
  }

  /// <summary>
  /// Normalizes AltCover branch coverage at the type level so compiler-generated helper types
  /// do not introduce misleading 0% coverage when their members lack branch metrics.
  /// </summary>
  private sealed class TypeBranchCoverageApplicabilityProcessor : ITypeBranchCoverageApplicabilityProcessor
  {
    private readonly AggregationWorkspaceState _state;

    public TypeBranchCoverageApplicabilityProcessor(AggregationWorkspaceState state)
    {
      _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public void ReconcileTypeBranchCoverageApplicability()
    {
      if (_state.Types.Count == 0)
      {
        return;
      }

      foreach (var typeEntry in _state.Types.Values)
      {
        EnsureTypeCoverageIsApplicable(typeEntry);
      }
    }

    private static void EnsureTypeCoverageIsApplicable(TypeEntry typeEntry)
    {
      var typeMetrics = typeEntry.Node.Metrics;
      if (!typeMetrics.TryGetValue(MetricIdentifier.AltCoverBranchCoverage, out var typeBranchMetric))
      {
        return;
      }

      if (HasMemberBranchCoverage(typeEntry.Node.Members))
      {
        return;
      }

      // WHY: AltCover reports 0% coverage for helper types that never expose branch metrics.
      // Removing the metric entirely keeps the report actionable and avoids false alarms.
      if (IsSyntheticZeroCoverage(typeBranchMetric))
      {
        typeMetrics.Remove(MetricIdentifier.AltCoverBranchCoverage);
      }
    }

    private static bool HasMemberBranchCoverage(IList<MemberMetricsNode> members)
    {
      foreach (var member in members)
      {
        if (member.Metrics.TryGetValue(MetricIdentifier.AltCoverBranchCoverage, out var branchMetric) &&
            branchMetric.Value.HasValue)
        {
          return true;
        }
      }

      return false;
    }

    private static bool IsSyntheticZeroCoverage(MetricValue metric)
      => !metric.Value.HasValue || metric.Value.Value == 0;
  }


  private static class ReportMetadataComposer
  {
    public static ReportMetadata Compose(ReportMetadataInput input)
    {
      ArgumentNullException.ThrowIfNull(input);
      return BuildReportMetadata(input);
    }

    [SuppressMessage(
        "Microsoft.Maintainability",
        "CA1506:Avoid excessive class coupling",
        Justification = "This composer method only maps the pre-built metadata DTO into the final <see cref=\"ReportMetadata\"/> structure so it must mention all DTO slots to keep the report complete. Splitting dependencies further would either reintroduce the same types or scatter metadata among meaningless helpers, so suppression preserves clarity.")]
    private static ReportMetadata BuildReportMetadata(ReportMetadataInput input)
    {
      var content = input.Content;
      return new ReportMetadata
      {
        GeneratedAtUtc = DateTime.UtcNow,
        BaselineReference = input.BaselineReference,
        Paths = input.Paths,
        ThresholdsByLevel = content.ThresholdMetadata.ThresholdsByLevel,
        ThresholdDescriptions = content.ThresholdMetadata.Descriptions,
        MetricDescriptors = content.MetricDescriptors,
        ExcludedMemberNamesPatterns = content.MemberNamesPatterns,
        ExcludedAssemblyNames = content.AssemblyNamesPatterns,
        ExcludedTypeNamePatterns = content.TypeNamesPatterns,
        SuppressedSymbols = input.SuppressedSymbols,
      RuleDescriptions = content.RuleDescriptions,
      MetricAliases = new Dictionary<MetricIdentifier, IReadOnlyList<string>>(input.MetricAliases)
      };
    }

    public static ReportMetadataInput CreateInput(
        MetricsAggregationInput input,
        MemberFilter memberFilter,
        AssemblyFilter assemblyFilter,
        TypeFilter typeFilter,
        HashSet<string>? usedRuleIds = null)
    {
      var specification = ReportMetadataSpecificationBuilder.Build(
          input,
          memberFilter,
          assemblyFilter,
          typeFilter,
          usedRuleIds);

      return specification.CreateInput(input);
    }

    private static ReportThresholdMetadata GatherThresholdMetadata(GatheringContext context)
        => CreateThresholdMetadata(context.Input.Thresholds);

    private static Dictionary<string, RuleDescription> GatherRuleDescriptions(GatheringContext context)
        => ProcessRuleDescriptions(context.Input.SarifDocuments, context.UsedRuleIds);

    private static FilterPatternExtractor.FilterPatterns GatherFilterPatterns(GatheringContext context)
        => ExtractFilterPatterns(context.MemberFilter, context.AssemblyFilter, context.TypeFilter);

    private sealed record GatheringContext(
        MetricsAggregationInput Input,
        MemberFilter MemberFilter,
        AssemblyFilter AssemblyFilter,
        TypeFilter TypeFilter,
        HashSet<string>? UsedRuleIds);

    private static Dictionary<string, RuleDescription> ProcessRuleDescriptions(
        IList<ParsedMetricsDocument> sarifDocuments,
        HashSet<string>? usedRuleIds)
        => RuleDescriptionProcessor.Process(sarifDocuments, usedRuleIds);

    private static IDictionary<MetricIdentifier, MetricDescriptor> GetMetricDescriptors()
        => MetricDescriptorCatalog.CreateDescriptors();

    private static FilterPatternExtractor.FilterPatterns ExtractFilterPatterns(
        MemberFilter memberFilter,
        AssemblyFilter assemblyFilter,
        TypeFilter typeFilter)
        => FilterPatternExtractor.Extract(memberFilter, assemblyFilter, typeFilter);

    private sealed class ReportMetadataSpecificationBuilder
    {
      [SuppressMessage(
          "Microsoft.Maintainability",
          "CA1506:Avoid excessive class coupling",
          Justification = "Building the metadata specification for reporting necessarily touches multiple domain objects (filters, descriptors, thresholds, SARIF rule descriptions) because it aggregates them into a single DTO. Splitting it further would only push the same dependencies elsewhere.")]
      public static ReportMetadataSpecification Build(
          MetricsAggregationInput input,
          MemberFilter memberFilter,
          AssemblyFilter assemblyFilter,
          TypeFilter typeFilter,
          HashSet<string>? usedRuleIds)
      {
        var context = CreateGatheringContext(input, memberFilter, assemblyFilter, typeFilter, usedRuleIds);
        var content = new ReportMetadataContent(
            GatherThresholdMetadata(context),
            GetMetricDescriptors(),
            GatherFilterPatterns(context),
            GatherRuleDescriptions(context));

        return new ReportMetadataSpecification(input.BaselineReference, input.Paths, content);
      }

      private static GatheringContext CreateGatheringContext(
          MetricsAggregationInput input,
          MemberFilter memberFilter,
          AssemblyFilter assemblyFilter,
          TypeFilter typeFilter,
          HashSet<string>? usedRuleIds)
      {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(memberFilter);
        ArgumentNullException.ThrowIfNull(assemblyFilter);
        ArgumentNullException.ThrowIfNull(typeFilter);

        return new GatheringContext(input, memberFilter, assemblyFilter, typeFilter, usedRuleIds);
      }
    }


    private static ReportThresholdMetadata CreateThresholdMetadata(
        IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds)
    {
      ArgumentNullException.ThrowIfNull(thresholds);

      var (thresholdLevels, thresholdDescriptions) = ThresholdMetadataBuilder.Build(thresholds);
      return new ReportThresholdMetadata(thresholdLevels, thresholdDescriptions);
    }
  }

  private sealed class ReportMetadataSpecification
  {
    public ReportMetadataSpecification(string? baselineReference, ReportPaths paths, ReportMetadataContent content)
    {
      BaselineReference = baselineReference;
      Paths = paths ?? throw new ArgumentNullException(nameof(paths));
      Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public string? BaselineReference { get; }

    public ReportPaths Paths { get; }

    public ReportMetadataContent Content { get; }

    public ReportMetadataInput CreateInput(MetricsAggregationInput input)
    {
      ArgumentNullException.ThrowIfNull(input);
      return CreateInput(input.SuppressedSymbols, input.MetricAliases);
    }

    public ReportMetadataInput CreateInput(
      IList<SuppressedSymbolInfo> suppressedSymbols,
      IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> metricAliases)
    {
      ArgumentNullException.ThrowIfNull(suppressedSymbols);
      ArgumentNullException.ThrowIfNull(metricAliases);
      return new ReportMetadataInput(BaselineReference, Paths, Content, suppressedSymbols, metricAliases);
    }
  }

  private sealed class ReportMetadataContent
  {
    public ReportMetadataContent(
        ReportThresholdMetadata thresholdMetadata,
        IDictionary<MetricIdentifier, MetricDescriptor> metricDescriptors,
        FilterPatternExtractor.FilterPatterns filterPatterns,
        Dictionary<string, RuleDescription> ruleDescriptions)
    {
      ThresholdMetadata = thresholdMetadata ?? throw new ArgumentNullException(nameof(thresholdMetadata));
      MetricDescriptors = metricDescriptors ?? throw new ArgumentNullException(nameof(metricDescriptors));
      FilterPatterns = filterPatterns ?? throw new ArgumentNullException(nameof(filterPatterns));
      RuleDescriptions = ruleDescriptions ?? throw new ArgumentNullException(nameof(ruleDescriptions));
    }

    public ReportThresholdMetadata ThresholdMetadata { get; }

    public IDictionary<MetricIdentifier, MetricDescriptor> MetricDescriptors { get; }

    public FilterPatternExtractor.FilterPatterns FilterPatterns { get; }

    public Dictionary<string, RuleDescription> RuleDescriptions { get; }

    public string? MemberNamesPatterns => FilterPatterns.MemberNamesPatterns;

    public string? AssemblyNamesPatterns => FilterPatterns.AssemblyNamesPatterns;

    public string? TypeNamesPatterns => FilterPatterns.TypeNamesPatterns;
  }

  private sealed record ReportMetadataInput(
      string? BaselineReference,
      ReportPaths Paths,
      ReportMetadataContent Content,
      IList<SuppressedSymbolInfo> SuppressedSymbols,
      IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> MetricAliases);

  private sealed class ReportThresholdMetadata
  {
    private readonly Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>> _thresholdsByLevel;
    private readonly Dictionary<MetricIdentifier, string?> _descriptions;

    public ReportThresholdMetadata(
        Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>> thresholdsByLevel,
        Dictionary<MetricIdentifier, string?> descriptions)
    {
      _thresholdsByLevel = thresholdsByLevel ?? throw new ArgumentNullException(nameof(thresholdsByLevel));
      _descriptions = descriptions ?? throw new ArgumentNullException(nameof(descriptions));
    }

    public Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>> ThresholdsByLevel
        => _thresholdsByLevel;

    public Dictionary<MetricIdentifier, string?> Descriptions
        => _descriptions;
  }
}

