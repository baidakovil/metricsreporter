# metricsreporter configuration, options, and precedence

## Sources and precedence

- CLI options override everything.
- Environment variables (`METRICSREPORTER_*`) override config file.
- Config file (`.metricsreporter.json`, discovered by walking up from the working directory unless `--config` overrides) overrides defaults.
- Defaults (only general values): `verbosity=normal`, `timeout=900s`, `logTruncationLimit=4000`, `workingDirectory=current directory`.

## Sections (JSON Schema: `src/MetricsReporter/Configuration/metricsreporter-config.schema.json`)

- `general`
  - `verbosity` (`quiet|minimal|normal|detailed`, default `normal`)
  - `timeoutSeconds` (seconds, default `900`)
  - `workingDirectory` (string, no default)
  - `logTruncationLimit` (int, default `4000`)
- `paths` (no defaults; missing required paths are validation errors)
  - `metricsDir`, `solutionName`, `baselineReference`
  - `report` (output JSON for `generate`)
  - `readReport` (input JSON for `read`/`readsarif`/`test`)
  - `thresholds` (file path), `thresholdsInline` (inline JSON)
  - `altcover[]`, `roslyn[]`, `sarif[]` (arrays of strings)
  - `baseline`, `outputHtml`, `inputJson`
  - `coverageHtmlDir`, `baselineStoragePath`
  - `suppressedSymbols`, `solutionDirectory`, `sourceCodeFolders[]`
  - `excludedMembers`, `excludedAssemblies`, `excludedTypes`
  - `analyzeSuppressedSymbols` (bool), `replaceBaseline` (bool)
- `scripts`
  - `generate[]` (PowerShell scripts run before aggregation)
  - `read.any[]` (scripts always run before read/readsarif/test)
  - `read.byMetric[]` (array of `{ metrics: [<metricId>...], path: "<script.ps1>" }`; script runs when any requested metric matches)

## Environment variables (examples)

- General: `METRICSREPORTER_VERBOSITY`, `METRICSREPORTER_TIMEOUT_SECONDS`, `METRICSREPORTER_WORKING_DIRECTORY`, `METRICSREPORTER_LOG_TRUNCATION_LIMIT`
- Paths: `METRICSREPORTER_PATHS_METRICS_DIR`, `..._SOLUTION_NAME`, `..._BASELINE_REF`, `..._REPORT`, `..._READ_REPORT`, `..._THRESHOLDS`, `..._THRESHOLDS_INLINE`, `..._ALTCOVER`, `..._ROSLYN`, `..._SARIF`, `..._BASELINE`, `..._OUTPUT_HTML`, `..._INPUT_JSON`, `..._COVERAGE_HTML_DIR`, `..._BASELINE_STORAGE_PATH`, `..._SUPPRESSED_SYMBOLS`, `..._SOLUTION_DIRECTORY`, `..._SOURCE_CODE_FOLDERS`, `..._EXCLUDED_MEMBERS`, `..._EXCLUDED_ASSEMBLIES`, `..._EXCLUDED_TYPES`, `..._ANALYZE_SUPPRESSED_SYMBOLS`, `..._REPLACE_BASELINE`
- Scripts: `METRICSREPORTER_SCRIPTS_GENERATE`, `METRICSREPORTER_SCRIPTS_READ_ANY`, `METRICSREPORTER_SCRIPTS_READ_BYMETRIC` (semicolon-separated `metric1,metric2:script.ps1` entries)

## CLI → config mapping (generate)

- Paths: `--metrics-dir`, `--solution-name`, `--baseline-ref`, `--baseline`, `--output-json`, `--output-html`, `--thresholds`, `--thresholds-file`, `--input-json`, `--baseline-storage-path`, `--coverage-html-dir`, `--suppressed-symbols`, `--solution-dir`, `--source-code-folders`
- Inputs: `--altcover`, `--roslyn`, `--sarif`
- Filters/flags: `--excluded-members`, `--excluded-assemblies`, `--excluded-types`, `--replace-baseline`, `--analyze-suppressed-symbols`
- Scripts: `--script` (maps to `scripts.generate`)

## CLI → config mapping (read / readsarif / test)

- Report path: `--report` (maps to `paths.readReport` when absent)
- Threshold overrides: `--thresholds-file`
- Filters: `--include-suppressed`, `--group-by`, `--symbol-kind`, `--ruleid` (readsarif/read), `--all`
- Metric-specific scripts: `--script` (maps to `scripts.read.any`), `--metric-script <Metric=Path>` (maps to `scripts.read.byMetric`)

