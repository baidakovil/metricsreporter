namespace MetricsReporter.MetricsReader.Services;

using System;
using MetricsReporter.Model;

/// <summary>
/// Provides helper methods to derive namespaces, types, and members from fully qualified symbols.
/// </summary>
internal static class SymbolMetadataParser
{
  private const string GlobalNamespace = "<global>";
  private const string UnknownSymbol = "<unknown>";

  /// <summary>
  /// Parses the supplied fully qualified symbol into metadata components.
  /// </summary>
  /// <param name="symbol">The fully qualified symbol to examine.</param>
  /// <param name="kind">The code element kind associated with the symbol.</param>
  /// <returns>Structured metadata describing namespace/type/member parts.</returns>
  public static SymbolMetadata Parse(string? symbol, CodeElementKind kind)
  {
    var safeSymbol = (symbol ?? string.Empty).Trim();
    if (safeSymbol.Length == 0)
    {
      return new SymbolMetadata(UnknownSymbol, UnknownSymbol, GlobalNamespace, null);
    }

    if (kind == CodeElementKind.Type)
    {
      var ns = ExtractNamespace(safeSymbol);
      return new SymbolMetadata(safeSymbol, safeSymbol, ns, null);
    }

    var withoutParameters = StripParameters(safeSymbol);
    var separatorIndex = FindMethodSeparatorIndex(withoutParameters);
    if (separatorIndex < 0)
    {
      var fallbackNamespace = ExtractNamespace(safeSymbol);
      return new SymbolMetadata(safeSymbol, safeSymbol, fallbackNamespace, null);
    }

    var typePart = withoutParameters[..separatorIndex];
    var methodPart = withoutParameters[(separatorIndex + 1)..];
    var typeName = typePart.Length == 0 ? safeSymbol : typePart;

    var nsName = ExtractNamespace(typeName);
    var normalizedMethod = methodPart.Length == 0 ? null : methodPart;

    return new SymbolMetadata(safeSymbol, typeName, nsName, normalizedMethod);
  }

  private static string StripParameters(string value)
  {
    var index = value.IndexOf('(');
    return index < 0 ? value : value[..index];
  }

  private static int FindMethodSeparatorIndex(string value)
  {
    var lastDot = value.LastIndexOf('.');
    if (lastDot < 0)
    {
      return -1;
    }

    if (lastDot > 0 && value[lastDot - 1] == '.')
    {
      return lastDot - 1;
    }

    return lastDot;
  }

  private static string ExtractNamespace(string value)
  {
    var trimmed = value.TrimEnd('.');
    var lastDot = trimmed.LastIndexOf('.');
    if (lastDot <= 0)
    {
      return GlobalNamespace;
    }

    return trimmed[..lastDot];
  }
}

/// <summary>
/// Represents decomposed metadata for a fully qualified symbol.
/// </summary>
internal readonly record struct SymbolMetadata(
  string Symbol,
  string TypeName,
  string Namespace,
  string? MethodName)
{
  /// <summary>
  /// Returns <see langword="true"/> when <see cref="MethodName"/> contains data.
  /// </summary>
  public bool HasMethod => !string.IsNullOrEmpty(MethodName);
}

