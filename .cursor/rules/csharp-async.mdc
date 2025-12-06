---
description: 'Best practices for C# async programming in Revit add-in context'
alwaysApply: false
---
# C# Async Programming Best Practices for Revit Add-ins

Your goal is to help me follow best practices for asynchronous programming in C# within Revit add-in context, considering the unique threading requirements of the Revit API.

## Naming Conventions

- Use the 'Async' suffix for all async methods
- Match method names with their synchronous counterparts when applicable (e.g., `GetDataAsync()` for `GetData()`)
- For async methods that have no sync equivalent, still use the 'Async' suffix for clarity

## Return Types

- Return `Task<T>` when the method returns a value
- Return `Task` when the method doesn't return a value
- Consider `ValueTask<T>` for high-performance scenarios to reduce allocations
- Avoid returning `void` for async methods except for event handlers
- Do not make .NET event handlers themselves `async`; keep the `void` signature and delegate to an internal async method, handling exceptions

## Revit API Threading Considerations

- **Critical**: Most Revit API calls must occur on the main UI thread
- Use `ExternalEvent` pattern for safe async operations that need Revit API access
- Never call Revit API methods directly from background threads
- Use `TaskCompletionSource<T>` to bridge between ExternalEvent and async patterns
- Consider the context when choosing between sync and async execution

## Exception Handling

- Use try/catch blocks around await expressions
- Avoid swallowing exceptions in async methods
- Use `ConfigureAwait(false)` in non-UI library code to prevent deadlocks
- In Revit context, avoid `ConfigureAwait(false)` when the continuation needs to access Revit API objects
- Propagate exceptions with `Task.FromException()` instead of throwing in async Task returning methods
- Log exceptions appropriately using the project's logging service

## Performance

- Use `Task.WhenAll()` for parallel execution of multiple tasks when they don't require Revit API
- Use `Task.WhenAny()` for implementing timeouts or taking the first completed task
- Avoid unnecessary async/await when simply passing through task results
- Consider cancellation tokens for long-running operations: `CancellationToken cancellationToken = default`
- Be cautious with parallel execution in Revit context due to API threading restrictions

## Revit ExternalEvent Pattern

```csharp
// Recommended pattern for async Revit API operations
public async Task<string> ExecuteAsync(string code)
{
    var tcs = new TaskCompletionSource<string>();
    externalEventHandler.Prepare(code, tcs);
    
    try
    {
        externalEvent.Raise();
    }
    catch (Exception ex)
    {
        tcs.TrySetException(ex);
    }
    
    return await tcs.Task;
}
```

## Common Pitfalls

- **Never** use `.Wait()`, `.Result`, or `.GetAwaiter().GetResult()` in async code
- Avoid mixing blocking and async code (leads to deadlocks)
- Don't create async void methods (except for event handlers)
- Always await Task-returning methods
- Avoid `ConfigureAwait(false)` in Revit context when accessing Revit API objects
- Don't forget to handle exceptions in fire-and-forget scenarios

## Implementation Patterns

- Implement the async command pattern for long-running operations
- Use async streams (`IAsyncEnumerable<T>`) for processing sequences asynchronously when appropriate
- Consider the task-based asynchronous pattern (TAP) for public APIs
- Lazy initialization of ExternalEvent to avoid issues in test contexts
- Use proper disposal patterns with `using` statements for resources

## Testing Async Code

- Use `async Task` for test methods (not `async void`)
- Use `Assert.ThrowsAsync<T>()` for testing exceptions
- Consider using `ConfigureAwait(false)` in test code to avoid deadlocks; optional with modern NUnit
- Mock async dependencies properly using `Task.FromResult()` or `Task.FromException()`

## Documentation

- Document async methods with `<returns>` describing the Task and its result
- Mention thread safety and Revit API context requirements in `<remarks>`
- Document any `CancellationToken` parameters and their behavior

## Example

```csharp
/// <summary>
/// Executes Python code asynchronously via Revit ExternalEvent.
/// </summary>
/// <param name="code">The Python code to execute.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>A task representing the execution result.</returns>
/// <remarks>
/// This method marshals execution to the Revit UI thread using ExternalEvent
/// to ensure safe access to the Revit API.
/// </remarks>
public async Task<string> ExecuteAsync(string code, CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(code)) 
        return string.Empty;

    var tcs = new TaskCompletionSource<string>();
    
    // Register cancellation
    using var registration = cancellationToken.Register(() => 
        tcs.TrySetCanceled(cancellationToken));
    
    try
    {
        externalEventHandler.Prepare(code, tcs);
        externalEvent.Raise();
        return await tcs.Task.ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        return FormatErrorAndLog(ex.Message);
    }
}
```

When reviewing my C# code, identify these issues and suggest improvements that follow these best practices while respecting Revit API threading requirements.