## Sample `.metricsreporter.json` (paths pulled from `c:\Users\baidakov\rca-plugin\build\Props\paths.props`)

```jsonc
{
  "general": {
    "verbosity": "normal",
    "timeoutSeconds": 900,
    "logTruncationLimit": 4000
  },
  "paths": {
    "metricsDir": "build/Metrics",
    "report": "build/Metrics/Report/MetricsReport.g.json",
    "readReport": "build/Metrics/Report/MetricsReport.g.json",
    "baseline": "build/MetricsTemp/MetricsBaseline.g.json",
    "thresholds": "build/MetricsRules/MetricsReporterThresholds.json",
    "outputHtml": "build/Metrics/Report/MetricsReport.html",
    "roslyn": [ "build/Metrics/Roslyn/SolutionMetrics.g.xml" ],
    "sarif": [ "build/Metrics/Sarif/*.sarif" ],
    "altcover": [ "build/MetricsTemp/CoverageStorage.g.xml" ],
    "coverageHtmlDir": "build/Metrics/AltCover/html",
    "suppressedSymbols": "build/MetricsTemp/RcaSuppressedSymbols.g.json",
    "baselineStoragePath": "%LOCALAPPDATA%/RCA/Metrics",
    "solutionName": "rca-plugin"
  },
  "scripts": {
    "generate": [ "scripts/prepare-metrics.ps1" ],
    "read": {
      "any": [ "scripts/refresh-report.ps1" ],
      "byMetric": [
        { "metrics": [ "AltCoverBranchCoverage", "AltCoverSequenceCoverage" ], "path": "scripts/coverage.ps1" },
        { "metrics": [ "SarifCaRuleViolations", "SarifIdeRuleViolations" ], "path": "scripts/readsarif.ps1" }
      ]
    }
  }
}
```

## Quick CLI examples

- Generate from inputs:  
  `metricsreporter generate --metrics-dir build/Metrics --altcover build/MetricsTemp/CoverageStorage.g.xml --roslyn build/Metrics/Roslyn/SolutionMetrics.g.xml --sarif build/Metrics/Sarif/ca.sarif --output-json build/Metrics/Report/MetricsReport.g.json --output-html build/Metrics/Report/MetricsReport.html`
- HTML from existing JSON:  
  `metricsreporter generate --input-json build/Metrics/Report/MetricsReport.g.json --output-html build/Metrics/Report/MetricsReport.html`
- Read a metric:  
  `metricsreporter read --report build/Metrics/Report/MetricsReport.g.json --namespace Sample.Loader --metric Complexity --all`
- Read SARIF:  
  `metricsreporter readsarif --report build/Metrics/Report/MetricsReport.g.json --namespace Sample.Loader --metric SarifIdeRuleViolations --all`
- Test a symbol:  
  `metricsreporter test --report build/Metrics/Report/MetricsReport.g.json --symbol Sample.Loader.Type.Method --metric Complexity`
## Configuration and option mapping

- Sources (priority): CLI > config file (`.metricsreporter.json`) > built-in defaults (general section only). Environment variables are intentionally **not** used to keep the flow simple and deterministic.
- Config search: if `--config` is not provided, the tool walks parent directories from the working directory to find `.metricsreporter.json`.
- Working directory default: current process directory.

### JSON schema (located at `src/MetricsReporter/Configuration/metricsreporter-config.schema.json`)

```json
{
  "general": {
    "verbosity": "Information | Warning | Error",
    "timeoutSeconds": 900,
    "workingDirectory": "C:\\repo",
    "logTruncationLimit": 4000
  },
  "paths": {
    "metricsDir": "C:\\repo\\build\\Metrics",
    "report": "C:\\repo\\build\\Metrics\\Report\\MetricsReport.g.json",
    "outputJson": "C:\\repo\\build\\Metrics\\Report\\MetricsReport.g.json",
    "outputHtml": "C:\\repo\\build\\Metrics\\Report\\MetricsReport.html",
    "thresholds": "{...json...}",
    "thresholdsFile": "C:\\repo\\build\\MetricsRules\\MetricsReporterThresholds.json",
    "baseline": "C:\\repo\\build\\MetricsTemp\\MetricsBaseline.g.json",
    "baselineRef": "refs/heads/main",
    "suppressedSymbols": "C:\\repo\\build\\MetricsTemp\\RcaSuppressedSymbols.g.json",
    "metricsReportStoragePath": "C:\\Users\\<user>\\AppData\\Local\\RCA\\Metrics",
    "coverageHtmlDir": "C:\\repo\\build\\Metrics\\AltCover\\html",
    "solutionDir": "C:\\repo",
    "altcover": ["C:\\repo\\build\\Metrics\\AltCover\\CoverageTemplate.g.xml"],
    "roslyn": ["C:\\repo\\build\\Metrics\\Roslyn\\SolutionMetrics.g.xml"],
    "sarif": ["C:\\repo\\build\\Metrics\\Sarif\\violations.sarif"],
    "sourceCodeFolders": ["src", "src/Tools", "tests"]
  },
  "scripts": {
    "generate": [
      ".scripts\\prepare-metrics.ps1"
    ],
    "read": {
      "any": [
        ".scripts\\prepare-read.ps1"
      ],
      "byMetric": [
        {
          "path": ".scripts\\coverage-read.ps1",
          "metrics": ["AltCoverBranchCoverage", "AltCoverLineCoverage"]
        }
      ]
    }
  }
}
```

