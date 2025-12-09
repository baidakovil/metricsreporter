# Increase test coverage for AltCover branch coverage

- Refactor code in the given namespace to achieve the required `AltCoverBranchCoverage` metric: all methods in classes must have sufficient branch coverage 75 for methods and 50 for classes. The goal is to ensure comprehensive test coverage for all code paths, including edge cases and error conditions.
- When working with coverage metrics, use the exact metric identifiers (e.g., `AltCoverBranchCoverage`, `AltCoverSequenceCoverage`) — aliases like `Coverage` are not supported.

## Requirements

- Use the `metricsreporter` CLI to update and retrieve metric values for symbols requiring test coverage improvement, one class at a time. Description and usage examples are provided in `docs/3-reference/3.2 - metricsreporter-cli.md`.
- Strictly follow the workflow described below to achieve the goal: increase branch coverage for all methods in all classes within the namespace mentioned above to acceptable levels.
- When writing tests, follow the testing best practices:
	•	Use Arrange-Act-Assert (AAA) pattern for test structure
	•	Follow requirements from `MetricsReporter.Tests/.cursor/rules/csharp-nunit.prompt.mdc`
	•	Use mock objects (NSubstitute) for dependency isolation
	•	Use meaningful test names that reflect the scenario being tested (pattern: `MethodName_Scenario_ExpectedBehavior`)
	•	Add a 1–4 line description before each test explaining what it verifies and why it matters
	•	Test both successful and failure scenarios
	•	Check boundary conditions and edge cases
	•	Do not ignore exceptions, nullable annotations, or complex states
	•	Write 2 to 5 test scenarios per method (1 only if the method is trivial)
- Tests must be of high quality: they should verify not only "happy path" scenarios but also edge cases, exception handling, nullable reference type behavior, and complex state transitions.
- Follow SOLID principles and the rules established in the project.
- Maintain nullable reference type annotations correctly when writing tests.

## Strictly follow this instruction until all classes are covered

### 1. Get problematic class

Using the `metricsreporter read` command, get the first "problematic" class that requires test coverage improvement. A problematic class is one that contains methods with insufficient branch coverage (`metricsreporter` handles this comparison automatically). Use `--group-by type` to get classes grouped by type. Always specify the full metric identifier (e.g., `AltCoverBranchCoverage`).

Example request to `metricsreporter` with the required options:

```powershell
dotnet tool run metricsreporter read --namespace <given_namespace> --metric AltCoverBranchCoverage --group-by type
```

If you receive a message that no suitable symbols are found (instead of an object with fields `metric`, `namespace`, `symbolKind`, `groupBy`, `violationsGroupsCount`, `violationsGroups`), this means there are no problematic classes: complete the task.

### 2. Analyze class and identify methods requiring tests

Begin working on the class. Study the code of the class, its methods with insufficient coverage, and related classes. Understand the business logic, dependencies, and code paths that need to be tested.

For each method in the class that has insufficient branch coverage, identify:
- All code paths (branches) that need to be covered
- Dependencies that need to be mocked
- Edge cases and boundary conditions
- Exception scenarios
- Nullable reference type scenarios
- Complex state transitions

### 3. Cancel conditions (prefer suppression, not exclusion)

Перед тем как писать тесты, оцени, нужно ли подавить метрику, а не исключать код из покрытия:

- Подавлять (рекомендуется): если метод/тип шумный для метрик, но его нужно оставить видимым в отчёте. Используй `[SuppressMessage("AltCoverBranchCoverage", "AltCoverBranchCoverage", Justification = "...")]` и `[SuppressMessage("AltCoverSequenceCoverage", "AltCoverSequenceCoverage", Justification = "...")]`. Метрика останется в отчёте и в HTML, но будет помечена как suppressed.


Если метод/тип подавлён, переходи к следующему. Если всё же решено исключить, добавь `[ExcludeFromCodeCoverage]` и продолжай по шагам.

### 4. Write tests

If tests should be written, proceed as follows:

1. Plan the test scenarios for each method:
   - Identify 2 to 5 test scenarios per method (1 only if the method is trivial)
   - Include happy path, edge cases, exception scenarios, and nullable cases
   - Determine which dependencies need to be mocked
2. Write test methods following the AAA pattern:
   - **Arrange**: Set up test data, mocks, and initial state
   - **Act**: Execute the method under test
   - **Assert**: Verify the expected outcome using FluentAssertions
   - Precede the test with a 1–4 line description of the scenario and its purpose
3. Use meaningful test names following the pattern `MethodName_Scenario_ExpectedBehavior`
4. Ensure tests follow all requirements from `@tests/.cursor/rules/csharp-nunit.prompt.mdc`
5. Verify that the solution build is successful: `dotnet build --no-incremental`. If the build fails, fix the code until the build is green.
6. Check that there are no compiler warnings or errors in the modified files. If there are, fix them.
7. Run tests to verify they pass. **IMPORTANT: Run tests only for the specific test project that corresponds to the project being worked on, not all tests.**
   Do not run general `dotnet test` command without specifying the test project. If tests fail, fix the tests or the code until all tests pass.

### 5. Verify coverage result

Using the `metricsreporter read` command with `--all` flag, verify that all methods in the class you worked on have sufficient branch coverage.

Example request to `metricsreporter` with the required options:

```powershell
dotnet tool run metricsreporter read --namespace <class_full_name> --metric AltCoverBranchCoverage --all
```

Where `<class_full_name>` is the fully qualified name of the class (type) for which tests were written.

If the command returns any methods with insufficient coverage (non-empty `violationsGroups` array), return to step 2 to write additional tests for the remaining uncovered methods. Coverage is collected automatically during the metrics update process.

If the command returns no violations (empty list or message indicating no problematic symbols), proceed to the next class as described in step 1.
