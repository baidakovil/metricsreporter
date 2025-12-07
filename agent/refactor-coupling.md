Refactor code in the given namespace to achieve the required Class Coupling metric: the metric value must be no more than 40 for classes and no more than 11 for methods. The corresponding levels are called Type and Member.

## Requirements

- Use the `metrics-reader` utility to update and retrieve metric values for symbols requiring refactoring, one request at a time. Description and usage examples are provided in `@docs/Metrics-Reporter.md`.
- Strictly follow the workflow described below to achieve the goal: reduce the metric for all symbols in the namespace mentioned above to acceptable limits.
- When reducing Coupling, use decoupling techniques provided by C# and .NET: Interfaces, Dependency Injection, DTOs, splitting classes/methods into smaller classes/methods and creating new ones.
- It is **forbidden to use "dummy" classes and methods**, i.e., delegation wrapper methods created solely to reduce the metric but lacking architectural meaning. Prefer suppression in case when there is no way to reduce metrics further.
- Follow SOLID principles and the rules established in the project.
- Maintain nullable reference type annotations correctly when refactoring.

## Strictly follow this instruction until all symbols are fixed

### 1. Get problematic symbol

Using the `metrics-reader readany` command, get the first "problematic" symbol that requires refactoring. A problematic symbol is one where the threshold is exceeded (`metrics-reader` handles this comparison automatically). Use `--symbol-kind Any` to first automatically get and refactor classes, as this is logical from an architectural perspective, and then automatically get methods. 

**IMPORTANT: Never use `--no-update` flag when calling `metrics-reader` commands.** The `--no-update` flag skips metric generation and returns stale data. Always let `metrics-reader` update metrics automatically to ensure you work with current values.

Example request to `metrics-reader` with the required options:

```powershell
dotnet tool run metricsreporter metrics-reader readany --namespace <given_namespace> --metric Coupling --symbol-kind Any
```

If you receive a message that no suitable symbols are found (instead of an object with fields `symbolFqn`, `symbolType`, `metric`, `value`, `threshold`, `delta`, `filePath`, `status`, `isSuppressed`), this means there are no problematic symbols: complete the task.

### 2. Analyze symbol

Begin working on the symbol. Study the code of the class or method and related classes and methods. Make a decision about the possibility of refactoring.

There is only one reason to cancel refactoring: refactoring should not be performed if further decoupling techniques lead to deterioration in code readability and maintainability. If refactoring is canceled, make a suppression for the symbol. Example suppression:

```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Maintainability",
    "CA1506:AvoidExcessiveClassCoupling",
    Justification = "Revit test run coordinator is an orchestration point over VSTest abstractions and RCA execution services; low-level details are already delegated to dedicated components.")]
```

If refactoring is canceled, add the suppression to the code and start from step 1.

### 3. Perform refactoring

If refactoring is possible, proceed as follows:

1. Plan the refactoring.
2. Perform the refactoring. Update XML documentation for all modified public members according to project rules.
3. Verify that the solution build is successful: `dotnet build --no-incremental`. If the build fails, fix the code until the build is green. Use the test-driven-development method (red-refactor-green).
4. Check that there are no compiler warnings or errors in the modified files. If there are, fix them.
5. After verifying the build, check that tests pass: `dotnet test --no-build`. If tests fail, fix the code as described in the previous step.

### 4. Verify result

Using the `metrics-reader test` command, verify that the symbol you worked on is fixed. 

**IMPORTANT: Never use `--no-update` flag when calling `metrics-reader` commands.** The `--no-update` flag skips metric generation and returns stale data. Always let `metrics-reader` update metrics automatically to ensure you work with current values.

Example request to `metrics-reader` with the required options:

```powershell
dotnet tool run metricsreporter metrics-reader test --symbol <symbol_been_refactored> --metric Coupling
```

If you see `"isOk": false` in the response, return to step 2 with this symbol. The number of additional refactoring attempts to achieve the required metric: 5 attempts per symbol (applies to both classes and methods). If after the fifth additional attempt the required metric is not achieved, then add a suppression attribute with a Justification message in English that fully explains the essence of the problem, if any (for example, that five attempts were insufficient for a proper refactoring), or simply a description of the reason why this symbol cannot be refactored (for example, that it is an orchestrator and therefore must maintain many references to other methods).

If `"isOk": true`, proceed to the next symbol as described in step 1.
