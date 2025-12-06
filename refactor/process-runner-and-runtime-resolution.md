## Goal
Introduce an `IProcessRunner` for MSBuild/external commands and explicit runtime project selection. Backward compatibility is NOT required.

## Requirements
- Create `IProcessRunner` + implementation: run process with timeout, cancellation, captured stdout/stderr, exit code, start/stop timestamps. Return a result DTO; no direct console writes.
- Inject `IProcessRunner` into metrics generation orchestrator; remove direct `ProcessStartInfo`/`Process` usage.
- Add explicit CLI option/config for runtime project path; delete globbing for `*Runtime.csproj`. Fail fast with clear error when missing.
- Log (via ILogger) command, working dir, exit code, duration, truncated outputs on failure.
- Surface errors as result + enum exit code + clear message; typed exceptions only if tests really need them.

## Constraints
- Align with SOLID and patterns in `dotnet-design-pattern-review.mdc`; DI-first.
- No backward compatibility; legacy env var fallbacks can be removed.

## Options and configuration alignment
- Minimal typed options (`Microsoft.Extensions.Options` + validation): runtime project path, anchor project path, timeout, verbosity, script paths. Defaults cover the rest.
- Options populated from CLI/env/config with the same schema reused by generate/read commands.
- Cancellation token flows from CLI command handlers through orchestrator into `IProcessRunner`.

## Logging integration
- Log start/finish with exit code and duration.
- Truncate stdout/stderr on failure with a simple size cap; avoid logging secrets.
- Include command name, working directory, and runtime/anchor project paths in scope/state.

## Script hooks
- Support executing one or many PowerShell scripts (no cross-platform requirement) provided via CLI/config; run sequentially in the order given.
- Scripts can be used for coverage/metrics prep; pipeline stops on the first non-zero exit code. Contract: 0 = success, >0 = fail.
- Runner passes working directory (default CWD), inherits env, applies timeout/verbosity; capture stdout/stderr with truncation.
