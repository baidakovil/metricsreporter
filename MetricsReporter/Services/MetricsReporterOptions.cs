namespace MetricsReporter.Services;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Describes command-line options supplied to the metrics reporter.
/// </summary>
public sealed class MetricsReporterOptions
{
  /// <summary>
  /// Solution name displayed in the report.
  /// </summary>
  public string SolutionName { get; init; } = "Solution";

  /// <summary>
  /// Paths to AltCover/OpenCover coverage XML files.
  /// </summary>
  public IReadOnlyCollection<string> AltCoverPaths { get; init; } = [];

  /// <summary>
  /// Paths to Roslyn code metrics XML reports.
  /// </summary>
  public IReadOnlyCollection<string> RoslynPaths { get; init; } = [];

  /// <summary>
  /// Paths to SARIF files.
  /// </summary>
  public IReadOnlyCollection<string> SarifPaths { get; init; } = [];

  /// <summary>
  /// Path to the baseline JSON file.
  /// </summary>
  public string? BaselinePath { get; init; }

  /// <summary>
  /// Optional textual descriptor for the baseline (commit hash, build identifier, etc.).
  /// </summary>
  public string? BaselineReference { get; init; }

  /// <summary>
  /// Threshold values encoded as JSON.
  /// </summary>
  public string? ThresholdsJson { get; init; }

  /// <summary>
  /// Optional path to a JSON file that contains threshold definitions.
  /// When specified, overrides <see cref="ThresholdsJson"/>.
  /// </summary>
  public string? ThresholdsPath { get; init; }

  /// <summary>
  /// Path to an existing JSON report to load (for HTML generation only).
  /// </summary>
  public string? InputJsonPath { get; init; }

  /// <summary>
  /// Path to the generated JSON report.
  /// </summary>
  public string OutputJsonPath { get; init; } = string.Empty;

  /// <summary>
  /// Path to the generated HTML report.
  /// </summary>
  public string OutputHtmlPath { get; init; } = string.Empty;

  /// <summary>
  /// Metrics directory (MetricsDir).
  /// </summary>
  public string MetricsDirectory { get; init; } = string.Empty;

  /// <summary>
  /// Path to the metrics reporter log file.
  /// </summary>
  public string LogFilePath { get; init; } = string.Empty;

  /// <summary>
  /// Verbosity value controlling minimum log level (quiet|minimal|normal|detailed).
  /// </summary>
  public string Verbosity { get; init; } = "normal";

  /// <summary>
  /// Comma-separated or semicolon-separated list of member name patterns to exclude from metrics reports.
  /// </summary>
  /// <remarks>
  /// Patterns support '*' and '?' wildcards. Method names are normalized when evaluated
  /// (leading dots are removed). Plain names without wildcards are treated as exact matches.
  /// If not specified, default excluded methods are used (constructors and compiler-generated methods).
  /// </remarks>
  public string? ExcludedMemberNamesPatterns { get; init; }

  /// <summary>
  /// Exclude methods from metrics.
  /// </summary>
  public bool ExcludeMethods { get; init; }

  /// <summary>
  /// Exclude properties from metrics.
  /// </summary>
  public bool ExcludeProperties { get; init; }

  /// <summary>
  /// Exclude fields from metrics.
  /// </summary>
  public bool ExcludeFields { get; init; }

  /// <summary>
  /// Exclude events from metrics.
  /// </summary>
  public bool ExcludeEvents { get; init; }

  /// <summary>
  /// Comma-separated or semicolon-separated list of assembly name patterns to exclude from metrics reports.
  /// </summary>
  /// <remarks>
  /// Assembly names containing any of the specified patterns will be excluded. Matching is case-insensitive.
  /// Examples: "Tests,Test" or "Tests;Test". If not specified, no assemblies are excluded.
  /// </remarks>
  public string? ExcludedAssemblyNames { get; init; }

  /// <summary>
  /// Comma-separated or semicolon-separated list of type name patterns to exclude from metrics reports.
  /// </summary>
  /// <remarks>
  /// Patterns are matched against fully qualified type names using substring matching (case-sensitive).
  /// Examples: "&lt;&gt;c,__DisplayClass". If not specified, no types are excluded via this filter.
  /// </remarks>
  public string? ExcludedTypeNamePatterns { get; init; }

