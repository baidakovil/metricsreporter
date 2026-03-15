---
applyTo: "**"
---

## Project Overview

MetricsReporter is a .NET 8 CLI tool that aggregates code coverage (OpenCover XML), complexity and coupling (Roslyn metrics XML), and analyzer violations (SARIF) from three independent sources into a single interactive HTML dashboard.

The tool supports AI-driven refactoring loops: it exposes a CLI to query violations, and the AI agent fixes the code, rebuilds, and verifies — iterating until every metric is green.

Key projects:
- `MetricsReporter/` — core library (parsing, processing, rendering, CLI commands)
- `MetricsReporter.Tests/` — NUnit test suite
- `MetricsReporter.Tool/` — .NET tool entry point

## AI-First Development Principles

- **Linear, step-by-step code flows** — Avoid complex nested logic that's hard for AI to understand and modify
- **Comprehensive WHY documentation** — Every non-trivial decision must be explained in comments or XML docs
- **Explicit error conditions** — Handle and document all expected failure modes
- **Prefer immutable data structures** — Reduces state-related bugs and makes code more predictable
- **Single responsibility principle** — Each method and class should have one clear purpose

## Code Standards

- Run full solution build for every build/test; per-project building is not allowed
- Follow `.editorconfig` for formatting, naming, and code style conventions
- Add XML doc comments (`///`) above every public class and method; follow `.github/instructions/csharp-docs.instructions.md`
- Name files to match the primary class they contain; organize code into folders by feature
- Use `using` directives only for namespaces you reference
- Avoid magic strings — define all literal strings as `const` or resource entries
- Prefer `ArgumentNullException.ThrowIfNull(param)` for parameter validation
- Enable nullable reference types: `<Nullable>enable</Nullable>` in every `.csproj`
- Use C# 12+ features: primary constructors, collection expressions, .NET 8 optimizations
- Build after every significant change: `dotnet build --no-incremental`

## Testing

- Write unit tests for all non-UI logic in `MetricsReporter.Tests/` following `.github/instructions/csharp-nunit.instructions.md`
- Nullable fields in test classes: `private TypeName _field = null!;` initialized in `[SetUp]`, not nullable types
- Use NSubstitute for mocking; use dependency injection for all services

## Architecture Patterns

### Dependency Injection
- Constructor injection with `ArgumentNullException.ThrowIfNull`
- Register services with appropriate lifetimes (Singleton, Scoped, Transient)
- Primary constructor syntax: `public class MyClass(IDependency dep)`

### Design Patterns
- Command Handler: `CommandHandler<TOptions>`, `ICommandHandler<TOptions>`, `CommandHandlerOptions` inheritance
- Factory pattern for complex object creation
- Interface segregation: prefix with `I`, name for capability (e.g., `IMetricsParser`, `IReportRenderer`)

## Documentation

- XML docs for all public APIs
- Update `docs/` when functionality changes; follow Diátaxis framework (tutorials / how-to / reference / explanation)

## Error Management

- Structured logging with `Microsoft.Extensions.Logging` patterns
- Meaningful error messages with context
- Use appropriate exception types per error condition
