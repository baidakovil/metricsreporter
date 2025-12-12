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
- Remove legacy `FileLogger` usage; if file logs are needed, add a DI-registered `ILoggerProvider` with a file sink. Default setup: console only.

## Style guide (applied)
- Use `ILogger<T>` with scopes that include solution, metrics directory, and output paths; avoid static loggers.
- Map verbosity: quiet/minimal → Warning, normal → Information, detailed → Debug.
- Milestones → Information (start/finish, file I/O begin/end with duration + paths); anomalies → Warning; failures → Error with `LogError(exception, ...)`.
- Truncate stdout/stderr to 4000 chars via `LogTruncator` and log the truncated value explicitly.
- Prefer structured messages (`logger.LogInformation("Parsing metrics file {Path}", path)`) and avoid interpolated strings.
- External process runs: log start, exit code, duration, and truncated streams when non-zero/timeout.

## Validation and docs
- After changes, run existing tests and add new ones if needed (e.g., logging config validation, truncation behavior).
- Update or add documentation to reflect logging setup and options.

## Constraints
- Nullable enabled; DI-first architecture.
- No backward compatibility requirements.
- Align with SOLID and patterns from `dotnet-design-pattern-review.mdc`.