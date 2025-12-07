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

    var registry = new SymbolRegistry();
    for (var index = 0; index < documents.Count; index++)
    {
      var document = documents[index];
      var documentId = ResolveDocumentId(document, index);

      if (!ValidateDocument(document, documentId, registry, logger))
      {
        return false;
      }
    }

    return true;
  }

  private static bool ValidateDocument(
    ParsedMetricsDocument document,
    string documentId,
    SymbolRegistry registry,
    ILogger logger)
  {
    foreach (var element in document.Elements)
    {
      if (!registry.TryAdd(element, documentId, logger))
      {
        return false;
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

  private sealed class SymbolRegistry
  {
    private readonly Dictionary<string, string> _origins = new(StringComparer.Ordinal);

    public bool TryAdd(ParsedCodeElement element, string documentId, ILogger logger)
    {
      if (!IsAltCoverSymbol(element))
      {
        return true;
      }

      var symbolKey = element.FullyQualifiedName;
      if (string.IsNullOrWhiteSpace(symbolKey))
      {
        return true;
      }

      if (_origins.TryGetValue(symbolKey, out var origin)
          && !string.Equals(origin, documentId, StringComparison.OrdinalIgnoreCase))
      {
        logger.LogError(
          $"Duplicate AltCover {DescribeKind(element.Kind)} '{symbolKey}' detected in '{origin}' and '{documentId}'. Ensure coverage XML inputs do not overlap.");
        return false;
      }

      _origins[symbolKey] = documentId;
      return true;
    }
  }
}



