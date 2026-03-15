# MetricsReporter — Features & Architecture

> **[← Back to main README](../README.md)**

## How It Works

```
  OpenCover XML ──┐
  Roslyn XML    ──┼──▶  metricsreporter generate  ──▶  report.json + report.html
  SARIF files   ──┤                                          │
  Baseline JSON ──┘                                          ▼
                                                    metricsreporter read / test
                                                             │
                                                    AI Agent refactoring loop
```

**MetricsReporter** normalizes symbols across data sources (OpenCover uses different naming than Roslyn, SARIF has its own convention), merges metrics into a canonical tree (Solution → Assembly → Namespace → Type → Member), applies thresholds, computes deltas against the previous baseline, and renders everything into a queryable JSON + interactive HTML.

## Feature Deep Dive

### The Dashboard

The HTML report is a single self-contained file with no external dependencies, no CDN calls, no build step. It runs on plain ES5 JavaScript and handles 50k+ rows without lag.

**What you can do:**

- **Drill down** — expand/collapse any level of the hierarchy
- **Filter** — instant substring search across all fully qualified names
- **Awareness slider** — show All / Warnings only / Errors only
- **Detail slider** — control nesting depth without re-rendering
- **Tooltips** — hover any metric for definition, aliases, source location, SARIF rule breakdown
- **Copy FQN** — one-click copy of any symbol's fully qualified name
- **Persistent preferences** — filter/awareness/detail settings survive browser refresh (localStorage)

![Dashboard](images/dashboard_observing.gif)

### AI Agent Integration — The Refactoring Loop

MetricsReporter ships with structured prompt files (`Metrics/Agent/refactor-*.md`) designed for AI coding agents. Each prompt defines a complete workflow:

1. **Query** — `metricsreporter read` finds the first threshold violation
2. **Analyze** — Agent studies the symbol's code and dependencies
3. **Refactor** — Agent applies C#/.NET best practices (guard clauses, DI, strategy pattern, etc.)
4. **Build & Test** — `dotnet build && dotnet test` to validate
5. **Verify** — `metricsreporter test` confirms the metric is now green
6. **Repeat** — Loop continues until no violations remain

Available prompts:

| File | Goal | Threshold |
|------|------|-----------|
| `refactor-complexity.md` | Reduce cyclomatic complexity | ≤ 15 (type), ≤ 100 (member) |
| `refactor-coupling.md` | Reduce class coupling | ≤ 40 (type), ≤ 11 (member) |
| `refactor-coverage.md` | Increase branch coverage | ≥ 50% (type), ≥ 75% (member) |
| `refactor-sarif.md` | Fix analyzer violations | 0 violations |

**Coverage workflow** is especially powerful — the agent writes NUnit tests with NSubstitute mocks, following AAA pattern, runs them, coverage is collected by OpenCover, and the agent verifies the results. It can cover entire namespaces in one session.

### Suppression System

Two placement options:

```csharp
// Symbol-level — directly on the method/class
[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
    Justification = "Validation flow requires multiple branches; splitting harms readability.")]
public void Validate() { }

// Assembly-level — centralized in GlobalSuppressions.cs
[assembly: SuppressMessage("Microsoft.Maintainability", "CA1506",
    Target = "~M:MyApp.Services.Orchestrator.Run(System.String)",
    Justification = "Orchestrator coordinates 8 services by design.")]
```

Suppressions appear in a dedicated `SuppressedSymbols.g.json` output and are rendered in the dashboard with visible justification tooltips — making it clear these are conscious decisions, not ignored violations.

### Threshold Configuration

Thresholds are defined per metric per symbol level (Solution / Assembly / Namespace / Type / Member):

```json
{
  "OpenCoverBranchCoverage": {
    "Type":   { "warning": 50, "error": 0 },
    "Member": { "warning": 75, "error": 0 }
  },
  "RoslynCyclomaticComplexity": {
    "Type":   { "warning": 15, "error": 100 },
    "Member": { "warning": 15, "error": 100 }
  },
  "RoslynClassCoupling": {
    "Type":   { "warning": 20, "error": 40 },
    "Member": { "warning": 5,  "error": 11 }
  },
  "SarifCaRuleViolations": {
    "Type":   { "warning": 0, "error": 0 },
    "Member": { "warning": 0, "error": 0 }
  }
}
```

Each metric value gets a status: `Success`, `Warning`, or `Error`. CLI exit codes reflect the worst status — so you can gate deployments on quality.

### Script Hooks

`.metricsreporter.json` supports pre/post-processing scripts:

```json
{
  "scripts": {
    "generate": ["scripts/prepare-metrics.ps1"],
    "read": {
      "any": ["scripts/refresh-report.ps1"],
      "byMetric": [
        {
          "metrics": ["OpenCoverBranchCoverage", "OpenCoverSequenceCoverage"],
          "path": "scripts/coverage.ps1"
        }
      ]
    }
  }
}
```

Metric-scoped scripts fire only when the matching metric is queried — so coverage collection only runs when the agent asks about coverage.

### Reconciliation Engine

MetricsReporter handles three complex data alignment problems:

1. **Namespace inference** — OpenCover XML has no namespace concept; MetricsReporter reconstructs namespaces using longest-prefix matching from Roslyn data, with FQN-slicing fallback
2. **Iterator state machine coverage** — OpenCover reports coverage on compiler-generated types like `Outer+<Method>d__0`; MetricsReporter detects these, transfers metrics to the real method, and removes noise from reports
3. **Plain nested type reconciliation** — Similar transfer for non-iterator nested types that are implementation details

---

## Documentation (Diataxis)

This folder organizes every document into the four Diataxis quadrants:

- **[Tutorials](./1-tutorials/1.0%20-%20README.md)** — Learning-oriented lessons that guide newcomers through a complete outcome
- **[How-To Guides](./2-how-to-guides/2.0%20-%20README.md)** — Problem-focused recipes for recurring tasks
- **[Reference](./3-reference/3.0%20-%20README.md)** — CLI commands, configuration schema, report formats, suppression rules
- **[Explanation](./4-explanation/4.0%20-%20README.md)** — Architecture deep dives, coverage pipeline, namespace inference

