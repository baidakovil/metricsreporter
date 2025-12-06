namespace MetricsReporter.Processing.Parsers;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

/// <summary>
/// Provides asynchronous access to Roslyn metrics XML documents stored on disk.
/// </summary>
internal interface IRoslynMetricsDocumentLoader
{
  /// <summary>
  /// Loads the XML document residing at <paramref name="path" />.
  /// </summary>
  /// <param name="path">Absolute path to the metrics XML file.</param>
  /// <param name="cancellationToken">Token that cancels file IO or XML parsing.</param>
  /// <returns>The loaded <see cref="XDocument" /> instance.</returns>
  Task<XDocument> LoadAsync(string path, CancellationToken cancellationToken);
}

/// <summary>
/// Default implementation that streams Roslyn metrics XML files from disk.
/// </summary>
internal sealed class RoslynMetricsDocumentLoader : IRoslynMetricsDocumentLoader
{
  /// <inheritdoc />
  public async Task<XDocument> LoadAsync(string path, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(path);

    await using var stream = File.OpenRead(path);
    return await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
  }
}


