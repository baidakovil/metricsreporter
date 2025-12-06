namespace MetricsReporter.Rendering;
using System;
using System.Net;
using System.Text;
using System.Text.Json;
using MetricsReporter.Model;
/// <summary>
/// Provides helper methods for rendering node-specific HTML elements.
/// </summary>
internal static class NodeRenderer
{
  private static readonly JsonSerializerOptions SymbolTooltipSerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
  };
  /// <summary>
  /// Builds symbol tooltip data attribute for a metrics node.
  /// </summary>
  /// <param name="node">The metrics node.</param>
  /// <returns>HTML data attribute string with symbol information, or empty string if not applicable.</returns>
  public static string BuildSymbolTooltipData(MetricsNode node)
  {
    if (string.IsNullOrWhiteSpace(node.FullyQualifiedName))
    {
      return string.Empty;
    }
    var role = NodeKindProvider.GetKind(node);
    var roleUpper = role.ToUpperInvariant();
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
  /// <summary>
  /// Renders the node name with appropriate HTML markup.
  /// </summary>
  /// <param name="builder">The string builder to append to.</param>
  /// <param name="node">The metrics node.</param>
  /// <param name="nameText">The HTML-encoded name text.</param>
  /// <param name="coverageLink">Optional coverage link URL.</param>
  /// <param name="isNodeRow">Whether this is a structural node row.</param>
  /// <param name="nameTooltipData">Tooltip data attribute string.</param>
  public static void RenderNodeName(
      StringBuilder builder,
      MetricsNode node,
      string nameText,
      string? coverageLink,
      bool isNodeRow,
      string nameTooltipData)
  {
    if (!isNodeRow && node is MemberMetricsNode memberNode)
    {
      if (memberNode.IncludesIteratorStateMachineCoverage)
      {
        builder.Append("<span class=\"method-state-machine symbol-indicator\" data-simple-tooltip=\"Includes coverage from compiler-generated iterator state machine\">⊃</span>");
      }
      builder.Append("<span class=\"name-text item-name\"" + nameTooltipData + ">" + nameText + "</span>");
    }
    else if (isNodeRow && node is TypeMetricsNode)
    {
      if (!string.IsNullOrEmpty(coverageLink))
      {
        builder.Append($"<a href=\"{coverageLink}\" class=\"name-text coverage-link-type\" target=\"_blank\" rel=\"noopener noreferrer\"" + nameTooltipData + ">" + nameText + "</a>");
      }
      else
      {
        builder.Append("<span class=\"name-text\"" + nameTooltipData + ">" + nameText + "</span>");
      }
    }
    else if (!isNodeRow)
    {
      builder.Append("<span class=\"name-text item-name\"" + nameTooltipData + ">" + nameText + "</span>");
    }
    else
    {
      builder.Append("<span class=\"name-text\"" + nameTooltipData + ">" + nameText + "</span>");
    }
    if (node.IsNew)
    {
      builder.Append(" <span class=\"badge badge-new\">NEW</span>");
    }
  }
  /// <summary>
  /// Appends row action icons (Open, Copy, Filter) to the string builder.
  /// </summary>
  /// <param name="builder">The string builder to append to.</param>
  /// <param name="node">The metrics node.</param>
  public static void AppendRowActionIcons(StringBuilder builder, MetricsNode node)
  {
    builder.AppendLine("      <span class=\"row-action-icons\" aria-hidden=\"true\">");
    if (HasOpenSource(node))
    {
      builder.AppendLine("        <button type=\"button\" class=\"row-action-icon\" data-action=\"open\" aria-label=\"Open file in Cursor\" data-simple-tooltip=\"Open file in Cursor\">");
      builder.AppendLine("          O");
      builder.AppendLine("        </button>");
    }
    builder.AppendLine("        <button type=\"button\" class=\"row-action-icon\" data-action=\"copy\" aria-label=\"Copy symbol name\" data-simple-tooltip=\"Copy fully qualified symbol name\">");
    builder.AppendLine("          C");
    builder.AppendLine("        </button>");
    builder.AppendLine("        <button type=\"button\" class=\"row-action-icon\" data-action=\"filter\" aria-label=\"Filter by this symbol\" data-simple-tooltip=\"Set filter to this symbol\">");
    builder.AppendLine("          F");
    builder.AppendLine("        </button>");
    builder.AppendLine("      </span>");
  }
  private static bool HasOpenSource(MetricsNode node)
      => node.Source?.Path is not null && node.Source.StartLine.HasValue;
}






