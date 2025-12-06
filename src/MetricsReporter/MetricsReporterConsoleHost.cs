using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Services;

namespace MetricsReporter;

internal sealed class MetricsReporterConsoleHost
{
  private readonly TextWriter _outputWriter;

  public MetricsReporterConsoleHost(TextWriter outputWriter)
  {
    _outputWriter = outputWriter ?? throw new ArgumentNullException(nameof(outputWriter));
  }

  public static async Task<int> ExecuteAsync(string[] args)
  {
    using var cts = new CancellationTokenSource();
    ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
    {
      eventArgs.Cancel = true;
      cts.Cancel();
    };

    Console.CancelKeyPress += cancelHandler;
    try
    {
      var host = new MetricsReporterConsoleHost(Console.Out);
      var exitCode = await host.RunAsync(args, cts.Token).ConfigureAwait(false);
      return (int)exitCode;
    }
    catch (OperationCanceledException)
    {
      Console.Error.WriteLine("Operation was cancelled.");
      return (int)MetricsReporterExitCode.ValidationError;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Error: {ex.Message}");
      return (int)MetricsReporterExitCode.ValidationError;
    }
    finally
    {
      Console.CancelKeyPress -= cancelHandler;
    }
  }

  public async Task<MetricsReporterExitCode> RunAsync(string[] args, CancellationToken cancellationToken)
  {
    if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
    {
      PrintUsage();
      return MetricsReporterExitCode.Success;
    }

    var options = ParseArguments(args);
    var application = new MetricsReporterApplication();
    return await application.RunAsync(options, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Parses raw command-line arguments into strongly-typed <see cref="MetricsReporterOptions"/>.
  /// </summary>
  /// <param name="args">The raw <see cref="string"/> array passed to the process.</param>
  /// <returns>Populated <see cref="MetricsReporterOptions"/> instance.</returns>
  /// <remarks>
  /// This method is <see langword="internal"/> to allow tests in <c>MetricsReporter.Tests</c>
  /// to verify CLI-to-options binding (e.g., <c>--replace-baseline</c> →
  /// <see cref="MetricsReporterOptions.ReplaceMetricsBaseline"/>).
  /// Because it does not depend on instance state, it is implemented as a static helper.
  /// </remarks>
  internal static MetricsReporterOptions ParseArguments(string[] args)
  {
    var parserState = new ArgumentParserState();
    ProcessArguments(args, parserState);
    ValidateArguments(parserState);
    return CreateOptions(parserState);
  }

  private static void ProcessArguments(string[] args, ArgumentParserState state)
  {
    for (var index = 0; index < args.Length; index++)
    {
      var argument = args[index];
      if (!TryProcessArgument(argument, args, ref index, state))
      {
        throw new ArgumentException($"Unknown argument '{argument}'. Use --help to view usage.", argument);
      }
    }
  }

  private static bool TryProcessArgument(string argument, string[] args, ref int index, ArgumentParserState state)
  {
    return argument switch
    {
      "--solution-name" => TrySetValue(args, ref index, argument, value => state.SolutionName = value),
      "--metrics-dir" => TrySetValue(args, ref index, argument, value => state.MetricsDir = value),
      "--altcover" => TrySetValue(args, ref index, argument, value => state.AltCoverPaths.Add(value)),
      "--roslyn" => TrySetValue(args, ref index, argument, value => state.RoslynPaths.Add(value)),
      "--sarif" => TrySetValue(args, ref index, argument, value => state.SarifPaths.Add(value)),
      "--baseline" => TrySetValue(args, ref index, argument, value => state.BaselinePath = value),
      "--baseline-ref" => TrySetValue(args, ref index, argument, value => state.BaselineRef = value),
      "--output-json" => TrySetValue(args, ref index, argument, value => state.OutputJson = value),
      "--output-html" => TrySetValue(args, ref index, argument, value => state.OutputHtml = value),
      "--thresholds" => TrySetValue(args, ref index, argument, value => state.Thresholds = value),
      "--thresholds-file" => TrySetValue(args, ref index, argument, value => state.ThresholdsFile = value),
      "--input-json" => TrySetValue(args, ref index, argument, value => state.InputJson = value),
      "--excluded-members" => TrySetValue(args, ref index, argument, value => state.ExcludedMemberNamesPatterns = value),
      "--excluded-assemblies" => TrySetValue(args, ref index, argument, value => state.ExcludedAssemblyNames = value),
      "--excluded-types" => TrySetValue(args, ref index, argument, value => state.ExcludedTypeNamePatterns = value),
      "--replace-baseline" => TrySetFlag(() => state.ReplaceBaseline = true),
      "--baseline-storage-path" => TrySetValue(args, ref index, argument, value => state.BaselineStoragePath = value),
      "--coverage-html-dir" => TrySetValue(args, ref index, argument, value => state.CoverageHtmlDir = value),
      "--analyze-suppressed-symbols" => TrySetFlag(() => state.AnalyzeSuppressedSymbols = true),
      "--suppressed-symbols" => TrySetValue(args, ref index, argument, value => state.SuppressedSymbolsPath = value),
      "--solution-dir" => TrySetValue(args, ref index, argument, value => state.SolutionDirectory = value),
      "--source-code-folders" => TryProcessSourceCodeFolders(args, ref index, state),
      _ => false
    };
  }

  private static bool TrySetValue(string[] args, ref int index, string argumentName, Action<string> setter)
  {
    var value = RequireValue(args, ref index, argumentName);
    setter(value);
    return true;
  }

  private static bool TrySetFlag(Action setter)
  {
    setter();
    return true;
  }

  private static bool TryProcessSourceCodeFolders(string[] args, ref int index, ArgumentParserState state)
  {
    const char FolderSeparatorComma = ',';
    const char FolderSeparatorSemicolon = ';';

    var foldersValue = RequireValue(args, ref index, "--source-code-folders");
    var folders = foldersValue.Split(
        new[] { FolderSeparatorComma, FolderSeparatorSemicolon },
        StringSplitOptions.RemoveEmptyEntries)
      .Select(f => f.Trim())
      .Where(f => !string.IsNullOrWhiteSpace(f));
    state.SourceCodeFolders.AddRange(folders);
    return true;
  }

  private static void ValidateArguments(ArgumentParserState state)
  {
    if (string.IsNullOrWhiteSpace(state.InputJson))
    {
      ValidateRequiredForGeneration(state);
    }
    else
    {
      ValidateRequiredForHtmlGeneration(state);
    }
  }

  private static void ValidateRequiredForGeneration(ArgumentParserState state)
  {
    if (string.IsNullOrWhiteSpace(state.MetricsDir))
    {
      throw new ArgumentException("--metrics-dir is required when not using --input-json.");
    }

    if (string.IsNullOrWhiteSpace(state.OutputJson))
    {
      throw new ArgumentException("--output-json is required when not using --input-json.");
    }
  }

  private static void ValidateRequiredForHtmlGeneration(ArgumentParserState state)
  {
    if (string.IsNullOrWhiteSpace(state.OutputHtml))
    {
      throw new ArgumentException("--output-html is required when using --input-json.");
    }
  }

  private static MetricsReporterOptions CreateOptions(ArgumentParserState state)
  {
    var normalizedMetricsDir = string.IsNullOrWhiteSpace(state.MetricsDir) ? string.Empty : Path.GetFullPath(state.MetricsDir);
    var reportDir = string.IsNullOrWhiteSpace(normalizedMetricsDir) ? Path.GetTempPath() : Path.Combine(normalizedMetricsDir, "Report");
    var logFilePath = Path.Combine(reportDir, "MetricsReporter.log");

    return new MetricsReporterOptions
    {
      SolutionName = string.IsNullOrWhiteSpace(state.SolutionName) ? "Solution" : state.SolutionName,
      MetricsDirectory = normalizedMetricsDir,
      AltCoverPaths = state.AltCoverPaths.Select(Path.GetFullPath).ToArray(),
      RoslynPaths = state.RoslynPaths.Select(Path.GetFullPath).ToArray(),
      SarifPaths = state.SarifPaths.Select(Path.GetFullPath).ToArray(),
      BaselinePath = state.BaselinePath is null ? null : Path.GetFullPath(state.BaselinePath),
      BaselineReference = state.BaselineRef,
      ThresholdsJson = state.Thresholds,
      ThresholdsPath = state.ThresholdsFile is null ? null : Path.GetFullPath(state.ThresholdsFile),
      InputJsonPath = state.InputJson is null ? null : Path.GetFullPath(state.InputJson),
      OutputJsonPath = string.IsNullOrWhiteSpace(state.OutputJson) ? string.Empty : Path.GetFullPath(state.OutputJson),
      OutputHtmlPath = string.IsNullOrWhiteSpace(state.OutputHtml) ? string.Empty : Path.GetFullPath(state.OutputHtml),
      LogFilePath = logFilePath,
      ExcludedMemberNamesPatterns = state.ExcludedMemberNamesPatterns,
      ExcludedAssemblyNames = state.ExcludedAssemblyNames,
      ExcludedTypeNamePatterns = state.ExcludedTypeNamePatterns,
      ReplaceMetricsBaseline = state.ReplaceBaseline,
      MetricsReportStoragePath = state.BaselineStoragePath is null ? null : Path.GetFullPath(state.BaselineStoragePath),
      CoverageHtmlDir = state.CoverageHtmlDir is null ? null : Path.GetFullPath(state.CoverageHtmlDir),
      AnalyzeSuppressedSymbols = state.AnalyzeSuppressedSymbols,
      SuppressedSymbolsPath = state.SuppressedSymbolsPath is null ? null : Path.GetFullPath(state.SuppressedSymbolsPath),
      SolutionDirectory = state.SolutionDirectory is null ? null : Path.GetFullPath(state.SolutionDirectory),
      SourceCodeFolders = state.SourceCodeFolders.ToArray()
    };
  }

  private sealed class ArgumentParserState
  {
    public List<string> RoslynPaths { get; } = [];
    public List<string> SarifPaths { get; } = [];
    public string? SolutionName { get; set; }
    public string? MetricsDir { get; set; }
    public List<string> AltCoverPaths { get; } = [];
    public string? BaselinePath { get; set; }
    public string? BaselineRef { get; set; }
    public string? OutputJson { get; set; }
    public string? OutputHtml { get; set; }
    public string? Thresholds { get; set; }
    public string? ThresholdsFile { get; set; }
    public string? InputJson { get; set; }
    public string? ExcludedAssemblyNames { get; set; }
    public string? ExcludedTypeNamePatterns { get; set; }
    public string? ExcludedMemberNamesPatterns { get; set; }
    public bool AnalyzeSuppressedSymbols { get; set; }
    public string? SuppressedSymbolsPath { get; set; }
    public string? SolutionDirectory { get; set; }
    public List<string> SourceCodeFolders { get; } = [];
    public bool ReplaceBaseline { get; set; }
    public string? BaselineStoragePath { get; set; }
    public string? CoverageHtmlDir { get; set; }
  }

  private static string RequireValue(string[] args, ref int index, string argumentName)
  {
    if (index + 1 >= args.Length)
    {
      throw new ArgumentException($"Missing value for {argumentName}.");
    }

    index++;
    return args[index];
  }

  private void PrintUsage()
  {
    _outputWriter.WriteLine("RCA Metrics Reporter");
    _outputWriter.WriteLine();
    _outputWriter.WriteLine("Required parameters:");
    _outputWriter.WriteLine("  --metrics-dir <path>    Root directory for metrics artifacts (MetricsDir).");
    _outputWriter.WriteLine("  --output-json <path>    Path to the resulting metrics-report.json.");
    _outputWriter.WriteLine();
    _outputWriter.WriteLine("Optional parameters:");
    _outputWriter.WriteLine("  --output-html <path>    Path to the resulting metrics-report.html.");
    _outputWriter.WriteLine("  --solution-name <name>  Solution name for the report header.");
    _outputWriter.WriteLine("  --altcover <path>       Path to AltCover/OpenCover coverage.xml (repeatable).");
    _outputWriter.WriteLine("  --roslyn <path>         Path to Roslyn metrics XML (repeat for multiple files).");
    _outputWriter.WriteLine("  --sarif <path>          Path to SARIF file (repeat for multiple files).");
    _outputWriter.WriteLine("  --baseline <path>       Path to baseline metrics JSON.");
    _outputWriter.WriteLine("  --baseline-ref <text>   Baseline reference label (git commit, build ID, etc.).");
    _outputWriter.WriteLine("  --thresholds <json>     JSON string with metric thresholds.");
    _outputWriter.WriteLine("  --thresholds-file <path> Path to JSON file with symbol-level thresholds.");
    _outputWriter.WriteLine("  --input-json <path>     Path to existing metrics-report.json (generates HTML only).");
    _outputWriter.WriteLine("  --replace-baseline      Automatically replace baseline if new report differs from existing baseline.");
    _outputWriter.WriteLine("  --baseline-storage-path <path> Directory where old baseline files are archived with timestamps.");
    _outputWriter.WriteLine("  --excluded-members <list> Comma-separated or semicolon-separated list of member name patterns to exclude.");
    _outputWriter.WriteLine("  --excluded-assemblies <list> Comma-separated or semicolon-separated list of assembly patterns to exclude.");
    _outputWriter.WriteLine("  --excluded-types <list> Comma-separated or semicolon-separated list of type name patterns to exclude.");
    _outputWriter.WriteLine("  --analyze-suppressed-symbols  Analyze SuppressMessage attributes and persist suppressed symbol metadata.");
    _outputWriter.WriteLine("  --suppressed-symbols <path>  Path to JSON file where suppressed symbol metadata will be stored.");
    _outputWriter.WriteLine("  --solution-dir <path>        Root directory of the solution source tree for suppressed symbol analysis.");
    _outputWriter.WriteLine("  --source-code-folders <list> Comma- or semicolon-separated list of source code folder paths (relative to solution-dir)");
    _outputWriter.WriteLine("                               that contain assembly projects. Example: \"src,src/Tools,tests\".");
  }
}


