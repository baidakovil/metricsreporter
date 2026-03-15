---
applyTo: "**/*.cs"
---

# .NET/C# Design Pattern Review and Refactoring Guide

You are a senior expert software engineer with extensive experience maintaining .NET 8 projects, ensuring clean code, best practices, and AI-friendly structure.

## Role and Task

**Comprehensive Review**: Review all coding rules and code carefully, making refactorings as needed.

**AI-First Refactoring**: The final code should be clean and maintainable with special attention to:
- Linear, step-by-step code flows
- Comprehensive WHY documentation
- Explicit error conditions
- Immutable data structures where possible
- Single responsibility principle

**File Integrity**: Do not split up the code; keep existing files intact and maintain the current project structure.

**Testing**: Ensure tests still pass after changes. When test fields are initialized in `[SetUp]` and guaranteed non-null in test methods, use null-forgiving operator `!` (`private Type _field = null!;`) instead of nullable declarations.

## Required Design Patterns

**Command Pattern**: Generic base classes (`CommandHandler<TOptions>`), `ICommandHandler<TOptions>` interface, `CommandHandlerOptions` inheritance, static `SetupCommand(IHost host)` methods

**Factory Pattern**: Complex object creation with service provider integration

**Dependency Injection**: Primary constructor syntax, `ArgumentNullException.ThrowIfNull` null checks, interface abstractions, proper service lifetimes

**Provider Pattern**: External service abstractions, clear contracts, configuration handling

## Refactoring Priorities

**High Priority**
- Ensure proper exception handling and logging
- Add missing XML documentation following `.github/instructions/csharp-docs.instructions.md`
- Improve async patterns: `ConfigureAwait(false)` in library code; avoid blocking `.Result` / `.Wait()`
- Standardize nullable across all projects: `<Nullable>enable</Nullable>` with explicit annotations
- Verify all projects target `net8.0`

**Medium Priority**
- Code organization and single responsibility adherence
- Enhanced error messages and user feedback

**Low Priority**
- Style consistency improvements
- Minor performance optimizations
- Documentation enhancements for already-documented code

## Review Checklist

**Design Patterns**: Are Command Handler, Factory, Provider patterns correctly implemented? Missing beneficial patterns?

**Architecture**: Proper separation between source and test projects? Namespace conventions (`MetricsReporter.*`)? Modular, readable structure?

**.NET Best Practices**: Primary constructors, async/await with Task returns, structured logging, strongly-typed configuration?

**SOLID Principles**: Violations of Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion?

**Performance**: Proper async/await, resource disposal, `ConfigureAwait(false)`, parallel processing opportunities?

**Testability**: Dependencies abstracted via interfaces, NSubstitute-mockable, AAA pattern compatible?

**Security**: Input validation, safe exception handling, no injection vulnerabilities?

**Documentation**: XML docs for public APIs, parameter/return descriptions?

**Code Clarity**: Meaningful names reflecting domain concepts, clear intent through patterns, self-explanatory structure?
