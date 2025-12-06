## Goal
Rebuild a single Spectre.Console.Cli application with modern command surface; backward compatibility is NOT required.

## Requirements
- One entry point: `metricsreporter` dotnet tool; remove dual hosts. Package only this tool.
- Commands:
  - `generate` (current metrics aggregation)
  - `read` (rename old `readany`; returns metric violations)
  - `readsarif` (SARIF-focused)
  - `test` (single symbol check)
- Shared options via base settings (report path, thresholds, include-suppressed, group-by, symbol-kind).
- Use Spectre validation/Help; consistent exit codes.
- DI-driven command handlers; no static parsing.
- Update docs/help text to reflect new names and single entry point.

## Constraints
- No backward compatibility; old `metrics-reader` prefix should be dropped.
- Keep commands linear and AI-friendly per `dotnet-design-pattern-review.mdc`.

## Packaging and distribution
- Ship as a single `dotnet tool` package (nupkg) with trimmed dependencies; remove legacy hosts.
- Simple nuspec/manifest is enough; no custom metadata beyond basics. Verify discovery via `dotnet metricsreporter --help`.
- Ensure CI builds/publishes the tool and updates install docs (`dotnet tool install -g metricsreporter`).

## Configuration and options alignment
- Typed options (`Microsoft.Extensions.Options` + validation) with a shared minimal schema: inputs/outputs, thresholds, verbosity, timeout, script lists. Defaults for the rest.
- Config file: `.metricsreporter.json` resolved from CWD upward (no global file). Sources priority: CLI > env > config file > defaults.
- Provide global options: verbosity, config path override, timeout defaults. No fancy log format switches.
- Validation errors should be reported with Spectre validation messages before command execution.
- Scripts in config/CLI: allow multiple entries; executed in listed order. `generate` uses its scripts before aggregation; `read` can define common scripts and per-metric scripts (e.g., `any` list always, `coverage` appended for coverage reads). Non-zero exit stops the pipeline. No built-in runtime/anchor MSBuild flow unless a script implements it.

## Exit codes and UX
- Keep consistent exit codes (0 OK, 1 parse error, 2 IO error, 3 validation error, etc.) and document them in `--help`.
- Emit migration hints in error/help texts to map old command names/options to the new surface (no execution of legacy aliases).
