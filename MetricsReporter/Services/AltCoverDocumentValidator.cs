namespace MetricsReporter.Services;

using System;
using System.Collections.Generic;
using MetricsReporter.Logging;
using MetricsReporter.Model;
using MetricsReporter.Processing;

/// <summary>
/// Validates AltCover documents to ensure unique symbol coverage across multiple files.
/// </summary>
internal static class AltCoverDocumentValidator
{
  /// <summary>
  /// Ensures that a symbol (type or member) is not reported by more than one AltCover file.
  /// </summary>
  /// <param name="documents">AltCover documents collected from CLI/MSBuild inputs.</param>
  /// <param name="logger">Logger for error reporting.</param>
  /// <returns><see langword="true"/> when no duplicate symbols exist; otherwise <see langword="false"/>.</returns>
  public static bool TryValidateUniqueSymbols(IList<ParsedMetricsDocument> documents, ILogger logger)
  {
    ArgumentNullException.ThrowIfNull(documents);
    ArgumentNullException.ThrowIfNull(logger);

    if (documents.Count <= 1)
    {
      return true;
    }

    var symbolOrigins = new Dictionary<string, SymbolOrigin>(StringComparer.Ordinal);
    for (var index = 0; index < documents.Count; index++)
    {
      var document = documents[index];
      var documentId = ResolveDocumentId(document, index);

      foreach (var element in document.Elements)
      {
        if (!IsAltCoverSymbol(element))
        {
          continue;
        }

        var symbolKey = element.FullyQualifiedName;
        if (string.IsNullOrWhiteSpace(symbolKey))
        {
          continue;
        }

        if (symbolOrigins.TryGetValue(symbolKey, out var origin)
            && !string.Equals(origin.DocumentId, documentId, StringComparison.OrdinalIgnoreCase))
        {
          logger.LogError(
              $"Duplicate AltCover {DescribeKind(element.Kind)} '{symbolKey}' detected in '{origin.DocumentId}' and '{documentId}'. Ensure coverage XML inputs do not overlap.");
          return false;
        }

        symbolOrigins[symbolKey] = new SymbolOrigin(documentId, element.Kind);
      }
    }

    return true;
  }

  private static bool IsAltCoverSymbol(ParsedCodeElement element)
      => element.Kind is CodeElementKind.Type or CodeElementKind.Member;

  private static string ResolveDocumentId(ParsedMetricsDocument document, int index)
  {
    if (!string.IsNullOrWhiteSpace(document.SourcePath))
    {
      return document.SourcePath;
    }

    var humanIndex = index + 1;
    return $"AltCoverDocument#{humanIndex}";
  }

  private static string DescribeKind(CodeElementKind kind)
    => kind switch
    {
      CodeElementKind.Member => "member",
      CodeElementKind.Type => "type",
      _ => "symbol"
    };

  private sealed record SymbolOrigin(string DocumentId, CodeElementKind Kind);
}



