## Summary of Changes (v0.2.6)

### Configuration
- `.metricsreporter.json`
  - Added member-kind filter flags: `excludeMethods`, `excludeProperties`, `excludeFields`, `excludeEvents`; defaulted fields to excluded, others to included.
  - Restored accessor name filters in `excludedMembers` (`get_*`, `set_*`, `add_*`, `remove_*`) per user adjustment.

### Models & Schema
- `MetricsReporter/Model/metrics-report.schema.json`
  - Kept `memberKind` enum aligned to code (Unknown, Method, Property, Field, Event); removed Accessor entry after reverting schema change.

### Processing / Filters
- `MetricsReporter/Processing/MemberFilter.cs`
  - Removed hardcoded default exclusions; filter now uses only provided patterns.
  - Added `HasPatterns` helper; tracks configured patterns.
  - Keeps exact-name cache for pattern matching.
- `MetricsReporter/Processing/Parsers/RoslynMetricsDocumentWalker.cs`
  - Skips property/event accessors (`get/set/add/remove`) at parse time.
  - Uses `RoslynMemberAccessorDetector` to detect accessor methods.

### Aggregation
- `MetricsReporter/Aggregation/StructuralElementMerger.cs`
  - Applies `MemberFilter` during member merge; respects name/FQN exclusions before adding to the tree.
- `MetricsReporter/Aggregation/MetricsAggregationService.cs`
  - Constructors wired to explicit filters; removed redundant post-filter pass to avoid extra traversal.

### Services / Options / CLI
- `MetricsReporter/Services/MetricsReporterOptions.cs` (unchanged in code, but options flow extended earlier to carry member-kind filters).
- `MetricsReporter/Cli/Infrastructure/AggregationOptionsResolver.cs`, `GenerateCommandContextBuilder.cs`, `Cli/Commands/GenerateCommand.cs` (integration already present; no further edits in this iteration).

### Rendering
- `MetricsReporter/Rendering/SymbolTooltipBuilder.cs`, `MetricsReporter/Rendering/NodeRenderer.cs`
  - Tooltips include `memberKind` data for symbols.

### Parsing & Line Index
- `MetricsReporter/Processing/Parsers/OpenCoverMethodParser.cs`
  - Sets `MemberKind` for OpenCover methods.
- `MetricsReporter/Processing/Parsers/RoslynMetricsDocumentWalker.cs`
  - Assigns precise `MemberKind` from Roslyn XML.
- `MetricsReporter/Processing/Parsers/SarifRuleViolationFactory.cs`
  - Marks nodes carrying SARIF violations (`HasSarifViolations`).
- `MetricsReporter/Aggregation/LineIndexBuilder.cs`
  - Applies member-kind filter and SARIF override when indexing members.

### Model Extensions
- `MetricsReporter/Processing/ParsedCodeElement.cs`
  - Added `MemberKind` and `HasSarifViolations`.
- `MetricsReporter/Model/MemberMetricsNode.cs`
  - Added `MemberKind` and `HasSarifViolations`.

### Filters
- `MetricsReporter/Processing/MemberKindFilter.cs`
  - Encapsulates kind-based exclusions (methods/properties/fields/events/accessors).

### Tests
- `MetricsReporter.Tests/Processing/Parsers/RoslynMetricsDocumentWalkerTests.cs`
  - Added coverage for accessor skipping and member-kind detection.
- `MetricsReporter.Tests/Aggregation/StructuralElementMergerMemberKindTests.cs`
  - Covers member-kind filtering, SARIF override, Roslyn vs OpenCover priority.
- `MetricsReporter.Tests/Aggregation/MetricsAggregationServiceTests.cs`
  - Added merged-source test (Roslyn + OpenCover same member) and nested/global namespace cases; updated to pass explicit member filters.
- `MetricsReporter.Tests/Processing/MemberFilterTests.cs`
  - Adjusted expectations after removing default exclusions.

### Packaging / Versioning
- `build/common.props`
  - Version bumped to `0.2.6`.
- Packed and published locally:
  - `MetricsReporter.0.2.6.nupkg`
  - `MetricsReporter.Tool.0.2.6.nupkg`
- Tool manifest updated to `metricsreporter.tool` 0.2.6.

### Docs
- `docs/2-how-to-guides/2.2 - ship-metricsreporter-update.md`
  - Used as reference for packing/updating local tool; no content changes.

### Usage Notes
- Member-name exclusions now rely solely on config/CLI/env; no baked-in defaults.
- To exclude fields: set `excludeFields: true` (done).
- Accessors are parsed out at source; `excludedMembers` still includes accessor patterns for redundancy. 

