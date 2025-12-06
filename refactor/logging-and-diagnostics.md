## Goal
Introduce structured logging across the solution using `Microsoft.Extensions.Logging`. Backward compatibility is NOT required.

## Requirements
- Wire `ILogger<T>` into orchestration, parsers, services, and CLI hosts; prefer DI over statics. `LoggerMessageAttribute` optional and only for hottest paths.
- Standardize log levels: Information for milestones, Warning for recoverable anomalies, Error for failures; include event context (command, solution, project, metric, paths).
- Centralize logger creation with a simple console logger; avoid `Console.WriteLine`. Tests use NullLogger or a lightweight sink.
- Add logging to external process runs (MSBuild, file I/O, report loading) with start/finish + duration + exit code; truncate stdout/stderr in logs with a fixed size cap.
- Capture exceptions with basic structured properties (command text, file paths); avoid logging secrets; no complex redaction/throttling.
- Keep logging lightweight and linear; avoid global static loggers. Policies consistent across services/CLI.
- Provide a short logging style guide (when to log, level mapping, simple truncation rule).

## Constraints
- Nullable enabled; DI-first architecture.
- No backward compatibility requirements.
- Align with SOLID and patterns from `dotnet-design-pattern-review.mdc`.

## Options and validation alignment
- Minimal typed options (`Microsoft.Extensions.Options` + validation): verbosity and log truncation limit. Populate from CLI/env/config file; defaults are fine otherwise.
- Shared schema: generation and reader commands consume the same basic logging settings.

