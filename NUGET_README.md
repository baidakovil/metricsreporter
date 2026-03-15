# MetricsReporter

### Turn your C# chaos into Coupling < 5 and Complexity < 15. In one prompt. Measurably.

[![CI](https://github.com/baidakovil/metricsreporter/actions/workflows/ci.yml/badge.svg)](https://github.com/baidakovil/metricsreporter/actions)
[![NuGet](https://img.shields.io/nuget/v/MetricsReporter.Tool.svg?logo=nuget)](https://www.nuget.org/packages/MetricsReporter.Tool)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/baidakovil/metricsreporter/blob/master/LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![AI-Ready](https://img.shields.io/badge/AI--driven-refactoring-ff6f00)](https://github.com/baidakovil/metricsreporter#ai-driven-refactoring)

---

**MetricsReporter** is a .NET 8 CLI tool that aggregates code coverage, complexity, coupling, and analyzer violations from **three independent sources** into one interactive dashboard — then lets you (or your AI agent) fix everything via a structured refactoring loop.

![MetricsReporter Dashboard](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/dashboard_observing.png)

> [▶ Watch the interactive dashboard demo (GIF)](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/dashboard_observing.gif)

---

## 🎯 The Problem

Your C# project has growing tech debt. You *feel* the code is getting worse, but:

- **Coverage data** lives in OpenCover XML, Roslyn metrics are in a separate XML, SARIF violations in yet another file
- **No single dashboard** shows coupling, complexity, coverage, and analyzer violations together
- **You can't measure** whether a refactoring actually helped
- **AI agents don't know** which method to fix first or whether the fix worked

## ✅ The Solution

> **One command. Three sources. One dashboard. Measurable improvement.**

MetricsReporter merges **OpenCover** (coverage), **Roslyn** (complexity & coupling), and **SARIF** (analyzer violations) into a unified report. Then it gives your AI coding agent a CLI to query, refactor, verify — in a loop — until every metric is green.

---

## 🤖 AI-Driven Refactoring

Hand your AI agent a namespace and a metric. It reads the violation, studies the code, refactors, rebuilds, verifies — all through the CLI. **No human in the loop.**

```powershell
# AI agent asks: "what's broken?"
metricsreporter read --namespace MyApp.Services --metric Coupling --symbol-kind Any

# AI agent fixes the code, rebuilds, then verifies:
metricsreporter test --symbol MyApp.Services.OrderProcessor --metric Coupling
# → { "isOk": true }  ✅
```

![AI refactoring prompt](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/prompt_to_refactor.png)

*Built-in refactoring prompts for complexity, coupling, and coverage — ready for Copilot, Cursor, or any AI agent*

> **Cover 1,000 lines of code with tests. Automatically.**
> The coverage workflow reads violations, writes NUnit tests with mocks, runs them, collects coverage, and verifies — until every branch is green.

---

## 📊 Interactive HTML Dashboard

Drill down from Solution → Assembly → Namespace → Type → Method. Filter instantly, toggle warning/error awareness, hover for metric details. No frameworks — pure JS, handles **50,000+ symbols** without lag.

![Statistics](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/statistics.png)

*Aggregate statistics at a glance — coverage %, complexity distribution, violation counts*

---

## 🔍 SARIF Violations with Breakdown

See exactly which CA/IDE rules fire at each level. Hover for rule descriptions, file paths, and line numbers.

![SARIF violation tooltip](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/tooltip_on_hovering_sarif_violations.png)

*Hover any SARIF metric to see rule-by-rule breakdown with source locations*

---

## 📈 ReportGenerator Integration

Seamless integration with [ReportGenerator](https://github.com/danielpalme/ReportGenerator) for interactive, line-by-line coverage visualization alongside your metrics dashboard.

![ReportGenerator coverage](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/reportgenerator.gif)

*Line-by-line coverage maps powered by ReportGenerator*

---

## 🛡️ Suppression System

Not every violation should be fixed. Mark intentional exceptions with `[SuppressMessage]` — they show up in the dashboard with justifications, not as false alarms.

![Suppression in code](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/suppression_sample_code.png)

*Suppression attribute in code*

![Suppression in dashboard](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/suppression_sample_dashboard.png)

*Suppression reflected in dashboard with justification tooltip*

---

## ⚡ More Features

### Baseline & Delta Tracking
Every run saves a baseline. Next run computes deltas automatically. See whether complexity went up or down, whether coverage improved, whether new violations appeared — **per method**.

### Threshold Gates for CI
Define warning/error thresholds per metric per level. CLI exits with code `0` (pass) or non-zero (fail) — plug it straight into your pipeline.

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

### Smart Reconciliation
OpenCover assigns coverage to compiler-generated state machines. Roslyn lacks namespace data. MetricsReporter handles all of it — iterator coverage transferred to real methods, namespaces inferred, duplicates detected.

![State machine reconciliation](https://raw.githubusercontent.com/baidakovil/metricsreporter/master/docs/images/hovering_on_include_state_machine.png)

*Iterator state machine coverage automatically attributed to the real method*

---

## 🚀 Quick Start

### Install

```powershell
dotnet tool install --global MetricsReporter.Tool
```

### Generate your first report

```powershell
metricsreporter generate `
  --opencover coverage.xml `
  --roslyn metrics.xml `
  --sarif analyzers.sarif `
  --output-json report.json `
  --output-html report.html `
  --thresholds-file thresholds.json
```

### Or use config-driven mode

Create `.metricsreporter.json` in your repo root and run:

```powershell
metricsreporter generate
```

### Query metrics

```powershell
# Find coupling violations
metricsreporter read --namespace MyApp.Services --metric Coupling

# Verify a symbol passes thresholds
metricsreporter test --symbol MyApp.Services.OrderService.Process --metric Complexity

# Coverage violations grouped by type
metricsreporter read --namespace MyApp --metric OpenCoverBranchCoverage --group-by type
```

---

## 🤖 AI Agent Workflow

MetricsReporter ships with ready-to-use prompt files for AI agents:

| Prompt | What the agent does |
|--------|-------------------|
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
5. Repeat until clean ✨
```

---

## 📋 Metrics Sources

| Source | Metrics |
|--------|---------|
| **OpenCover** | Sequence Coverage, Branch Coverage, Cyclomatic Complexity, NPath Complexity |
| **Roslyn** | Maintainability Index, Cyclomatic Complexity, Class Coupling, Depth of Inheritance, Lines of Code |
| **SARIF** | CA-prefix (FxCop), IDE-prefix analyzer violations with rule-level breakdown |

---

## 📖 Links

- **GitHub**: [github.com/baidakovil/metricsreporter](https://github.com/baidakovil/metricsreporter)
- **Documentation**: [Full Diataxis docs](https://github.com/baidakovil/metricsreporter/tree/master/docs)
- **Changelog**: [CHANGELOG.md](https://github.com/baidakovil/metricsreporter/blob/master/CHANGELOG.md)
- **License**: [MIT](https://github.com/baidakovil/metricsreporter/blob/master/LICENSE)
