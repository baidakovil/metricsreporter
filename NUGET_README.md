# MetricsReporter


**Turn your C# chaos into Coupling < 5 and Complexity < 15. In one prompt. Measurably.**

[![CI](https://github.com/baidakovil/metricsreporter/actions/workflows/ci.yml/badge.svg)](https://github.com/baidakovil/metricsreporter/actions)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-181717?logo=github)](https://github.com/baidakovil/metricsreporter)
[![NuGet](https://img.shields.io/nuget/v/MetricsReporter.Tool.svg?logo=nuget)](https://www.nuget.org/packages/MetricsReporter.Tool)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/baidakovil/metricsreporter/blob/master/LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)

[![Coverage](https://codecov.io/gh/baidakovil/metricsreporter/branch/master/graph/badge.svg)](https://codecov.io/gh/baidakovil/metricsreporter)
<img alt="Tests" src="https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/baidakovil/8fa349f2c1c8422a8c3e831343542811/raw/metricsreporter-tests.json">
<img alt="Lines of code" src="https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/baidakovil/8fa349f2c1c8422a8c3e831343542811/raw/metricsreporter-loc.json">

---

**MetricsReporter** is a .NET 8 CLI tool that aggregates code coverage, complexity, coupling, and analyzer violations from three independent sources into one interactive dashboard — then lets you (or your AI agent) fix everything via a structured refactoring loop.

![MetricsReporter Dashboard](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/dashboard_observing.png)

**▶ [Open Live Interactive Demo](https://baidakovil.github.io/metricsreporter/docs/samples/MetricsReport.html)**

---

## Production-Ready Example

MetricsReporter is also demonstrated on the production-ready [baidakovil/eShop](https://github.com/baidakovil/eShop) repository, which shows a full end-to-end setup on a non-trivial solution: Roslyn metrics, SARIF diagnostics, OpenCover coverage, ReportGenerator output, AI-assisted refactoring, and a published HTML dashboard. See the interactive demo for the eShop example here: https://baidakovil.github.io/eShop/MetricsReport.html


---

```
  coverage.xml  +  metrics.xml  +  violations.sarif   →   one interactive HTML
  (OpenCover)      (Roslyn)        (Analyzers)              + unified JSON
```

---


## The Problem

Your C# project has growing tech debt, but:

- **Coverage, metrics, and violations live in three separate files** — OpenCover XML, Roslyn XML, and SARIF JSON
- **No single view** shows coupling, complexity, coverage, and analyzer violations together
- **You can't measure** whether a refactoring actually helped
- **AI agents don't know** which method to fix first or whether the fix worked

---

## Quick Start

```powershell
# Install
dotnet tool install --global MetricsReporter.Tool

# Generate dashboard from your three data sources
metricsreporter generate --opencover coverage.xml --roslyn metrics.xml --sarif analyzers.sarif --output-html report.html

# Query violations from CLI — returns JSON
metricsreporter read --namespace MyApp.Services --metric Coupling
# → [{"kind":"Type","fullyQualifiedName":"MyApp.Services.OrderService","metrics":{"RoslynClassCoupling":{"value":14,"status":"Warning"}}}]

# Verify a fix passes thresholds
metricsreporter test --symbol MyApp.Services.OrderService.Process --metric Complexity
# → {"isOk":true}
```

Open `report.html` — you'll see the dashboard from the screenshot above.

> **Next step →** [Full tutorial: produce your first dashboard](https://github.com/baidakovil/metricsreporter/blob/master/docs/1-tutorials/1.1%20-%20first-metrics-run.md) · [CLI reference](https://github.com/baidakovil/metricsreporter/blob/master/docs/3-reference/3.2%20-%20metricsreporter-cli.md) · [Configuration reference](https://github.com/baidakovil/metricsreporter/blob/master/docs/3-reference/3.1%20-%20configuration-options.md)

---

## AI-Driven Refactoring

Hand your AI agent a namespace and a metric. It reads the violation, studies the code, refactors, rebuilds, verifies — all through the CLI. **No human in the loop.**

```powershell
# AI agent asks: "what's broken?"
metricsreporter read --namespace MyApp.Services --metric Coupling

# AI agent fixes the code, rebuilds, then verifies:
metricsreporter test --symbol MyApp.Services.OrderProcessor --metric Coupling
# → { "isOk": true }
```

![AI refactoring prompt](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/prompt_to_refactor.png)

*Built-in refactoring prompts for complexity, coupling, and coverage — ready for Copilot, Cursor, or any AI agent*

> **Cover 1,000 lines of code with tests. Automatically.**
> The coverage workflow reads violations, writes NUnit tests with mocks, runs them, collects coverage, and verifies — until every branch is green.

---

## Interactive HTML Dashboard

Drill down from Solution → Assembly → Namespace → Type → Method. Filter instantly, toggle warning/error awareness, hover for metric details. No frameworks — pure JS, handles **50,000+ symbols** without lag.

![Statistics](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/statistics.png)

*Aggregate statistics at a glance — coverage %, complexity distribution, violation counts*

---

## SARIF Violations with Breakdown

See exactly which CA/IDE rules fire at each level. Hover for rule descriptions, file paths, and line numbers.

![SARIF violation tooltip](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/tooltip_on_hovering_sarif_violations.png)

*Hover any SARIF metric to see rule-by-rule breakdown with source locations*

---

## ReportGenerator Integration

Seamless integration with [ReportGenerator](https://github.com/danielpalme/ReportGenerator) for interactive, line-by-line coverage visualization alongside your metrics dashboard.

![ReportGenerator coverage](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/reportgenerator.gif)

*Line-by-line coverage maps powered by ReportGenerator*

---

## Suppression System

Not every violation should be fixed. Mark intentional exceptions with `[SuppressMessage]` — they show up in the dashboard with justifications, not as false alarms.

![Suppression in code](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/suppression_sample_code.png)

*Suppression attribute in code*

![Suppression in dashboard](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/suppression_sample_dashboard.png)

*Suppression reflected in dashboard with justification tooltip*

---

## More Features

**Baseline & Delta Tracking** — Every run saves a baseline. Next run computes deltas automatically. See whether complexity went up or down, whether coverage improved, whether new violations appeared — **per method**.

**Threshold Gates for CI** — Define warning/error thresholds per metric per level. CLI exits with code `0` (pass) or non-zero (fail) — plug it straight into your pipeline.

```json
{
  "RoslynClassCoupling": {
    "Type":   { "warning": 20, "error": 40 },
    "Member": { "warning": 5,  "error": 11 }
  },
  "RoslynCyclomaticComplexity": {
    "Type":   { "warning": 15, "error": 100 },
    "Member": { "warning": 15, "error": 100 }
  }
}
```

**Smart Reconciliation** — OpenCover assigns coverage to compiler-generated state machines. Roslyn lacks namespace data. MetricsReporter handles all of it — iterator coverage transferred to real methods, namespaces inferred, duplicates detected.

![State machine reconciliation](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/hovering_on_include_state_machine.png)

*Iterator state machine coverage automatically attributed to the real method*

---


## AI Agent Workflow

MetricsReporter ships with ready-to-use prompt files for AI agents:

| Prompt file | What the agent does |
|-------------|-------------------|
| `refactor-complexity.md` | Reduce cyclomatic complexity below thresholds |
| `refactor-coupling.md` | Reduce class coupling with DI, interfaces, DTOs |
| `refactor-coverage.md` | Write tests until branch coverage passes |
| `refactor-sarif.md` | Fix CA/IDE analyzer violations |

**The loop:**

```
1. metricsreporter read  → find violation
2. Study code            → plan refactoring
3. Refactor + build      → apply changes
4. metricsreporter test  → verify fix
5. Repeat until clean
```

---

## Metrics Sources

| Source | Metrics |
|--------|---------|
| **OpenCover** | Sequence Coverage, Branch Coverage, Cyclomatic Complexity, NPath Complexity |
| **Roslyn** | Maintainability Index, Cyclomatic Complexity, Class Coupling, Depth of Inheritance, Lines of Code |
| **SARIF** | CA-prefix (FxCop), IDE-prefix analyzer violations with rule-level breakdown |

---

## Links

- **GitHub**: [github.com/baidakovil/metricsreporter](https://github.com/baidakovil/metricsreporter)
- **Documentation**: [Full Diataxis docs](https://github.com/baidakovil/metricsreporter/tree/master/docs)
- **Tutorial**: [Produce your first dashboard](https://github.com/baidakovil/metricsreporter/blob/master/docs/1-tutorials/1.1%20-%20first-metrics-run.md)
- **CLI Reference**: [metricsreporter-cli.md](https://github.com/baidakovil/metricsreporter/blob/master/docs/3-reference/3.2%20-%20metricsreporter-cli.md)
- **Configuration Reference**: [configuration-options.md](https://github.com/baidakovil/metricsreporter/blob/master/docs/3-reference/3.1%20-%20configuration-options.md)
- **Changelog**: [CHANGELOG.md](https://github.com/baidakovil/metricsreporter/blob/master/CHANGELOG.md)
- **License**: [MIT](https://github.com/baidakovil/metricsreporter/blob/master/LICENSE)
