## Overview
Rebuild CLI as single Spectre.Console.Cli tool with unified config, script-driven orchestration via IProcessRunner, and clean command structure. No backward compatibility required.

## Goal
Introduce a unified `metricsreporter` CLI with Spectre.Console.Cli, `IProcessRunner` abstraction for external commands, and script-driven coverage/metrics preparation. Replace dual hosts and hardcoded MSBuild orchestration with flexible, testable architecture.

## Requirements

### CLI structure
- One entry point: `metricsreporter` dotnet tool; remove dual hosts (MetricsReporterConsoleHost, MetricsReaderConsoleHost).
- Commands:
  - `generate` (current metrics aggregation)
  - `read` (rename old `readany`; returns metric violations)
  - `readsarif` (SARIF-focused)
  - `test` (single symbol check)
- Shared options via base settings (report path, thresholds, include-suppressed, group-by, symbol-kind).
- Use Spectre validation/Help; consistent exit codes.
- DI-driven command handlers; no static parsing.
- Update docs/help text to reflect new names and single entry point.

### IProcessRunner abstraction
- Create `IProcessRunner` + implementation: run process with timeout, cancellation, captured stdout/stderr, exit code, start/stop timestamps. Return a result DTO; no direct console writes.
- Inject `IProcessRunner` into metrics generation orchestrator; remove direct `ProcessStartInfo`/`Process` usage.
- Log (via ILogger) command, working dir, exit code, duration, truncated outputs on failure.
- Surface errors as result + enum exit code + clear message; typed exceptions only if tests really need them.
- Support user-provided scripts for coverage/metrics prep; no baked-in runtime/anchor MSBuild assumptions. Remove legacy runtime globs/env (`*Runtime.csproj`, `METRICS_RUNTIME_PROJECT`, `METRICS_REPORTER_RUNTIME_PROJECT`) and related resolution code in `MetricsUpdater`/similar.

## Constraints
- Align with SOLID and patterns in `dotnet-design-pattern-review.mdc`; DI-first.
- No backward compatibility; legacy env var fallbacks, runtime globs, and `metrics-reader` prefix can be removed.
- Keep commands linear and AI-friendly.

## Configuration and options alignment
- Create the complete configuration schema:
  1. **`src/MetricsReporter/Configuration/metricsreporter-config.schema.json`** — JSON Schema for `.metricsreporter.json` validation/IDE support, covering all sections: `general` (verbosity, timeout, workingDirectory, logTruncationLimit), `paths` (report, thresholds, altcover, roslyn, sarif, baseline, outputHtml), `scripts` (generate array, read.any array, read.byMetric object).
  2. **`docs/refactor/options-schema.md`** — Documentation with JSON→C# mapping, precedence rules (CLI > env > config file > defaults), examples for CLI args and env vars.
- After schema creation, implement the requirements using these sections.
- Introduce a JSON config file `.metricsreporter.json` as a first-class source. Resolve from CWD upward (no global file). Sources priority: CLI > env > config file > defaults.
- Provide global options: verbosity, config path override, timeout defaults. No fancy log format switches.
- Validation errors should be reported with Spectre validation messages before command execution.
- Cancellation token flows from CLI command handlers through orchestrator into `IProcessRunner`.

## Script hooks
- Support executing one or many PowerShell scripts (no cross-platform requirement) provided via CLI/config; run sequentially in the order given.
- Scripts replace the old MSBuild-target orchestration: command handler resolves scripts (generate or read.any + read.byMetric), then `IProcessRunner` executes them before any parsing/aggregation; no automatic MSBuild unless the script calls it.
- Pipeline stops on the first non-zero exit code. Contract: 0 = success, >0 = fail.
- Runner passes working directory (default CWD), inherits env, applies timeout/verbosity; capture stdout/stderr with truncation.
- Scripts in config/CLI: allow multiple entries; executed in listed order. `generate` uses its scripts before aggregation; `read` can define common scripts and per-metric scripts (e.g., `any` list always, `coverage` appended for coverage reads). Non-zero exit stops the pipeline. No built-in runtime/anchor MSBuild flow unless a script implements it.

## Logging integration
- Log start/finish with exit code and duration.
- Truncate stdout/stderr on failure with a simple size cap; avoid logging secrets.
- Include command name, working directory, and script identifiers in scope/state.

## CLI migration plan
- Replace manual parsing in MetricsReporterConsoleHost with Spectre commands: create `GenerateCommand : Command<GenerateSettings>`, `ReadCommand`, `ReadSarifCommand`, `TestCommand`; remove legacy `ParseArguments()` flow.
- Convert `MetricsReporter.Tool` into the single Spectre-based CLI (dotnet tool); remove redundant/legacy console hosts.
- Shared options: define global options (verbosity, config path, timeout) at root; per-command options in their `Settings`. Spectre will render global + command options for `--help`; branch commands are enough, no extra nesting needed beyond root + commands.

## Packaging and distribution
- Ship as a single `dotnet tool` package (nupkg) with trimmed dependencies; remove legacy hosts.
- Simple nuspec/manifest is enough; no custom metadata beyond basics. Verify discovery via `dotnet metricsreporter --help`.
- Ensure CI builds/publishes the tool and updates install docs (`dotnet tool install -g metricsreporter`).

## Exit codes and UX
- Keep consistent exit codes (0 OK, 1 parse error, 2 IO error, 3 validation error, etc.) and document them in `--help`.

## Placement vs MetricsUpdater
- `IProcessRunner` (and scripts) supersede `MetricsUpdater`'s hardcoded MSBuild calls: the orchestration should call scripts first, then proceed to parsing/aggregation. `MetricsUpdater` should be refactored/removed or become a thin orchestrator delegating to scripts + parsing without its own MSBuild invocations.

## Validation and docs
- After changes, run existing tests and add new ones if needed (CLI wiring, help output, config precedence, script selection, failure handling).
- Update or add documentation to describe the new Spectre CLI, commands, configuration file usage, script-first orchestration, and option precedence.

