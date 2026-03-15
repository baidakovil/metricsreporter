<p align="center">
  <h1 align="center">MetricsReporter</h1>
  <p align="center">
    <b>Turn your C# chaos into Coupling&nbsp;&lt;&nbsp;5 and Complexity&nbsp;&lt;&nbsp;15. In one prompt. Measurably.</b>
  </p>
</p>

<p align="center">
  <a href="https://github.com/baidakovil/metricsreporter/actions/workflows/ci.yml"><img alt="CI" src="https://github.com/baidakovil/metricsreporter/actions/workflows/ci.yml/badge.svg"></a>
  <a href="https://www.nuget.org/packages/MetricsReporter.Tool"><img alt="NuGet" src="https://img.shields.io/nuget/v/MetricsReporter.Tool.svg?logo=nuget"></a>
  <a href="https://github.com/baidakovil/metricsreporter/blob/master/LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-blue.svg"></a>
  <a href="https://dotnet.microsoft.com/"><img alt=".NET 8" src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet"></a>
  <a href="#ai-driven-refactoring"><img alt="AI-Ready" src="https://img.shields.io/badge/AI--driven-refactoring-ff6f00"></a>
</p>

---

**MetricsReporter** is a .NET 8 CLI tool that aggregates code coverage, complexity, coupling, and analyzer violations from **three independent sources** into one interactive dashboard — then lets you (or your AI agent) fix everything via a structured refactoring loop.

<p align="center">
  <img src="docs/images/dashboard_observing.png" alt="MetricsReporter Dashboard" width="100%">
  <br>
  <sub><a href="docs/images/dashboard_observing.gif">▶ Watch interactive dashboard demo (GIF)</a></sub>
</p>

## The problem

Your C# project has growing tech debt. You *feel* the code is getting worse, but:

- **Coverage data** lives in OpenCover XML, Roslyn metrics are in a separate XML, SARIF violations in yet another file
- **No single dashboard** shows coupling, complexity, coverage, and analyzer violations together
- **You can't measure** whether a refactoring actually helped
- **AI agents don't know** which method to fix first or whether the fix worked

## The solution

```
One prompt. Three sources. One dashboard. Measurable improvement.
```
Under the hood, MetricsReporter **parses three formats** and cross-links them by symbol name:

| Input | Format | What it extracts |
|-------|--------|-----------------|
| `dotnet-coverage` / AltCover | **OpenCover XML** | sequence & branch coverage, cyclomatic/NPath complexity per method |
| `dotnet-metrics` (Roslyn) | **Metrics XML** | maintainability index, cyclomatic complexity, class coupling, depth of inheritance, lines of code |
| Roslyn analyzers | **SARIF JSON** | CA / IDE rule violations with file paths and line numbers |

All three are merged into a **single JSON file** keyed by fully-qualified symbol. From that JSON, the tool renders a **self-contained HTML file** — one file, no server, no frameworks, pure JavaScript — that works offline and handles 50k+ symbols without pagination.

### vs SonarQube / NDepend

Tools like SonarQube and NDepend are powerful, but they require a server, a license, or a cloud account — and plugging them into an AI refactoring loop means dealing with APIs, tokens, and webhooks.

MetricsReporter installs in one command (`dotnet tool install --global MetricsReporter.Tool`), runs entirely on your machine, and outputs plain JSON to stdout. Any AI agent that can run a subprocess and read its output can drive the full refactor → verify loop — no API key, no SDK, no network required.


## Key Features

### AI-Driven Refactoring

Give any AI agent a namespace and a metric — Copilot, Cursor, Claude. It reads the violation, edits the code, rebuilds, and calls `test` to verify. metricsreporter only measures; the agent does the work.

```powershell
# AI agent asks: "what's broken?"
metricsreporter read --namespace MyApp.Services --metric Coupling --symbol-kind Any

# ↓ YOUR AI AGENT takes over: reads the source, edits files, runs `dotnet build`
# metricsreporter does NOT refactor anything — it only measures
# (this step is fully owned by Copilot, Cursor, or whichever agent you use)

# AI agent verifies the fix:
metricsreporter test --symbol MyApp.Services.OrderProcessor --metric Coupling
# → { "isOk": true }  ✅
```

