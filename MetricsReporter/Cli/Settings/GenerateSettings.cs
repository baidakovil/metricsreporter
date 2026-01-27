using System.Collections.Generic;
using System.ComponentModel;
using Spectre.Console.Cli;

namespace MetricsReporter.Cli.Settings;

/// <summary>
/// Settings for the generate command.
/// </summary>
internal sealed class GenerateSettings : CliSettingsBase
{
  [CommandOption("--solution-name <NAME>")]
  [Description("Solution name displayed in reports.")]
  public string? SolutionName { get; init; }

  [CommandOption("--metrics-dir <PATH>")]
  [Description("Root directory for metrics artifacts (OpenCover/Roslyn/SARIF/baseline).")]
  public string? MetricsDir { get; init; }

  [CommandOption("--opencover <PATH>")]
  [Description("Path to OpenCover coverage XML. Repeat for multiple files.")]
  public List<string> OpenCover { get; init; } = [];

  [CommandOption("--roslyn <PATH>")]
  [Description("Path to Roslyn metrics XML. Repeat for multiple files.")]
  public List<string> Roslyn { get; init; } = [];

  [CommandOption("--sarif <PATH>")]
  [Description("Path to SARIF file. Repeat for multiple files.")]
  public List<string> Sarif { get; init; } = [];

  [CommandOption("--baseline <PATH>")]
  [Description("Path to baseline metrics JSON.")]
  public string? Baseline { get; init; }

  [CommandOption("--baseline-ref <TEXT>")]
  [Description("Baseline reference label (git commit, build ID, etc.).")]
  public string? BaselineReference { get; init; }

  [CommandOption("--output-json <PATH>")]
  [Description("Path to the resulting metrics-report.json.")]
  public string? OutputJson { get; init; }

  [CommandOption("--output-html <PATH>")]
  [Description("Path to the resulting metrics-report.html.")]
  public string? OutputHtml { get; init; }

  [CommandOption("--thresholds <JSON>")]
  [Description("Inline JSON string with metric thresholds.")]
  public string? Thresholds { get; init; }

  [CommandOption("--thresholds-file <PATH>")]
  [Description("Path to JSON file with symbol-level thresholds.")]
  public string? ThresholdsFile { get; init; }

  [CommandOption("--input-json <PATH>")]
  [Description("Existing metrics-report.json used to generate HTML only.")]
  public string? InputJson { get; init; }

  [CommandOption("--excluded-members <LIST>")]
  [Description("Comma/semicolon-separated member name patterns to exclude.")]
  public string? ExcludedMembers { get; init; }

  [CommandOption("--exclude-methods")]
  [Description("Exclude methods from metrics output.")]
  public bool ExcludeMethods { get; init; }

  [CommandOption("--exclude-properties")]
  [Description("Exclude properties from metrics output.")]
  public bool ExcludeProperties { get; init; }

  [CommandOption("--exclude-fields")]
  [Description("Exclude fields from metrics output.")]
  public bool ExcludeFields { get; init; }

  [CommandOption("--exclude-events")]
  [Description("Exclude events from metrics output.")]
  public bool ExcludeEvents { get; init; }

  [CommandOption("--excluded-assemblies <LIST>")]
  [Description("Comma/semicolon-separated assembly name patterns to exclude.")]
  public string? ExcludedAssemblies { get; init; }

  [CommandOption("--excluded-types <LIST>")]
  [Description("Comma/semicolon-separated type name patterns to exclude.")]
  public string? ExcludedTypes { get; init; }

  [CommandOption("--replace-baseline")]
  [Description("Replace baseline when new report differs.")]
  public bool ReplaceBaseline { get; init; }

  [CommandOption("--baseline-storage-path <PATH>")]
  [Description("Directory where old baseline files are archived.")]
  public string? BaselineStoragePath { get; init; }

  [CommandOption("--coverage-html-dir <PATH>")]
  [Description("Directory containing HTML coverage reports for hyperlinking.")]
  public string? CoverageHtmlDir { get; init; }

  [CommandOption("--analyze-suppressed-symbols")]
  [Description("Enable suppressed-symbol analysis before aggregation.")]
  public bool AnalyzeSuppressedSymbols { get; init; }

  [CommandOption("--suppressed-symbols <PATH>")]
  [Description("Path to JSON file where suppressed symbol metadata is stored.")]
  public string? SuppressedSymbols { get; init; }

  [CommandOption("--solution-dir <PATH>")]
  [Description("Solution directory used for suppressed-symbol analysis.")]
  public string? SolutionDirectory { get; init; }

  [CommandOption("--source-code-folders <LIST>")]
  [Description("Comma/semicolon-separated list of source code folder roots for suppressed-symbol analysis.")]
  public string? SourceCodeFolders { get; init; }

  [CommandOption("--script <PATH>")]
  [Description("PowerShell script executed before aggregation. Repeat for multiple scripts.")]
  public List<string> Scripts { get; init; } = [];
}

