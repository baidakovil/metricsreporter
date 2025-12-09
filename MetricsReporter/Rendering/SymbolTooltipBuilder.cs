namespace MetricsReporter.Rendering;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using MetricsReporter.Model;

/// <summary>
/// Builds HTML data attributes for symbol tooltips.
/// </summary>
internal static class SymbolTooltipBuilder
{
  private static readonly JsonSerializerOptions SymbolTooltipSerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  /// <summary>
  /// Builds a data-symbol-info attribute containing tooltip information for a metrics node.
  /// </summary>
  /// <param name="node">The metrics node to build tooltip data for.</param>
  /// <returns>
  /// A data-symbol-info attribute string with JSON-encoded tooltip data, or an empty string
  /// if the node has no fully qualified name.
  /// </returns>
  public static string BuildDataAttribute(MetricsNode node)
  {
    if (string.IsNullOrWhiteSpace(node.FullyQualifiedName))
    {
      return string.Empty;
    }

    var role = NodeKindProvider.GetKind(node);
    var roleUpper = role.ToUpperInvariant();
    if (node is MemberMetricsNode memberNode && memberNode.MemberKind != MemberKind.Unknown)
    {
      roleUpper = $"{roleUpper}: {memberNode.MemberKind.ToString().ToUpperInvariant()}";
    }
    var source = node.Source;
    var data = new
    {
      role = roleUpper,
      fullyQualifiedName = node.FullyQualifiedName,
      sourcePath = source?.Path,
      sourceStartLine = source?.StartLine,
      sourceEndLine = source?.EndLine
    };

    var json = JsonSerializer.Serialize(data, SymbolTooltipSerializerOptions);

    return $" data-symbol-info=\"{WebUtility.HtmlEncode(json)}\"";
  }
}


