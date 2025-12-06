## Goal
Introduce an `IProcessRunner` for external commands with script-driven coverage/metrics preparation. Backward compatibility is NOT required.

## Requirements
- Create `IProcessRunner` + implementation: run process with timeout, cancellation, captured stdout/stderr, exit code, start/stop timestamps. Return a result DTO; no direct console writes.
- Inject `IProcessRunner` into metrics generation orchestrator; remove direct `ProcessStartInfo`/`Process` usage.
- Log (via ILogger) command, working dir, exit code, duration, truncated outputs on failure.
- Surface errors as result + enum exit code + clear message; typed exceptions only if tests really need them.
- Support user-provided scripts for coverage/metrics prep; no baked-in runtime/anchor MSBuild assumptions. Remove legacy runtime globs/env (`*Runtime.csproj`, `METRICS_RUNTIME_PROJECT`, `METRICS_REPORTER_RUNTIME_PROJECT`) and related resolution code in `MetricsUpdater`/similar.

## Constraints
- Align with SOLID and patterns in `dotnet-design-pattern-review.mdc`; DI-first.
- No backward compatibility; legacy env var fallbacks and runtime globs can be removed.

## Options and configuration alignment
- **Prerequisites**: Before implementation, create both `src/MetricsReporter/Configuration/metricsreporter-config.schema.json` (JSON Schema for `.metricsreporter.json` validation) and `docs/refactor/options-schema.md` (documentation covering sections `general`, `paths`, `scripts`, JSON→C# mapping, precedence rules).
- Define sections: `general` (verbosity, timeout, workingDirectory, logTruncationLimit) and `scripts` (generate list; read.any list; read.byMetric map). Defaults cover the rest.
- Introduce `.metricsreporter.json` as a first-class config source (resolved from CWD upward); populate options from CLI/env/config using this schema. Precedence: CLI > env vars > config file > defaults.
- Cancellation token flows from CLI command handlers through orchestrator into `IProcessRunner`.

## Logging integration
- Log start/finish with exit code and duration.
- Truncate stdout/stderr on failure with a simple size cap; avoid logging secrets.
- Include command name, working directory, and script identifiers in scope/state.

## Script hooks
- Support executing one or many PowerShell scripts (no cross-platform requirement) provided via CLI/config; run sequentially in the order given.
- Scripts replace the old MSBuild-target orchestration: command handler resolves scripts (generate or read.any + read.byMetric), then `IProcessRunner` executes them before any parsing/aggregation; no automatic MSBuild unless the script calls it.
- Pipeline stops on the first non-zero exit code. Contract: 0 = success, >0 = fail.
- Runner passes working directory (default CWD), inherits env, applies timeout/verbosity; capture stdout/stderr with truncation.

## Placement vs MetricsUpdater
- `IProcessRunner` (and scripts) supersede `MetricsUpdater`’s hardcoded MSBuild calls: the orchestration should call scripts first, then proceed to parsing/aggregation. `MetricsUpdater` should be refactored/removed or become a thin orchestrator delegating to scripts + parsing without its own MSBuild invocations.

## Validation and docs
- After changes, run existing tests and add new ones if needed (script selection, precedence, failure handling).
- Update or add documentation to describe the script-first orchestration and option precedence.
