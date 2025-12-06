# MetricsUpdater: (Deprecated) Design, Dependencies, and Future Improvements

> **Status:** Removed in the Spectre-based CLI refactor. Script hooks executed via `IProcessRunner` now replace MetricsUpdater’s MSBuild orchestration. This document is kept for historical context.

## What MetricsUpdater does
`MetricsUpdater` (in `src/MetricsReporter/MetricsReader/Services/MetricsUpdater.cs`) orchestrates two MSBuild invocations:
1. **Collect coverage** via the `CollectCoverage` target (runs if `AltCoverEnabled=true` in `code-metrics.props`). Uses a runtime project because it contains instrumentation targets (override via `METRICS_RUNTIME_PROJECT` if needed).
2. **Generate metrics dashboard** via `GenerateMetricsDashboard` (and baseline handling), using an *anchor* test project.

### Anchor project resolution
- Looks for an anchor project to trigger `GenerateMetricsDashboard`. Resolution order:
  1. Env vars: `METRICS_TARGETS_ANCHOR_PROJECT` or `METRICS_REPORTER_ANCHOR_PROJECT` (value may be `ProjectName` or `ProjectName.csproj`).
  2. Fallback patterns: `MetricsReporter.Tests.csproj`.
- If none found, throws: `Metrics anchor project file could not be located`.

### Process start info
- Coverage command: `dotnet msbuild "<runtime project>" /t:CollectCoverage /p:AltCoverEnabled=true`
- Metrics command: `dotnet msbuild "<anchor project>" /t:Build /p:GenerateMetricsDashboard=true /p:BuildProjectReferences=false /p:SkipMetricsReporterBuild=true /p:RoslynMetricsEnabled=true`
- Output is streamed to stdout/stderr; non‑zero exit codes raise `InvalidOperationException`.

## Strengths
- **Self-contained orchestration**: No external scripts; uses `dotnet msbuild` with explicit targets.
- **Configurable anchor**: Environment variables allow consumers to adapt without code changes.
- **Sequential coverage → metrics**: Ensures coverage artifacts are produced before aggregation.

## Weaknesses / risks
- **MSBuild coupling**: Hard-wired to MSBuild targets and specific project names; brittle across repositories.
- **Env-var contract**: Anchor override via env vars is implicit and undocumented in public API.
- **Mixed responsibilities**: Orchestrator triggers coverage, metrics, and handles console I/O—blurs separation of concerns.
- **Error reporting**: Throws generic `InvalidOperationException` with limited context; no structured logging/diagnostics.
- **Tooling assumptions**: Assumes AltCover targets exist in runtime project; assumes consumers use the same target naming conventions.

## If positioning as a community/clean/SOLID tool
Recommended refactor:
- **Extract an orchestration abstraction**: Define `IMetricsUpdateOrchestrator` with clear inputs (solution path, anchor project, coverage settings, additional arguments) and a structured result object (status, captured stdout/stderr, exit codes).
- **Pluggable runners**: Wrap process execution behind `IProcessRunner` (with injection for tests). Allow custom MSBuild paths/arguments; surface diagnostics (start/end timestamps, command lines, exit codes).
- **Explicit configuration object**: Replace env-var lookup with a typed options class (e.g., `MetricsUpdateOptions`) that can be populated from CLI, env vars, or JSON config; validate early with rich errors.
- **Decouple repository-specific defaults**: Move repo-specific defaults into a consumer-side profile or a minimal adapter layer; keep the library repo-agnostic.
- **Logging**: Introduce structured logging (Microsoft.Extensions.Logging) with verbosity switches; log command lines, resolved paths, and duration.
- **Failure typing**: Use specific exception types or a `Result` discriminated union to report missing anchor, missing runtime project, process non-zero exit, or IO issues.
- **Parallel readiness**: Consider allowing coverage generation to be optional or externally provided; the orchestrator should accept precomputed coverage paths instead of forcing AltCover runs.
- **Public docs/tests**: Document configuration (anchor selection, coverage toggles) and add integration tests that stub `IProcessRunner` to assert arguments and failure handling.

## Minimal changes for current consumers
- Keep env-var override for anchor but also allow an explicit CLI parameter to set anchor project.
- Log resolved anchor/runtime projects and the full commands before execution.
- Improve error messages to include the search patterns used and the working directory.


