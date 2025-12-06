namespace MetricsReporter.Rendering;

using System;
using System.Text;
using MetricsReporter.Model;

/// <summary>
/// Renders child nodes for a metrics node in the HTML table.
/// </summary>
internal sealed class NodeChildrenRenderer
{
  private readonly HtmlTableGenerator _tableGenerator;

  /// <summary>
  /// Initializes a new instance of the <see cref="NodeChildrenRenderer"/> class.
  /// </summary>
  /// <param name="tableGenerator">The table generator instance to use for rendering rows.</param>
  public NodeChildrenRenderer(HtmlTableGenerator tableGenerator)
  {
    _tableGenerator = tableGenerator ?? throw new ArgumentNullException(nameof(tableGenerator));
  }

  /// <summary>
  /// Renders child nodes for the specified parent node.
  /// </summary>
  /// <param name="node">The parent metrics node.</param>
  /// <param name="level">The nesting level for child nodes.</param>
  /// <param name="parentId">The ID of the parent row.</param>
  /// <param name="builder">The string builder to append HTML to.</param>
  /// <param name="assemblyName">The current assembly name context.</param>
  /// <param name="typeName">The current type name context.</param>
  public void Render(MetricsNode node, int level, string parentId, StringBuilder builder, string? assemblyName, string? typeName)
  {
    switch (node)
    {
      case SolutionMetricsNode solution:
        foreach (var assembly in NodeOrderer.GetOrderedAssemblies(solution))
        {
          _tableGenerator.RenderNodeRows(assembly, level, parentId, builder, assembly.Name);
        }
        break;
      case AssemblyMetricsNode assembly:
        foreach (var ns in NodeOrderer.GetOrderedNamespaces(assembly))
        {
          _tableGenerator.RenderNodeRows(ns, level, parentId, builder, assemblyName);
        }
        break;
      case NamespaceMetricsNode @namespace:
        foreach (var type in NodeOrderer.GetOrderedTypes(@namespace))
        {
          _tableGenerator.RenderNodeRows(type, level, parentId, builder, assemblyName);
        }
        break;
      case TypeMetricsNode type:
        foreach (var member in NodeOrderer.GetOrderedMembers(type))
        {
          _tableGenerator.RenderNodeRows(member, level, parentId, builder, assemblyName, typeName);
        }
        break;
    }
  }
}