### CLI → C# options

| CLI | C# destination | Notes |
| --- | --- | --- |
| `--metrics-dir` | `MetricsReporterOptions.MetricsDirectory` | Required for generate when `--input-json` is absent |
| `--output-json` | `MetricsReporterOptions.OutputJsonPath` | Required for generate when `--input-json` is absent |
| `--output-html` | `MetricsReporterOptions.OutputHtmlPath` | Required when `--input-json` is used |
| `--altcover` | `MetricsReporterOptions.AltCoverPaths` | Repeatable; overrides config list |
| `--roslyn` | `MetricsReporterOptions.RoslynPaths` | Repeatable; overrides config list |
| `--sarif` | `MetricsReporterOptions.SarifPaths` | Repeatable; overrides config list |
| `--baseline` | `MetricsReporterOptions.BaselinePath` | |
| `--baseline-ref` | `MetricsReporterOptions.BaselineReference` | |
| `--thresholds` | `MetricsReporterOptions.ThresholdsJson` | |
| `--thresholds-file` | `MetricsReporterOptions.ThresholdsPath` | |
| `--input-json` | `MetricsReporterOptions.InputJsonPath` | Triggers HTML-only mode |
| `--analyze-suppressed-symbols` | `MetricsReporterOptions.AnalyzeSuppressedSymbols` | |
| `--suppressed-symbols` | `MetricsReporterOptions.SuppressedSymbolsPath` | |
| `--solution-dir` | `MetricsReporterOptions.SolutionDirectory` | |
| `--source-code-folders` | `MetricsReporterOptions.SourceCodeFolders` | Comma/semicolon split |
| `--coverage-html-dir` | `MetricsReporterOptions.CoverageHtmlDir` | |
| `--replace-baseline` | `MetricsReporterOptions.ReplaceMetricsBaseline` | |
| `--baseline-storage-path` | `MetricsReporterOptions.MetricsReportStoragePath` | |
| `--excluded-members` | `MetricsReporterOptions.ExcludedMemberNamesPatterns` | |
| `--excluded-assemblies` | `MetricsReporterOptions.ExcludedAssemblyNames` | |
| `--excluded-types` | `MetricsReporterOptions.ExcludedTypeNamePatterns` | |
| `--report` (read/readsarif/test) | `ResolvedPathConfiguration.Report` | Required for read flows |
| `--thresholds-file` (read/readsarif/test) | `ResolvedPathConfiguration.ThresholdsFile` | Optional override |

### Precedence behavior

- CLI always wins over config file.
- Config file values are used when CLI leaves an option unset.
- Defaults are applied only to `general` (verbosity, timeout, workingDirectory, logTruncationLimit). All path values must come from CLI or config; missing required paths produce a validation error.

### Sample CLI invocations

```pwsh
dotnet metricsreporter generate `
  --config .metricsreporter.json `
  --metrics-dir build/Metrics `
  --output-json build/Metrics/Report/MetricsReport.g.json

dotnet metricsreporter read `
  --config .metricsreporter.json `
  --report build/Metrics/Report/MetricsReport.g.json `
  --namespace Sample.Loader `
  --metric Coupling

dotnet metricsreporter readsarif `
  --report build/Metrics/Report/MetricsReport.g.json `
  --namespace Sample.Loader `
  --metric Any `
  --ruleid CA1506

dotnet metricsreporter test `
  --report build/Metrics/Report/MetricsReport.g.json `
  --symbol Sample.Loader.SomeType.SomeMethod `
  --metric Complexity
```

