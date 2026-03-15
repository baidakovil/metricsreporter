---
applyTo: "**/*.cs"
---

# C# Documentation Best Practices

## Core Requirements

- **All public members** must have XML doc comments
- **Internal members** should be documented if complex or non-obvious
- **Private methods** with complex logic should have explanatory inline comments
- Documentation language is always **English**
- Focus on **WHY**, not just WHAT — explain decisions, context, and reasoning

## Standard XML Tags

- `<summary>` — brief description of what the member does
- `<param>` — each method parameter with a clear description
- `<paramref>` — reference a parameter name inside `<summary>` or `<remarks>`
- `<returns>` — return value; for async methods describe what the `Task` resolves to
- `<remarks>` — additional context, implementation notes, usage guidance
- `<example>` — usage example showing how to call the member
- `<exception>` — exceptions thrown, with the condition that triggers each
- `<see langword="null"/>` — for `null`, `true`, `false` and other language keywords
- `<see cref="Type"/>` — inline reference to another type or member
- `<seealso cref="Type"/>` — standalone "see also" reference
- `<inheritdoc/>` — inherit docs from base class or interface
- `<typeparam>` — type parameters in generic types or methods
- `<c>` — inline code within prose
- `<code language="csharp">` — code block inside `<example>`

## AI-First Documentation Principles

- **Explain WHY decisions were made** — the context helps AI agents maintain code correctly
- **Document error conditions explicitly** — what can go wrong and under what circumstances
- **Document performance implications** — memory allocations, blocking calls, large inputs
- **Include pre-conditions and post-conditions** when non-obvious
- **Avoid restating the signature** — `/// <summary>Gets the name.</summary>` adds zero value; explain the intent

## Examples

```csharp
/// <summary>
/// Normalizes a fully qualified symbol name from OpenCover format to Roslyn format.
/// OpenCover emits return types prefixed before the method name; this method strips them
/// so symbols can be matched across both metric sources.
/// </summary>
/// <param name="rawSymbol">The raw symbol string as emitted by OpenCover XML output.</param>
/// <returns>
/// The normalized symbol string without return-type prefix, or <paramref name="rawSymbol"/>
/// unchanged if the format is not recognized.
/// </returns>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="rawSymbol"/> is <see langword="null"/>.
/// </exception>
public string Normalize(string rawSymbol) { ... }
```
