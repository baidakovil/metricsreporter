namespace MetricsReporter.Rendering;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using MetricsReporter.Model;

/// <summary>
/// Generates the HTML table for the metrics report.
/// Handles recursive rendering of the metrics hierarchy.
/// </summary>
internal sealed class HtmlTableGenerator
{
  private readonly MetricIdentifier[] _metricOrder;
  private readonly IReadOnlyDictionary<MetricIdentifier, string?> _metricUnits;
  private int _idCounter;
  private CoverageLinkBuilder? _coverageLinkBuilder;
  private Dictionary<(string Fqn, MetricIdentifier Metric), SuppressedSymbolInfo>? _suppressedIndex;
  private RowStateCalculator? _stateCalculator;
  private RowAttributeBuilder? _attributeBuilder;
  private MetricCellRenderer? _metricCellRenderer;
  private NodeChildrenRenderer? _childrenRenderer;

  /// <summary>
  /// Initializes a new instance of the <see cref="HtmlTableGenerator"/> class.
  /// </summary>
  /// <param name="metricOrder">The order of metrics to display in columns.</param>
  /// <param name="metricUnits">Units associated with each metric.</param>
  public HtmlTableGenerator(MetricIdentifier[] metricOrder, IReadOnlyDictionary<MetricIdentifier, string?> metricUnits)
  {
    _metricOrder = metricOrder ?? throw new ArgumentNullException(nameof(metricOrder));
    _metricUnits = metricUnits ?? throw new ArgumentNullException(nameof(metricUnits));
  }

  /// <summary>
  /// Generates the HTML table markup for the metrics report.
  /// </summary>
  /// <param name="report">The metrics report.</param>
  /// <param name="coverageHtmlDir">Optional path to HTML coverage reports directory for generating hyperlinks.</param>
  /// <returns>HTML markup for the table.</returns>
  public string Generate(MetricsReport report, string? coverageHtmlDir = null)
  {
    ArgumentNullException.ThrowIfNull(report);

    _idCounter = 0;
    return BuildTableHtml(report, coverageHtmlDir);
  }

  private string BuildTableHtml(MetricsReport report, string? coverageHtmlDir)
  {
    InitializeRenderers(report, coverageHtmlDir);

    var builder = new StringBuilder();
    TableContentBuilder.Build(_metricOrder, report, this, builder);

    CleanupRenderers();

    return builder.ToString();
  }

  private void InitializeRenderers(MetricsReport report, string? coverageHtmlDir)
  {
    var components = TableRendererInitializer.Initialize(
      _metricOrder,
      _metricUnits,
      report,
      coverageHtmlDir);
    AssignRenderers(components);
  }

  private void AssignRenderers(RendererComponents components)
  {
    _coverageLinkBuilder = components.CoverageLinkBuilder;
    _suppressedIndex = components.SuppressedIndex;
    _stateCalculator = components.StateCalculator;
    _attributeBuilder = components.AttributeBuilder;
    _metricCellRenderer = components.MetricCellRenderer;
    _childrenRenderer = new NodeChildrenRenderer(this);
  }

  private void CleanupRenderers()
  {
    _stateCalculator = null;
    _attributeBuilder = null;
    _metricCellRenderer = null;
    _childrenRenderer = null;
  }

  internal void RenderTableBody(MetricsReport report, StringBuilder builder)
  {
    // Skip Solution node and render Assemblies directly as top-level items (level 0)
    if (report.Solution is SolutionMetricsNode solution)
    {
      RenderAssemblies(solution, builder);
    }
  }

  private void RenderAssemblies(SolutionMetricsNode solution, StringBuilder builder)
  {
    foreach (var assembly in solution.Assemblies.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
    {
      RenderNodeRows(assembly, 0, null, builder, assembly.Name);
    }
  }

  internal void RenderNodeRows(MetricsNode node, int level, string? parentId, StringBuilder builder, string? currentAssembly = null, string? currentType = null)
  {
    var thisId = "node-" + (++_idCounter).ToString();
    var hasChildren = HasChildren(node);
    var role = GetNodeRole(node);
    var isStructuralNode = role is "assembly" or "namespace" or "type";
    var assemblyName = UpdateAssemblyName(node, currentAssembly);
    var typeName = UpdateTypeName(node, currentType);
    var isNodeRow = hasChildren || isStructuralNode;
    var symbolTag = isNodeRow ? "th" : "td";
    var rowClass = isNodeRow ? "node-row node-header" : "node-row node-item";
    var symbolTooltipData = SymbolTooltipBuilder.BuildDataAttribute(node);
    var coverageLink = BuildCoverageLink(node, isNodeRow, assemblyName);
    var nameText = WebUtility.HtmlEncode(node.Name);

    var combinedAttributes = _attributeBuilder!.BuildAllAttributes(node);
    AppendRowStart(builder, rowClass, thisId, level, parentId, hasChildren, role, node.IsNew, node.FullyQualifiedName, combinedAttributes);
    AppendSymbolCell(builder, node, symbolTag, hasChildren, isStructuralNode, nameText, coverageLink, thisId, isNodeRow, symbolTooltipData);
    _metricCellRenderer!.AppendCells(node, symbolTag, builder);
    builder.AppendLine("    </tr>");

    _childrenRenderer!.Render(node, level + 1, thisId, builder, assemblyName, typeName);
  }

  private static bool HasChildren(MetricsNode node)
      => node switch
      {
        SolutionMetricsNode s => s.Assemblies.Any(),
        AssemblyMetricsNode a => a.Namespaces.Any(),
        NamespaceMetricsNode n => n.Types.Any(),
        TypeMetricsNode t => t.Members.Any(),
        _ => false
      };

  private static string? UpdateAssemblyName(MetricsNode node, string? currentAssembly)
      => node is AssemblyMetricsNode assemblyNode ? assemblyNode.Name : currentAssembly;

  private static string? UpdateTypeName(MetricsNode node, string? currentType)
      => node is TypeMetricsNode typeNode ? typeNode.Name : currentType;


  private string? BuildCoverageLink(MetricsNode node, bool isNodeRow, string? assemblyName)
      => isNodeRow && node is TypeMetricsNode typeNode && _coverageLinkBuilder is not null
          ? _coverageLinkBuilder.BuildLink(typeNode, assemblyName)
          : null;

  private static void AppendRowStart(
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

  private static void AppendSymbolCell(
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

    RenderNodeName(builder, node, nameText, coverageLink, isNodeRow, nameTooltipData);
    AppendRowActionIcons(builder, node);

    builder.AppendLine($"</{symbolTag}>");
  }

  private static void RenderNodeName(
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

  private static void AppendRowActionIcons(StringBuilder builder, MetricsNode node)
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


  private static string GetNodeRole(MetricsNode node)
      => node switch
      {
        AssemblyMetricsNode => "assembly",
        NamespaceMetricsNode => "namespace",
        TypeMetricsNode => "type",
        MemberMetricsNode => "member",
        _ => "node"
      };

}


