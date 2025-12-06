namespace MetricsReporter.Processing;

using System.Collections.Generic;
using System.Threading;
using MetricsReporter.Model;

/// <summary>
/// Encapsulates the context for suppressed symbol analysis.
/// </summary>
/// <remarks>
/// This DTO reduces coupling by grouping related parameters together,
/// making it easier to pass analysis context between methods.
/// </remarks>
internal sealed class SuppressedSymbolAnalysisContext
{
  /// <summary>
  /// Initializes a new instance of the <see cref="SuppressedSymbolAnalysisContext"/> class.
  /// </summary>
  /// <param name="normalizedRoot">The normalized root directory of the solution.</param>
  /// <param name="normalizedFolders">The normalized source code folder paths.</param>
  /// <param name="assemblyFilter">The filter to determine which assemblies to exclude.</param>
  /// <param name="suppressedSymbols">The collection to add discovered suppressed symbols to.</param>
  /// <param name="cancellationToken">Cancellation token for the operation.</param>
  public SuppressedSymbolAnalysisContext(
      string normalizedRoot,
      string[] normalizedFolders,
      AssemblyFilter assemblyFilter,
      ICollection<SuppressedSymbolInfo> suppressedSymbols,
      CancellationToken cancellationToken)
  {
    NormalizedRoot = normalizedRoot;
    NormalizedFolders = normalizedFolders;
    AssemblyFilter = assemblyFilter;
    SuppressedSymbols = suppressedSymbols;
    CancellationToken = cancellationToken;
  }

  /// <summary>
  /// Gets the normalized root directory of the solution.
  /// </summary>
  public string NormalizedRoot { get; }

  /// <summary>
  /// Gets the normalized source code folder paths.
  /// </summary>
  public string[] NormalizedFolders { get; }

  /// <summary>
  /// Gets the filter to determine which assemblies to exclude.
  /// </summary>
  public AssemblyFilter AssemblyFilter { get; }

  /// <summary>
  /// Gets the collection to add discovered suppressed symbols to.
  /// </summary>
  public ICollection<SuppressedSymbolInfo> SuppressedSymbols { get; }

  /// <summary>
  /// Gets the cancellation token for the operation.
  /// </summary>
  public CancellationToken CancellationToken { get; }
}