Ready-to-use agent prompts ship in [`Metrics/Agent/`](Metrics/Agent/): [`refactor-complexity.md`](Metrics/Agent/refactor-complexity.md), [`refactor-coupling.md`](Metrics/Agent/refactor-coupling.md), [`refactor-coverage.md`](Metrics/Agent/refactor-coverage.md), [`refactor-sarif.md`](Metrics/Agent/refactor-sarif.md).

<p align="center">
  <img src="docs/images/prompt_to_refactor.png" alt="AI refactoring prompt">
  <br>
  <sub>Built-in refactoring prompts for complexity, coupling, and coverage — ready for Copilot, Cursor, or any AI agent</sub>
</p>

### Interactive HTML Dashboard

Drill down from Solution to Assembly to Namespace to Type to Method. Filter instantly, toggle warning/error awareness, hover for metric details. No frameworks — pure JS, handles 50k+ symbols.

Keyboard shortcuts: `F` focus filter · `Enter` apply · `X` clear · `N` new only · `C` changes only · `D`/`S` detail level · `A`/`Z` awareness level · `E`/`R` expand/collapse · `Q` reset all.


<p align="center">
  <img src="docs/images/statistics.png" alt="Statistics view">
  <br>
  <sub>Aggregate statistics at a glance — coverage %, complexity distribution, violation counts</sub>
</p>

### ReportGenerator Integration

