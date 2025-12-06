## Nested Type Handling

This guide describes how nested types are normalised, merged, and rendered across the Metrics Reporter toolchain. The goal is to ensure that symbols such as `MetricsReporter.Aggregation.StructuralElementMerger.MemberResolutionContext` remain anchored to their real namespaces even when different data sources describe them with different conventions.

### Namespace inference pipeline

1. **Parsers**  
   - Roslyn: `RoslynMetricsDocumentWalker` emits namespace elements with `ParentFullyQualifiedName` set to the assembly and assigns the namespace FQN to each descendant type/member.  
   - AltCover: `AltCoverMetricsParser` reports classes directly under the assembly. Nested classes use `Outer/Nested`; the parser converts `/` to `+` and records the assembly as the parent.

2. **Structural merge** (`StructuralElementMerger`)  
   - When Roslyn provides `ParentFullyQualifiedName`, `MergeType` trusts it and skips inference.  
   - When the parent is the assembly (AltCover), `ResolveNamespaceFromIndexesOrFqn` checks the namespace index for the longest matching namespace; if none is found, `NamespaceResolutionHelper.ExtractNamespaceFromTypeFqn` slices the FQN at the last dot and uses `<global>` when no dot exists.  
   - Types whose names contain dots remain type nodes; namespaces are only created from actual namespace entries or the fallback slicing logic.

3. **Coverage reconciliation** (`PlainNestedTypeCoverageReconciler`)  
   - Converts AltCover `Outer+Inner` names to dot FQNs and copies metrics onto the dot-type nodes created earlier. Because the namespace inference already placed those dot types under the real namespace, coverage transfer does not introduce phantom namespaces.

4. **Member handling**  
   - Members inherit the declaring type FQN. If the type is synthesised during `EnsureTypeForMember`, the same namespace resolution logic is used so member FQNs stay aligned with the namespace tree.

### Suppressions and CLI tooling

- `FullyQualifiedNameBuilder` maintains independent stacks for namespaces and types; nested types are recorded as `Namespace.Outer.Inner`. This keeps `[SuppressMessage]` bindings aligned with the aggregated report.
- `NamespaceMatcher` (used by `metrics-reader`) treats `.` `+` and `:` as valid separators after the namespace prefix, so queries such as `metrics-reader readany --namespace MetricsReporter --symbol-kind Type` return nested types regardless of their original notation.

### Rendering

The HTML generator displays whatever `FullyQualifiedName` is stored in the type node. Because the hierarchy already contains the correct namespace/type relationship, nested types simply appear as additional type rows; no fake namespace rows are emitted.

### Edge cases and guarantees

| Scenario | Behaviour | Tests |
| --- | --- | --- |
| Namespace declared in Roslyn data | Namespace is taken directly from `ParentFullyQualifiedName`. | `BuildReport_RoslynNestedType_UsesDeclaredNamespace` |
| Namespace contains dots that look like nested types (e.g., `MyCompany.Services.Core`) | Longest prefix lookup finds the real namespace before any slicing occurs. | `BuildReport_NamespaceIndexBeatsNestedTypeHeuristics_WhenNamespaceContainsDots` |
| Namespace entry is missing (filtered out or AltCover-only data) | String slicing fallback produces `Namespace.Type`; `<global>` is used when no dot exists. | `BuildReport_MissingNamespaceFallsBackToStringSlicing` |
| AltCover plus notation / iterator reconciliation | `PlainNestedTypeCoverageReconciler` copies coverage to the dot-type and removes the `+` type when appropriate. | `BuildReport_PlainNestedPlusTypeCoverage_IsTransferredToDotTypeAndTypeIsHidden` |
| Nested members created before their types | `EnsureTypeForMember` resolves the namespace using the same helper, so members never create pseudo-namespaces. | Covered indirectly via aggregation and reconciliation tests |

These tests live in `tests/MetricsReporter.Tests/Aggregation/MetricsAggregationServiceTests.cs`. They ensure that both the namespace-index path and the fallback path behave as documented. When adding new data sources or renderers, favour this behaviour:

- Register namespaces explicitly whenever possible to avoid relying on heuristics.
- When synthesising types or members, call `NamespaceResolutionHelper` so future modules inherit the same decision tree.
- Keep CLI and UI consumers agnostic of the separator (`.` vs `+`) by always storing dot-style FQNs in the final DTOs.
