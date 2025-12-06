namespace MetricsReporter.Rendering;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using MetricsReporter.Model;
/// <summary>
/// Renders individual table rows for metrics nodes.
/// </summary>
internal static class RowRenderer
{
  /// <summary>
  /// Appends the start of a table row to the string builder.
  /// </summary>
  /// <param name="builder">The string builder to append to.</param>
  /// <param name="rowClass">The CSS class for the row.</param>
  /// <param name="thisId">The unique ID for this row.</param>
  /// <param name="level">The hierarchy level.</param>
  /// <param name="parentId">The parent row ID, or <see langword="null"/>.</param>
  /// <param name="hasChildren">Whether this row has children.</param>
  /// <param name="role">The role string (e.g., "assembly", "type").</param>
  /// <param name="isNew">Whether this is a new node.</param>
  /// <param name="fullyQualifiedName">The fully qualified name of the node.</param>
  /// <param name="extendedAttributes">Additional HTML attributes.</param>
  public static void AppendRowStart(
      StringBuilder builder,
      string rowClass,
      string thisId,
      int level,
      string? parentId,
      bool hasChildren,
      string role,
      bool isNew,
      string? fullyQualifiedName,
      string extendedAttributes)
  {
    var fqnAttribute = string.IsNullOrWhiteSpace(fullyQualifiedName)
        ? string.Empty
        : $" data-fqn=\"{WebUtility.HtmlEncode(fullyQualifiedName)}\"";
    builder.AppendLine("    <tr class=\"" + rowClass + "\" " +
        $"data-id=\"{thisId}\" data-level=\"{level}\" data-parent=\"{parentId ?? string.Empty}\" data-has-children=\"{hasChildren.ToString().ToLowerInvariant()}\" data-role=\"{role}\" data-is-new=\"{(isNew ? "true" : "false")}\"{fqnAttribute}{extendedAttributes}>");
  }
  /// <summary>
  /// Appends a symbol cell to the string builder.
  /// </summary>
  /// <param name="builder">The string builder to append to.</param>
  /// <param name="node">The metrics node.</param>
  /// <param name="symbolTag">The HTML tag to use (th or td).</param>
  /// <param name="hasChildren">Whether this node has children.</param>
  /// <param name="isStructuralNode">Whether this is a structural node (assembly, namespace, type).</param>
  /// <param name="nameText">The HTML-encoded name text.</param>
  /// <param name="coverageLink">Optional coverage link URL.</param>
  /// <param name="thisId">The unique ID for this row.</param>
  /// <param name="isNodeRow">Whether this is a node row (has children or is structural).</param>
  /// <param name="nameTooltipData">Tooltip data attribute string.</param>
  public static void AppendSymbolCell(
      StringBuilder builder,
      MetricsNode node,
      string symbolTag,
      bool hasChildren,
      bool isStructuralNode,
      string nameText,
      string? coverageLink,
      string thisId,
      bool isNodeRow,
      string nameTooltipData)
  {
    var symbolClasses = "symbol" + (hasChildren || isStructuralNode ? " has-expander" : string.Empty);
    builder.Append($"      <{symbolTag} class=\"{symbolClasses}\">");
    if (hasChildren)
    {
      builder.Append($"<button class=\"expander\" data-target=\"{thisId}\" aria-label=\"Toggle expand/collapse\">-</button>");
    }
    else if (isStructuralNode)
    {
      builder.Append("<span class=\"expander-placeholder symbol-indicator\" data-simple-tooltip=\"No child nodes available\" aria-hidden=\"true\">Ø</span>");
    }
    NodeRenderer.RenderNodeName(builder, node, nameText, coverageLink, isNodeRow, nameTooltipData);
    NodeRenderer.AppendRowActionIcons(builder, node);
    builder.AppendLine($"</{symbolTag}>");
  }
}






