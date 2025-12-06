namespace MetricsReporter.Processing;

using System;
using System.IO;
using MetricsReporter.Model;

/// <summary>
/// Processes individual C# files to extract suppressed symbol information.
/// </summary>
/// <remarks>
/// This class encapsulates the logic for processing files and collecting
/// suppressed symbol information, reducing coupling in the main analyzer class.
/// </remarks>
internal static class SuppressedSymbolFileProcessor
{
  /// <summary>
  /// Processes all C# files in the specified directories and collects suppressed symbol information.
  /// </summary>
  /// <param name="context">The analysis context containing all necessary parameters.</param>
  /// <param name="fileAnalyzer">The action to analyze a single file.</param>
  public static void ProcessFiles(
      SuppressedSymbolAnalysisContext context,
      Action<string, string, ICollection<SuppressedSymbolInfo>, System.Threading.CancellationToken> fileAnalyzer)
  {
    foreach (var filePath in SourceCodeFolderProcessor.EnumerateCSharpFiles(context.NormalizedRoot, context.NormalizedFolders))
    {
      context.CancellationToken.ThrowIfCancellationRequested();

      var assemblyName = SourceCodeFolderProcessor.TryResolveAssemblyName(context.NormalizedRoot, filePath, context.NormalizedFolders);
      if (string.IsNullOrWhiteSpace(assemblyName) || context.AssemblyFilter.ShouldExcludeAssembly(assemblyName))
      {
        continue;
      }

      var relativePath = Path.GetRelativePath(context.NormalizedRoot, filePath);
      fileAnalyzer(filePath, relativePath, context.SuppressedSymbols, context.CancellationToken);
    }
  }
}