  /// <summary>
  /// When <see langword="true"/>, automatically replaces the baseline file if the new report differs from the existing baseline.
  /// </summary>
  /// <remarks>
  /// If enabled, the application will compare the new metrics-report.json with the existing metrics-baseline.json.
  /// If they differ, the old baseline will be archived to <see cref="MetricsReportStoragePath"/> with a timestamp,
  /// and the new report will become the new baseline before generating the final report with deltas.
  /// </remarks>
  public bool ReplaceMetricsBaseline { get; init; }

  /// <summary>
  /// Directory path where old baseline files are archived with timestamps when baseline replacement is enabled.
  /// </summary>
  /// <remarks>
  /// When baseline replacement occurs, the old baseline file is moved to this directory with a timestamp suffix
  /// to ensure unique filenames. Defaults to <c>C:\Users\&lt;username&gt;\AppData\Local\MetricsReporter\Metrics</c>.
  /// </remarks>
  public string? MetricsReportStoragePath { get; init; }

  /// <summary>
  /// Directory path to HTML coverage reports generated by reportgenerator.
  /// Used to generate hyperlinks from metrics report to coverage HTML files.
  /// </summary>
  /// <remarks>
  /// If specified, member nodes in the HTML report will include links to corresponding
  /// coverage HTML files in this directory. Links are only created if the target HTML file exists.
  /// </remarks>
  public string? CoverageHtmlDir { get; init; }

  /// <summary>
  /// When <see langword="true"/>, performs an additional Roslyn-based scan of the solution
  /// to discover <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/> usages
  /// before aggregating metrics.
  /// </summary>
  /// <remarks>
  /// The scan is limited to <c>.cs</c> files under <see cref="SolutionDirectory"/> and respects
  /// <see cref="ExcludedAssemblyNames"/> by skipping source roots whose assembly names match
  /// any of the configured exclusion patterns. Results are written to
  /// <see cref="SuppressedSymbolsPath"/> and also embedded into the final <c>metrics-report.json</c>
  /// so that the HTML dashboard can visually distinguish suppressed metrics.
  /// </remarks>
  public bool AnalyzeSuppressedSymbols { get; init; }

  /// <summary>
  /// Optional path to the JSON file where suppressed symbol metadata is stored.
  /// </summary>
  /// <remarks>
  /// When <see cref="AnalyzeSuppressedSymbols"/> is enabled, the application will generate
  /// <c>SuppressedSymbols.json</c> at this path. During aggregation the same file is read
  /// back and its contents are embedded into the report metadata to make the information
  /// available for HTML rendering and external tooling.
  /// </remarks>
  public string? SuppressedSymbolsPath { get; init; }

  /// <summary>
  /// Root directory of the solution source tree used for suppressed symbol analysis.
  /// </summary>
  /// <remarks>
  /// When <see cref="AnalyzeSuppressedSymbols"/> is enabled, this directory is used as
  /// the base for resolving paths in <see cref="SourceCodeFolders"/>. The scanner derives
  /// a logical assembly name from the folder structure relative to the source code folders
  /// and applies <see cref="ExcludedAssemblyNames"/> filters before processing files.
  /// </remarks>
  public string? SolutionDirectory { get; init; }

  /// <summary>
  /// List of source code folder paths (relative to <see cref="SolutionDirectory"/>)
  /// that contain assembly projects to scan for suppressed symbols.
  /// </summary>
  /// <remarks>
  /// Each folder in this list is scanned recursively for <c>.cs</c> files. The assembly name
  /// is derived from the first segment after the source code folder in the file path.
  /// For example, if <c>SourceCodeFolders = ["src", "src/Tools", "tests"]</c> and a file
  /// is located at <c>src/Sample.TestAdapter/File.cs</c>, the assembly name is <c>Sample.TestAdapter</c>.
  /// For <c>src/Tools/MetricsReporter/File.cs</c>, the assembly name is <c>MetricsReporter</c>
  /// because <c>src/Tools</c> is matched first (longest prefix match).
  /// </remarks>
  /// <example>
  /// Typical values: <c>["src", "src/Tools", "tests"]</c> or <c>["Source", "Tests"]</c>
  /// </example>
  public IReadOnlyCollection<string> SourceCodeFolders { get; init; } = [];

  /// <summary>
  /// Optional metric alias mappings keyed by canonical metric identifier.
  /// </summary>
  public IReadOnlyDictionary<MetricIdentifier, IReadOnlyList<string>> MetricAliases { get; init; }
    = new Dictionary<MetricIdentifier, IReadOnlyList<string>>();
}


