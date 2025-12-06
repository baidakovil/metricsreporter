namespace MetricsReporter.Processing.Parsers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
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
    await using var stream = System.IO.File.OpenRead(path);
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
    var assemblyNode = CreateNode(CodeElementKind.Assembly, assemblyName, assemblyName, null, null);
    PopulateMetrics(assemblyNode.Metrics, module.Element(XmlNamespace + "Summary"));
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

      foreach (var member in ParseMethods(classElement, classNode, files))
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
        assemblyNode.FullyQualifiedName,
        null);

    PopulateMetrics(classNode.Metrics, classElement.Element(XmlNamespace + "Summary"));
    return classNode;
  }

  private static IEnumerable<ParsedCodeElement> ParseMethods(
      XElement classElement,
      ParsedCodeElement classNode,
      Dictionary<string, string> files)
  {
    var methods = classElement.Element(XmlNamespace + "Methods")?.Elements(XmlNamespace + "Method")
                  ?? Enumerable.Empty<XElement>();

    foreach (var method in methods)
    {
      yield return ParseMethod(method, classNode, files);
    }
  }

  /// <summary>
  /// Parses a single method element from AltCover XML into a parsed code element.
  /// </summary>
  /// <param name="methodElement">The XML element representing the method.</param>
  /// <param name="classNode">The parsed code element representing the containing class.</param>
  /// <param name="files">Dictionary mapping file IDs to file paths.</param>
  /// <returns>A parsed code element representing the method.</returns>
  private static ParsedCodeElement ParseMethod(
      XElement methodElement,
      ParsedCodeElement classNode,
      Dictionary<string, string> files)
  {
    var memberNode = AltCoverMethodNodeFactory.Create(methodElement, classNode, files);
    PopulateMethodMetrics(memberNode.Metrics, methodElement);

    return memberNode;
  }

  private static class AltCoverMethodNodeFactory
  {
    internal static ParsedCodeElement Create(
        XElement methodElement,
        ParsedCodeElement classNode,
        Dictionary<string, string> files)
    {
      var methodName = methodElement.Element(XmlNamespace + "Name")?.Value ?? "<unknown-method>";
      var methodNameForExtraction = methodName.Replace("::", ".", StringComparison.Ordinal);
      var normalizedMethodFqn = NormalizeMethodName(methodName, classNode.FullyQualifiedName);
      var methodDisplayName = SymbolNormalizer.ExtractMethodName(methodNameForExtraction) ?? "<unknown-method>";
      var sourceLocation = ResolveSourceLocation(methodElement, files);

      return CreateNode(
          CodeElementKind.Member,
          methodDisplayName,
          normalizedMethodFqn,
          classNode.FullyQualifiedName,
          sourceLocation);
    }
  }

  private static ParsedCodeElement CreateNode(CodeElementKind kind, string name, string? fqn, string? parentFqn, SourceLocation? source)
      => new(kind, name, fqn)
      {
        ParentFullyQualifiedName = parentFqn,
        Source = source
      };

  private static SourceLocation? ResolveSourceLocation(XElement methodElement, Dictionary<string, string> files)
  {
    var fileRef = methodElement.Element(XmlNamespace + "FileRef");
    var fileId = fileRef?.Attribute("uid")?.Value;
    if (fileId is null || !files.TryGetValue(fileId, out var path))
    {
      return null;
    }

    var sequencePoints = methodElement.Element(XmlNamespace + "SequencePoints")?.Elements(XmlNamespace + "SequencePoint");
    if (sequencePoints is null || !sequencePoints.Any())
    {
      return new SourceLocation { Path = path };
    }

    var minLine = sequencePoints.Min(SeqStartLine);
    var maxLine = sequencePoints.Max(SeqEndLine);

    return new SourceLocation
    {
      Path = path,
      StartLine = minLine,
      EndLine = maxLine
    };
  }

  private static int SeqStartLine(XElement point) => (int)(point.Attribute("sl")?.GetDecimalValue() ?? 0m);
  private static int SeqEndLine(XElement point) => (int)(point.Attribute("el")?.GetDecimalValue() ?? 0m);

  private static void PopulateMetrics(IDictionary<MetricIdentifier, MetricValue> target, XElement? summary)
  {
    if (summary is null)
    {
      return;
    }

    AddMetric(target, MetricIdentifier.AltCoverSequenceCoverage, summary.Attribute("sequenceCoverage"));

    // WHY: Branch coverage is only applicable when there are actual branch points to measure.
    // If numBranchPoints is 0 or missing, branch coverage should not be included in the report
    // to avoid misleading 0% coverage values for code that has no branches.
    var numBranchPoints = summary.Attribute("numBranchPoints")?.GetDecimalValue();
    if (numBranchPoints.HasValue && numBranchPoints.Value > 0)
    {
      AddMetric(target, MetricIdentifier.AltCoverBranchCoverage, summary.Attribute("branchCoverage"));
    }

    AddMetric(target, MetricIdentifier.AltCoverCyclomaticComplexity, summary.Attribute("maxCyclomaticComplexity"));
    AddMetric(target, MetricIdentifier.AltCoverNPathComplexity, summary.Attribute("maxNPathComplexity"));
  }

  private static void PopulateMethodMetrics(IDictionary<MetricIdentifier, MetricValue> target, XElement method)
  {
    AddMetric(target, MetricIdentifier.AltCoverSequenceCoverage, method.Attribute("sequenceCoverage"));

    // WHY: Branch coverage is only applicable when there are actual BranchPoint elements to measure.
    // If the BranchPoints element is empty or missing, branch coverage should not be included
    // to avoid misleading 0% coverage values for methods that have no branches (e.g., simple getters,
    // methods with only linear code paths). This prevents false warnings when sequence coverage is 100%
    // but branch coverage shows 0% due to the absence of branches rather than uncovered branches.
    var branchPoints = method.Element(XmlNamespace + "BranchPoints");
    if (branchPoints is not null && branchPoints.Elements(XmlNamespace + "BranchPoint").Any())
    {
      AddMetric(target, MetricIdentifier.AltCoverBranchCoverage, method.Attribute("branchCoverage"));
    }

    AddMetric(target, MetricIdentifier.AltCoverCyclomaticComplexity, method.Attribute("cyclomaticComplexity"));
    AddMetric(target, MetricIdentifier.AltCoverNPathComplexity, method.Attribute("nPathComplexity"));
  }

  private static void AddMetric(IDictionary<MetricIdentifier, MetricValue> target, MetricIdentifier identifier, XAttribute? attribute)
  {
    if (attribute is null)
    {
      return;
    }

    var value = attribute.GetDecimalValue();
    if (value is null)
    {
      return;
    }

    target[identifier] = new MetricValue
    {
      Value = value,
      Status = ThresholdStatus.NotApplicable
    };
  }

  private static string? NormalizeTypeName(string? fullName)
  {
    if (string.IsNullOrWhiteSpace(fullName))
    {
      return fullName;
    }

    // AltCover uses Namespace.Type/Nested to describe nested types.
    return fullName.Replace('/', '+');
  }

  /// <summary>
  /// Normalizes a method name from AltCover format to a unified format.
  /// </summary>
  /// <param name="methodName">The method name from AltCover (e.g., "void Namespace.Type.Method(System.Object, System.String)" or "void Method(System.Object)").</param>
  /// <param name="typeFqn">The fully qualified name of the declaring type (e.g., "Namespace.Type").</param>
  /// <returns>
  /// Normalized fully qualified method name with parameters replaced by "..." (e.g., "Namespace.Type.Method(...)").
  /// </returns>
  /// <remarks>
  /// AltCover provides method names in the format: "ReturnType FullMethodSignature(ParameterTypes)".
  /// The method signature may or may not include the full type path:
  /// - With full path: "void Sample.Loader.LoaderApp.OnApplicationIdling(System.Object, ...)"
  /// - Without full path: "void OnApplicationIdling(System.Object, ...)"
  /// 
  /// This method:
  /// 1. Removes the return type prefix
  /// 2. Replaces C++ style "::" with "." and "/" with "+" for nested types
  /// 3. Normalizes the method signature using SymbolNormalizer to replace parameters with "..."
  /// 4. If the signature doesn't include the type path and typeFqn is provided, prepends it
  /// </remarks>
  private static string? NormalizeMethodName(string? methodName, string? typeFqn)
  {
    if (string.IsNullOrWhiteSpace(methodName))
    {
      return methodName;
    }

    // Remove return type prefix (format: "ReturnType Method(...)")
    var spaceIndex = methodName.IndexOf(' ');
    var signature = spaceIndex >= 0 ? methodName[(spaceIndex + 1)..] : methodName;

    // Replace C++ style operators with C# style
    signature = signature.Replace("::", ".", StringComparison.Ordinal);
    signature = signature.Replace('/', '+');

    // Find where the method name starts (before parameters)
    var paramStart = signature.IndexOf('(');
    if (paramStart < 0)
    {
      // No parameters, check if we need to prepend type
      if (!string.IsNullOrWhiteSpace(typeFqn) && !signature.StartsWith(typeFqn + ".", StringComparison.Ordinal))
      {
        return $"{typeFqn}.{signature}";
      }
      return signature;
    }

    // Normalize the method signature (replace parameters with "...")
    var normalizedSignature = SymbolNormalizer.NormalizeFullyQualifiedMethodName(signature);

    if (normalizedSignature is null)
    {
      return null;
    }

    // Extract the part before parameters to check if it includes the type path
    var normalizedParamStart = normalizedSignature.IndexOf('(');
    if (normalizedParamStart < 0)
    {
      // No parameters in normalized signature (shouldn't happen, but handle it)
      return normalizedSignature;
    }

    var normalizedPrefix = normalizedSignature[..normalizedParamStart];

    // Check if the normalized signature already includes the type path
    // We check if it starts with typeFqn followed by a dot
    var hasTypePath = !string.IsNullOrWhiteSpace(typeFqn) &&
                      normalizedPrefix.StartsWith(typeFqn + ".", StringComparison.Ordinal);

    // If the signature doesn't include the type path and we have typeFqn, prepend it
    if (!hasTypePath && !string.IsNullOrWhiteSpace(typeFqn))
    {
      // Extract just the method name (everything after the last dot, or the whole thing if no dot)
      var methodNameOnly = ExtractMethodNameFromSignature(normalizedPrefix);
      return $"{typeFqn}.{methodNameOnly}(...)";
    }

    return normalizedSignature;
  }

  /// <summary>
  /// Extracts the method name from a signature prefix (the part before parameters).
  /// </summary>
  /// <param name="signaturePrefix">The signature prefix (e.g., "Namespace.Type.Method" or "Method").</param>
  /// <returns>The method name (e.g., "Method").</returns>
  private static string ExtractMethodNameFromSignature(string signaturePrefix)
  {
    // Handle generic methods: Method&lt;T&gt;
    var genericStart = signaturePrefix.IndexOf('<');
    if (genericStart >= 0)
    {
      signaturePrefix = signaturePrefix[..genericStart];
    }

    // Extract method name (after last dot, or the whole thing if no dot)
    var lastDot = signaturePrefix.LastIndexOf('.');
    return lastDot >= 0 ? signaturePrefix[(lastDot + 1)..] : signaturePrefix;
  }
}

file static class XmlExtensions
{
  public static decimal? GetDecimalValue(this XAttribute? attribute)
  {
    if (attribute?.Value is null)
    {
      return null;
    }

    return decimal.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
        ? value
        : null;
  }
}

