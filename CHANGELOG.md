# Changelog

All notable changes to MetricsReporter are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [0.4.4] - 2026-04-01

### Fixed

- Config validation now correctly allows the `editorPrefix` option in the `general` section.
- HTML generation now propagates `general.editorPrefix` through the full generate pipeline, including HTML-only generation and post-script aggregation.
- Source-link rows in the HTML report now emit the configured editor protocol instead of falling back to `vscode://file/` when `editorPrefix` is set.

## [0.4.2] - 2026-03-29

### Added

- **Customizable editor links** — you can now set any URL prefix for opening files from the report (e.g., `cursor://`, `vscode://file/`) using the `general.editorPrefix` setting in the config. The default is `vscode://file/`.

- **NUGET_README.md** (NuGet readme) aligned with the main README.

- **HTTP/HTTPS ReportGenerator support** — the `CoverageLinkBuilder` now allows specifying an `http://` or `https://` URL in the `coverageHtmlDir` config setting. This enables direct linking to remote or locally served HTML coverage reports without requiring local file existence checks.

### Fixed

- Fix: `coverageHtmlDir` passed to HTML-only generation path to ensure coverage links are rendered when using HTTP/HTTPS URLs.
- Fix: Configuration warning for `coverageHtmlDir` no longer appears for HTTP/HTTPS URLs.

- **Tool version** is now shown dynamically in the HTML report under the spoiler in the `Tool info` section.

## [0.4.0] - 2026-03-15

### Added

- **AI-agent refactoring prompts** — ready-to-use prompt files for complexity, coupling, coverage, and SARIF violation reduction (`Metrics/Agent/refactor-*.md`).
- **Interactive HTML dashboard** — drill-down explorer with filter box, detail slider, awareness slider, suppression overlays, and localStorage persistence.
- **Three-source aggregation** — merges OpenCover (coverage), Roslyn (complexity, coupling, maintainability), and SARIF (analyzer violations) into a single canonical report.
- **CLI query engine** — `generate`, `read`, `readsarif`, and `test` commands with structured JSON output and CI-friendly exit codes.
- **Threshold-based gating** — warning/error thresholds per metric per symbol level (Solution, Assembly, Namespace, Type, Member).
- **Baseline & delta tracking** — automatic baseline rotation with per-metric delta computation across runs.
- **Suppression system** — `[SuppressMessage]` attribute support with justification tracking in dashboard and JSON output.
- **Member-kind filtering** — exclude methods, properties, fields, or events from reports; accessor patterns (`get_*`, `set_*`) filtered at parse time.
- **Iterator state machine reconciliation** — coverage from compiler-generated `<Method>d__0` types transferred back to real methods.
- **Plain nested type reconciliation** — coverage metrics for non-iterator nested types correctly attributed.
- **Namespace inference** — OpenCover's missing namespace data reconstructed via longest-prefix matching from Roslyn.
- **PowerShell script hooks** — per-command and per-metric script execution with timeout and failure handling.
- **ReportGenerator integration** — HTML coverage reports generated alongside metrics dashboard.
- **Metric aliases** — friendly names (`Coupling`, `Complexity`, `BranchCoverage`) for canonical metric identifiers.
- **Config-driven mode** — `.metricsreporter.json` auto-discovery for zero-flag operation.
- **Diataxis documentation** — tutorials, how-to guides, reference, and explanation docs.

### Infrastructure

- .NET 8 target framework with nullable reference types and latest C# language version.
- Spectre.Console.Cli for command parsing.
- NUnit + FluentAssertions + NSubstitute test suite.
- GitHub Actions CI workflow.

[0.4.2]: https://github.com/baidakovil/metricsreporter/releases/tag/v0.4.2
[0.4.4]: https://github.com/baidakovil/metricsreporter/releases/tag/v0.4.4
[0.4.0]: https://github.com/baidakovil/metricsreporter/releases/tag/v0.4.0
