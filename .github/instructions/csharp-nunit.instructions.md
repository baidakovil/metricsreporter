---
applyTo: "MetricsReporter.Tests/**/*.cs"
---

# NUnit Best Practices for MetricsReporter

## Project Setup

- Use separate test projects for every `.csproj` with naming convention `[ProjectName].Tests`
- Target framework: `net8.0`
- Required packages (pinned for reproducibility):
  - `Microsoft.NET.Test.Sdk` version `17.9.0`
  - `NUnit` version `3.14.0`
  - `NUnit3TestAdapter` version `4.5.0`
  - `FluentAssertions` version `6.12.0`
  - `NSubstitute` version `5.1.0`
- Run tests with `dotnet test` (full solution, not per-project)
- Test class names match the class under test (e.g., `SymbolNormalizerTests` for `SymbolNormalizer`)

## Test Structure

- Apply `[TestFixture]` attribute to test classes; `[Test]` to test methods
- Follow the Arrange-Act-Assert (AAA) pattern
- Precede each test with a 1–4 line description of intent: what behavior/scenario it verifies and why it matters
- Name tests using the pattern `MethodName_Scenario_ExpectedBehavior`
- Use `[SetUp]` / `[TearDown]` for per-test setup and teardown
- Use `[OneTimeSetUp]` / `[OneTimeTearDown]` for per-class setup and teardown

## Nullable Field Conventions

- Declare test fields that are initialized in `[SetUp]` as `private TypeName _field = null!;` — use `null!` not `TypeName?`
- This prevents unnecessary nullable warnings and signals the field is guaranteed non-null in all `[Test]` methods

## Assertion Style

- Use `FluentAssertions` for all assertions (`result.Should().Be(expected)`)
- Prefer `using FluentAssertions;` at the top level
- Avoid `Assert.That(...)` when FluentAssertions is available

## Mocking

- Use `NSubstitute` for all mocking
- Create substitutes with `Substitute.For<IInterface>()`
- Configure with `.Returns()`, verify calls with `.Received()`

## Test Coverage Guidelines

- Target branch coverage (both true and false paths for all conditions)
- Write 2–5 scenarios per method: happy path, edge cases, exception handling
- Test names must describe the specific scenario, not just the method name

## Data-Driven Tests

- Use `[TestCase(...)]` for simple parameterized scenarios
- Use `[TestCaseSource(...)]` for complex test data sets
- Prefer inline `[TestCase]` when parameters fit on one line

## What Not to Test

- Private implementation details (test via public interface)
- Framework behavior (e.g., .NET built-in types or NSubstitute mechanics)
- Auto-generated or trivial boilerplate code
