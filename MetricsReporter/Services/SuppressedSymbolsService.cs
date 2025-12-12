namespace MetricsReporter.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides suppressed symbol metadata by either running Roslyn analysis or loading cached artefacts.
/// </summary>
internal sealed class SuppressedSymbolsService : ISuppressedSymbolsService
{
  /// <summary>
  /// Resolves suppressed symbols according to the configured options.
  /// </summary>
  /// <param name="options">Metrics reporter options.</param>
  /// <param name="logger">Logger for progress and error messages.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <returns>A list of suppressed symbols. Returns an empty list when no data is available.</returns>
  public async Task<List<SuppressedSymbolInfo>> ResolveAsync(
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    if (!options.AnalyzeSuppressedSymbols)
    {
      return await LoadFromCacheAsync(options, cancellationToken).ConfigureAwait(false);
    }

    return await AnalyzeAsync(options, logger, cancellationToken).ConfigureAwait(false);
  }

  private static async Task<List<SuppressedSymbolInfo>> LoadFromCacheAsync(
      MetricsReporterOptions options,
      CancellationToken cancellationToken)
  {
    var loadedSymbols = await SuppressedSymbolsLoader.LoadAsync(options.SuppressedSymbolsPath, cancellationToken).ConfigureAwait(false);
    return loadedSymbols.ToList();
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Microsoft.Maintainability",
      "CA1506:Avoid excessive class coupling",
      Justification = "Suppressed symbols analysis method coordinates Roslyn analysis, directory resolution, and file I/O through helper methods; further decomposition would create artificial wrappers that degrade code readability.")]
  private static async Task<List<SuppressedSymbolInfo>> AnalyzeAsync(
      MetricsReporterOptions options,
      ILogger logger,
      CancellationToken cancellationToken)
  {
    try
    {
      ValidateAnalysisOptions(options);
      var analysisContext = CreateAnalysisContext(options);
      LogAnalysisStart(analysisContext, logger);

      var suppressedReport = ExecuteAnalysis(analysisContext, cancellationToken);
      EnsureOutputDirectoryExists(options.SuppressedSymbolsPath!);
      await WriteAnalysisResultsAsync(suppressedReport, options.SuppressedSymbolsPath!, cancellationToken).ConfigureAwait(false);

      var suppressedSymbols = suppressedReport.SuppressedSymbols.ToList();
      LogAnalysisCompletion(suppressedSymbols.Count, logger);
      return suppressedSymbols;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to analyze suppressed symbols. Proceeding without suppression metadata.");
      return [];
    }
  }

  private static void ValidateAnalysisOptions(MetricsReporterOptions options)
  {
    if (string.IsNullOrWhiteSpace(options.SuppressedSymbolsPath))
    {
      throw new ArgumentException("Suppressed symbols path must be specified when AnalyzeSuppressedSymbols is enabled.", nameof(options));
    }
  }

  private static SuppressedSymbolsAnalysisContext CreateAnalysisContext(MetricsReporterOptions options)
  {
    var suppressionRoot = ResolveRootDirectory(options);
    var sourceCodeFolders = options.SourceCodeFolders ?? Array.Empty<string>();
    return new SuppressedSymbolsAnalysisContext(suppressionRoot, sourceCodeFolders, options.ExcludedAssemblyNames);
  }

  private static void LogAnalysisStart(SuppressedSymbolsAnalysisContext context, ILogger logger)
  {
    var foldersText = string.Join(", ", context.SourceCodeFolders);
    var excludedAssemblies = context.ExcludedAssemblyNames ?? string.Empty;
    logger.LogDebug(
      "Analyzing suppressed symbols via Roslyn root={SolutionRoot} sourceFolders=[{SourceFolders}] excludedAssemblies={ExcludedAssemblies}",
      context.SolutionRoot,
      foldersText,
      excludedAssemblies);
  }

  private static SuppressedSymbolsReport ExecuteAnalysis(
      SuppressedSymbolsAnalysisContext context,
      CancellationToken cancellationToken)
  {
    return Processing.SuppressedSymbolsAnalyzer.Analyze(
        context.SolutionRoot,
        context.SourceCodeFolders,
        context.ExcludedAssemblyNames,
        cancellationToken);
  }

  private static void EnsureOutputDirectoryExists(string suppressedSymbolsPath)
  {
    var suppressedDirectory = Path.GetDirectoryName(suppressedSymbolsPath);
    if (!string.IsNullOrWhiteSpace(suppressedDirectory) && !Directory.Exists(suppressedDirectory))
    {
      Directory.CreateDirectory(suppressedDirectory);
    }
  }

  private static async Task WriteAnalysisResultsAsync(
      SuppressedSymbolsReport report,
      string outputPath,
      CancellationToken cancellationToken)
  {
    await SuppressedSymbolsWriter.WriteAsync(report, outputPath, cancellationToken).ConfigureAwait(false);
  }

  private static void LogAnalysisCompletion(int symbolCount, ILogger logger)
  {
    logger.LogInformation("Suppressed symbols analysis completed: {EntryCount} entries", symbolCount);
  }

  private sealed record SuppressedSymbolsAnalysisContext(
      string SolutionRoot,
      IReadOnlyCollection<string> SourceCodeFolders,
      string? ExcludedAssemblyNames);

  private static string ResolveRootDirectory(MetricsReporterOptions options)
  {
    if (!string.IsNullOrWhiteSpace(options.SolutionDirectory))
    {
      var explicitRoot = Path.GetFullPath(options.SolutionDirectory);
      if (Directory.Exists(explicitRoot))
      {
        return explicitRoot;
      }
    }

    var startDirectory = !string.IsNullOrWhiteSpace(options.MetricsDirectory)
      ? Path.GetFullPath(options.MetricsDirectory)
      : AppContext.BaseDirectory;

    var currentDirectory = new DirectoryInfo(startDirectory);
    while (currentDirectory is not null)
    {
      try
      {
        var hasSolution = currentDirectory.GetFiles("*.sln").Length > 0;
        if (hasSolution)
        {
          return currentDirectory.FullName;
        }
      }
      catch (IOException)
      {
        // Ignore IO issues and continue walking up the tree.
      }
      catch (UnauthorizedAccessException)
      {
        // Ignore permission issues when probing for solution files.
      }

      currentDirectory = currentDirectory.Parent;
    }

    return startDirectory;
  }
}


