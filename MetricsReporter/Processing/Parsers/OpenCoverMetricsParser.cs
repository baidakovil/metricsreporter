namespace MetricsReporter.Processing.Parsers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MetricsReporter.Model;

/// <summary>
/// Parses OpenCover XML reports using a namespace-agnostic, tolerant strategy.
/// </summary>
public sealed class OpenCoverMetricsParser : IMetricsSourceParser
{
  /// <inheritdoc />
  public async Task<ParsedMetricsDocument> ParseAsync(string path, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(path);

    var elements = await ReadCodeElementsAsync(path, cancellationToken).ConfigureAwait(false);

    return new ParsedMetricsDocument
    {
      SolutionName = string.Empty,
      Elements = elements,
      SourcePath = Path.GetFullPath(path)
    };
  }

  private static async Task<List<ParsedCodeElement>> ReadCodeElementsAsync(string path, CancellationToken cancellationToken)
  {
    var document = await LoadXmlDocumentAsync(path, cancellationToken).ConfigureAwait(false);
    var coverageRoot = ExtractCoverageRoot(document);
    var modules = ExtractModules(coverageRoot);
    return modules.SelectMany(ParseModule).ToList();
  }

  /// <summary>
  /// Loads an XML document from the specified file path.
  /// </summary>
  /// <param name="path">Path to the XML file.</param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  /// <returns>The loaded XML document.</returns>
  private static async Task<XDocument> LoadXmlDocumentAsync(string path, CancellationToken cancellationToken)
  {
    await using var stream = File.OpenRead(path);
    return await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Extracts the OpenCover root element from the XML document.
  /// </summary>
  /// <param name="document">The XML document to extract from.</param>
  /// <returns>The element that contains OpenCover coverage data.</returns>
  private static XElement ExtractCoverageRoot(XDocument document)
  {
    var root = document.Root ?? throw new InvalidOperationException("Coverage XML document has no root element.");

    if (IsCoverageRoot(root))
    {
      return root;
    }

    return root.Descendants().FirstOrDefault(IsCoverageRoot) ?? root;
  }

  /// <summary>
  /// Extracts module elements from the OpenCover root element.
  /// </summary>
  /// <param name="coverageRoot">The OpenCover root element containing modules.</param>
  /// <returns>Enumerable collection of Module elements.</returns>
  private static IEnumerable<XElement> ExtractModules(XElement coverageRoot)
  {
    var modulesElement = coverageRoot.ElementByLocalName("Modules")
                         ?? coverageRoot.DescendantsByLocalName("Modules").FirstOrDefault();

    if (modulesElement is not null)
    {
      return modulesElement.ElementsByLocalName("Module");
    }

    return coverageRoot.DescendantsByLocalName("Module");
  }

  private static bool IsCoverageRoot(XElement element)
    => string.Equals(element.Name.LocalName, "CoverageSession", StringComparison.OrdinalIgnoreCase)
       || string.Equals(element.Name.LocalName, "Coverage", StringComparison.OrdinalIgnoreCase);

  private static IEnumerable<ParsedCodeElement> ParseModule(XElement module)
  {
    var assemblyName = module.ElementByLocalName("ModuleName")?.Value ?? "<unknown-assembly>";
    var assemblyNode = CreateNode(CodeElementKind.Assembly, assemblyName, assemblyName, null);
    OpenCoverMetricMapper.PopulateSummaryMetrics(assemblyNode.Metrics, module.ElementByLocalName("Summary"));
    yield return assemblyNode;

    var files = BuildFileMap(module);

    foreach (var typeNode in ParseClasses(module, assemblyNode, files))
    {
      yield return typeNode;
    }
  }

  private static Dictionary<string, string> BuildFileMap(XElement module)
  {
    return module.ElementByLocalName("Files")?
               .ElementsByLocalName("File")
               .Select(file => new
               {
                 Id = file.AttributeByLocalName("uid")?.Value,
                 Path = file.AttributeByLocalName("fullPath")?.Value
               })
               .Where(file => file.Id is not null && file.Path is not null)
               .ToDictionary(file => file.Id!, file => file.Path!, StringComparer.OrdinalIgnoreCase)
           ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
  }

  private static IEnumerable<ParsedCodeElement> ParseClasses(
      XElement module,
      ParsedCodeElement assemblyNode,
      Dictionary<string, string> files)
  {
    var classesElement = module.ElementByLocalName("Classes");
    var classElements = classesElement?.ElementsByLocalName("Class")
                        ?? module.DescendantsByLocalName("Class");

    foreach (var classElement in classElements)
    {
      var classNode = CreateClassNode(classElement, assemblyNode);
      yield return classNode;

      foreach (var member in OpenCoverMethodParser.ParseMethods(classElement, classNode, files))
      {
        yield return member;
      }
    }
  }

  private static ParsedCodeElement CreateClassNode(XElement classElement, ParsedCodeElement assemblyNode)
  {
    var className = classElement.ElementByLocalName("FullName")?.Value ?? "<unknown-class>";
    var classNode = CreateNode(
        CodeElementKind.Type,
        className,
        NormalizeTypeName(className),
        assemblyNode.FullyQualifiedName);

    OpenCoverMetricMapper.PopulateSummaryMetrics(classNode.Metrics, classElement.ElementByLocalName("Summary"));
    return classNode;
  }

  private static ParsedCodeElement CreateNode(CodeElementKind kind, string name, string? fqn, string? parentFqn)
      => new(kind, name, fqn)
      {
        ParentFullyQualifiedName = parentFqn
      };

  private static string? NormalizeTypeName(string? fullName)
  {
    if (string.IsNullOrWhiteSpace(fullName))
    {
      return fullName;
    }

    // OpenCover uses Namespace.Type/Nested to describe nested types.
    return fullName.Replace('/', '+');
  }
}
