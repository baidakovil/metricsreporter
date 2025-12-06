namespace MetricsReporter.Processing.Parsers;

using System;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Model;

/// <summary>
/// Parses Microsoft.CodeAnalysis.Metrics XML reports.
/// </summary>
public sealed class RoslynMetricsParser : IMetricsSourceParser
{
  private readonly IRoslynMetricsDocumentLoader documentLoader;
  private readonly RoslynMetricsDocumentWalker documentWalker;

  /// <summary>
  /// Initializes a parser that uses the default loader and document walker.
  /// </summary>
  public RoslynMetricsParser()
      : this(new RoslynMetricsDocumentLoader(), new RoslynMetricsDocumentWalker())
  {
  }

  /// <summary>
  /// Initializes a parser with custom dependencies for loader and walker abstractions.
  /// </summary>
  /// <param name="documentLoader">Provides access to Roslyn metrics XML files.</param>
  /// <param name="documentWalker">Converts <see cref="System.Xml.Linq.XDocument"/> into parsed elements.</param>
  /// <exception cref="ArgumentNullException">Thrown when either dependency is <see langword="null"/>.</exception>
  internal RoslynMetricsParser(
      IRoslynMetricsDocumentLoader documentLoader,
      RoslynMetricsDocumentWalker documentWalker)
  {
    this.documentLoader = documentLoader ?? throw new ArgumentNullException(nameof(documentLoader));
    this.documentWalker = documentWalker ?? throw new ArgumentNullException(nameof(documentWalker));
  }

  /// <inheritdoc />
  public async Task<ParsedMetricsDocument> ParseAsync(string path, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(path);

    var document = await documentLoader.LoadAsync(path, cancellationToken).ConfigureAwait(false);
    return RoslynMetricsDocumentWalker.Parse(document);
  }
}


