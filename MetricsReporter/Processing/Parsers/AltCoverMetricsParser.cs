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
/// Parses AltCover/OpenCover XML reports.
/// </summary>
public sealed class AltCoverMetricsParser : IMetricsSourceParser
{
  private static readonly XNamespace XmlNamespace = XNamespace.None;

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
    var coverageSession = ExtractCoverageSession(document);
    var modules = ExtractModules(coverageSession);
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
  /// Extracts the CoverageSession root element from the XML document.
  /// </summary>
  /// <param name="document">The XML document to extract from.</param>
  /// <returns>The CoverageSession element.</returns>
  /// <exception cref="InvalidOperationException">Thrown when CoverageSession root element is not found.</exception>
  private static XElement ExtractCoverageSession(XDocument document)
  {
    return document.Element(XmlNamespace + "CoverageSession")
           ?? throw new InvalidOperationException("CoverageSession root element not found.");
  }

  /// <summary>
  /// Extracts module elements from the CoverageSession element.
  /// </summary>
  /// <param name="coverageSession">The CoverageSession element containing modules.</param>
  /// <returns>Enumerable collection of Module elements.</returns>
  private static IEnumerable<XElement> ExtractModules(XElement coverageSession)
  {
    return coverageSession
        .Element(XmlNamespace + "Modules")
        ?.Elements(XmlNamespace + "Module")
        ?? Enumerable.Empty<XElement>();
  }

  private static IEnumerable<ParsedCodeElement> ParseModule(XElement module)
  {
    var assemblyName = module.Element(XmlNamespace + "ModuleName")?.Value ?? "<unknown-assembly>";
    var assemblyNode = CreateNode(CodeElementKind.Assembly, assemblyName, assemblyName, null);
    AltCoverMetricMapper.PopulateSummaryMetrics(assemblyNode.Metrics, module.Element(XmlNamespace + "Summary"));
    yield return assemblyNode;

    var files = BuildFileMap(module);

    foreach (var typeNode in ParseClasses(module, assemblyNode, files))
    {
      yield return typeNode;
    }
  }

  private static Dictionary<string, string> BuildFileMap(XElement module)
  {
    return module.Element(XmlNamespace + "Files")?
               .Elements(XmlNamespace + "File")
               .Select(file => new
               {
                 Id = file.Attribute("uid")?.Value,
                 Path = file.Attribute("fullPath")?.Value
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
    var classesElement = module.Element(XmlNamespace + "Classes");
    if (classesElement is null)
    {
      yield break;
    }

    foreach (var classElement in classesElement.Elements(XmlNamespace + "Class"))
    {
      var classNode = CreateClassNode(classElement, assemblyNode);
      yield return classNode;

      foreach (var member in AltCoverMethodParser.ParseMethods(classElement, classNode, files, XmlNamespace))
      {
        yield return member;
      }
    }
  }

  private static ParsedCodeElement CreateClassNode(XElement classElement, ParsedCodeElement assemblyNode)
  {
    var className = classElement.Element(XmlNamespace + "FullName")?.Value ?? "<unknown-class>";
    var classNode = CreateNode(
        CodeElementKind.Type,
        className,
        NormalizeTypeName(className),
        assemblyNode.FullyQualifiedName);

    AltCoverMetricMapper.PopulateSummaryMetrics(classNode.Metrics, classElement.Element(XmlNamespace + "Summary"));
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

    // AltCover uses Namespace.Type/Nested to describe nested types.
    return fullName.Replace('/', '+');
  }
}