[ReportGenerator](https://github.com/danielpalme/ReportGenerator) produces line-by-line HTML coverage reports. Point `--coverage-html-dir` at its output and the dashboard links directly to it.

MetricsReporter lets you configure shell scripts that run automatically on `generate`, `read` and `test` commands. This means your AI agent can trigger a full rebuild and coverage recollection as part of its verify step — no manual reruns needed. The typical AI-driven coverage loop looks like:

1. `metricsreporter read` — finds uncovered methods
2. AI agent writes NUnit tests
3. `metricsreporter test` — triggers the configured script: runs `dotnet test`, recollects OpenCover output, re-generates the report
4. CLI returns the updated coverage result — agent iterates until green

<p align="center">
  <img src="docs/images/reportgenerator.gif" alt="ReportGenerator coverage view" width="100%">
  <br>
  <sub>Line-by-line coverage maps powered by ReportGenerator, launched alongside MetricsReporter</sub>
</p>

### SARIF Violations with Breakdown

See exactly which CA/IDE rules fire at each level. Hover for rule descriptions, file paths, and line numbers.

<p align="center">
  <img src="docs/images/tooltip_on_hovering_sarif_violations.png" alt="SARIF violation tooltip" width="400">
  <br>
  <sub>Hover any SARIF metric to see rule-by-rule breakdown with source locations</sub>
</p>

### Suppression System

Not every violation should be fixed. Mark intentional exceptions with `[SuppressMessage]` — they show up in the dashboard with justifications, not as false alarms.

Two placement options, same effect:
- **Symbol-level** — attribute sits directly on the class or method.
- **Assembly-level** — attribute lives in `GlobalSuppressions.cs` with a `Target` pointing to any type or member (`~T:`, `~M:`, `~P:`, `~E:`, `~F:`). Useful when the justification applies to the whole type and you don't want to scatter attributes across the codebase.

Note: suppressions are not inherited — `~T:MyType` suppresses the type row only, not its members. Namespace-level suppression is not supported ([full reference](docs/3-reference/3.4%20-%20suppression-guidelines.md)).

<p align="center">
  <img src="docs/images/suppression_sample_code.png" alt="Suppression in code" width="100%">
  <br>
  <sub>Suppression attribute in code</sub>
</p>

<p align="center">
  <img src="docs/images/suppression_sample_dashboard.png" alt="Suppression in dashboard" width="100%">
  <br>
  <sub>Suppression reflected in dashboard with justification tooltip</sub>
</p>

### Baseline & Delta Tracking

Every run saves a baseline. Next run computes deltas automatically. You see whether complexity went up or down, whether coverage improved, whether new violations appeared — per method.

### Threshold Gates for CI

Define warning/error thresholds per metric per level. CLI exits with code `0` (pass) or non-zero (fail) — plug it straight into your pipeline.

```json
{
  "RoslynClassCoupling": {
    "Assembly":  { "warning": 60, "error": 120 },
    "Namespace": { "warning": 40, "error": 80  },
    "Type":      { "warning": 20, "error": 40  },
    "Member":    { "warning": 5,  "error": 11  }
  },
  "RoslynCyclomaticComplexity": {
    "Type":   { "warning": 15, "error": 100 },
    "Member": { "warning": 15, "error": 100 }
  },
  "OpenCoverBranchCoverage": {
    "Member": { "warning": 80, "error": 60, "higherIsBetter": true }
  }
}
```

### Smart Reconciliation

OpenCover assigns coverage to compiler-generated state machines (`<Method>d__0`); Roslyn provides no namespace data. MetricsReporter transfers iterator coverage back to real methods, infers namespaces, and removes duplicate noise ([details](docs/4-explanation/4.4%20-%20iterator-coverage-reconciliation.md)).

<p align="center">
  <img src="docs/images/hovering_on_include_state_machine.png" alt="State machine reconciliation tooltip" width="500">
  <br>
  <sub>Iterator state machine coverage automatically attributed to the real method</sub>
</p>

### Other Features

**Dashboard interactivity**
- **Filter box** — type any substring to instantly narrow the tree to matching symbols, rule IDs, or file paths
- **Copy symbol** — one click copies the fully-qualified name to the clipboard (ready to paste into `--symbol`)
- **Open in editor** — deep-link opens the source file at the exact line in your IDE
- **Awareness & detail sliders** — dial between All / Warning / Error rows and Solution / Namespace / Type / Member depth without rebuilding the DOM
- **State persists** — filter text, sliders, and expand/collapse state survive page refresh via `localStorage`

**Configuration & input**
- **Three-layer config with priority** — CLI flags override env vars (`METRICSREPORTER_*`), which override `.metricsreporter.json`, which override built-in defaults; mix freely
- **Config validation with exit code 3** — the config file is schema-validated before any command runs; unknown keys, ambiguous script routes, and duplicate aliases all produce clear errors
- **Metric aliases** — map long canonical names (`RoslynClassCoupling`) to short shorthands (`Coupling`, `cc`) in config, env vars, or `--metric-aliases`; aliases are embedded in the report and shown as column-header tooltips

→ [Configuration reference](docs/3-reference/3.1%20-%20configuration-options.md)

**Symbol filtering**
- **Wildcard exclusion patterns** — exclude members, types, and assemblies by glob patterns (`*b__*`, `Tests`, `<>c`); configured in JSON or via CLI flags
- **Member-kind toggles** — independently include/exclude methods, properties, fields, and events from the report; fields are excluded by default to reduce noise

**Suppression**
- **Symbol-level suppression** — place `[SuppressMessage]` directly on any type or member
- **Assembly-level suppression** — add `[assembly: SuppressMessage(..., Target = "~M:Namespace.Type.Method(...)")]` in `GlobalSuppressions.cs` to suppress without touching the source symbol
- Suppressed metrics appear in the dashboard with a badge and justification tooltip, not as false alarms; `--include-suppressed` exposes them in `read`/`test` output

**Baseline & history**
- **Automatic baseline rotation** — `replaceBaseline=true` archives the previous baseline with a timestamp and promotes the new report; `%LOCALAPPDATA%` expansion supported for baseline storage path
- **HTML-only re-render** — `generate --input-json report.json` re-renders the dashboard from an existing JSON without rerunning any tooling

**AltCover support**
- Full first-class support for [AltCover](https://github.com/SteveGilham/altcover) — both `dotnet-coverage` and AltCover produce OpenCover XML; AltCover-specific complexity metrics (`OpenCoverCyclomaticComplexity`, `OpenCoverNPathComplexity`) are included when present
- Dedicated agent prompt files for AltCover complexity refactoring ship in [`Metrics/Agent/`](Metrics/Agent/)
- Battle-tested in a production plugin project with a complex AltCover instrumentation pipeline

---

<h2 id="install">Quick Start</h2>

### Install

```powershell
dotnet tool install --global MetricsReporter.Tool
```

### Prerequisites for Roslyn metrics

MetricsReporter **reads** Roslyn metrics XML — it does not produce it on its own. You need `Metrics.exe` from the [`Microsoft.CodeAnalysis.Metrics`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Metrics) NuGet package to generate `SolutionMetrics.g.xml`.

**Platform note:** `Metrics.exe` is a native-framework binary and must match your OS/architecture. This repo ships a prebuilt `win-arm64` binary in [`build/Resources/metrics/win-arm64/`](build/Resources/metrics/win-arm64/) — it was compiled from source because no official prebuilt existed for that platform. If you are on **Windows x64**, you need to swap it out; see [`build/Resources/metrics/README.md`](build/Resources/metrics/README.md) for instructions (download from NuGet or build from source). Coverage and the HTML dashboard work on all platforms regardless.

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

Create `.metricsreporter.json` in your repo root ([configuration reference](docs/3-reference/3.1%20-%20configuration-options.md)):

```json
{
  "metricsDir": "Metrics",
  "openCover": ["Metrics/OpenCover/coverage.xml"],
  "roslyn": ["Metrics/Roslyn/SolutionMetrics.g.xml"],
  "sarif": ["Metrics/Sarif/ca.sarif"],
  "outputJson": "Metrics/MetricsReport.g.json",
  "outputHtml": "Metrics/MetricsReport.html",
  "thresholdsFile": "Metrics/MetricsRules/MetricsReporterThresholds.json"
}
```

Then just:

```powershell
metricsreporter generate
```

### Query metrics from CLI

```powershell
# Find first coupling violation in a namespace
metricsreporter read --namespace MyApp.Services --metric Coupling

# Verify a specific symbol passes thresholds
metricsreporter test --symbol MyApp.Services.OrderService.Process --metric Complexity

# List all coverage violations, grouped by type
metricsreporter read --namespace MyApp --metric OpenCoverBranchCoverage --group-by type
```

→ [Full CLI reference](docs/3-reference/3.2%20-%20metricsreporter-cli.md)

---

<h2 id="ai-driven-refactoring">AI Agent Workflow</h2>

MetricsReporter ships with ready-to-use prompt files for AI agents:

| Prompt | What the agent does |
|--------|-------------------|
| [`refactor-complexity.md`](Metrics/Agent/refactor-complexity.md) | Reduce cyclomatic complexity below thresholds |
| [`refactor-coupling.md`](Metrics/Agent/refactor-coupling.md) | Reduce class coupling with DI, interfaces, DTOs |
| [`refactor-coverage.md`](Metrics/Agent/refactor-coverage.md) | Write tests until branch coverage passes |
| [`refactor-sarif.md`](Metrics/Agent/refactor-sarif.md) | Fix CA/IDE analyzer violations |

**The agent loop is simple:**

1. `metricsreporter read` — find violation
2. Refactor — apply changes
3. `metricsreporter test` — verify fix
4. Repeat until clean

The agent runs this loop autonomously — no human input needed between steps.

---

## Metrics Sources

| Source | Metrics |
|--------|---------|
| **OpenCover** | Sequence Coverage, Branch Coverage, Cyclomatic Complexity, NPath Complexity |
| **Roslyn** | Maintainability Index, Cyclomatic Complexity, Class Coupling, Depth of Inheritance, Lines of Code |
| **SARIF** | CA-prefix (FxCop), IDE-prefix analyzer violations with rule-level breakdown |

---

## Documentation

[Documentation](docs/README.md):

- **[Tutorials](docs/1-tutorials/1.0%20-%20README.md)** — Get your first dashboard running
- **[How-To Guides](docs/2-how-to-guides/2.0%20-%20README.md)** — Config files, scripts, shipping updates
- **[Reference](docs/3-reference/3.0%20-%20README.md)** — CLI commands, config schema, report format, suppressions
- **[Explanation](docs/4-explanation/4.0%20-%20README.md)** — Architecture, coverage pipeline, namespace inference

---

## Contributing

```powershell
git clone https://github.com/baidakovil/metricsreporter.git
cd metricsreporter
dotnet restore && dotnet build && dotnet test
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for code style, branch workflow, and PR guidelines.

## License

[MIT](LICENSE)

