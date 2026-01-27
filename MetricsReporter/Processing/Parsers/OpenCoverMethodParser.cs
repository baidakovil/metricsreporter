namespace MetricsReporter.Processing.Parsers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using MetricsReporter.Model;

/// <summary>
/// Parses method entries within OpenCover class elements.
/// </summary>
internal static class OpenCoverMethodParser
{
  internal static IEnumerable<ParsedCodeElement> ParseMethods(
      XElement classElement,
      ParsedCodeElement classNode,
      Dictionary<string, string> files)
  {
    var methods = classElement.ElementByLocalName("Methods")?.ElementsByLocalName("Method")
                  ?? Enumerable.Empty<XElement>();

    foreach (var method in methods)
    {
      yield return ParseMethod(method, classNode, files);
    }
  }

  private static ParsedCodeElement ParseMethod(
      XElement methodElement,
      ParsedCodeElement classNode,
      Dictionary<string, string> files)
  {
    var memberNode = OpenCoverMethodNodeFactory.Create(methodElement, classNode, files);
    OpenCoverMetricMapper.PopulateMethodMetrics(memberNode.Metrics, methodElement);

    return memberNode;
  }

  private static SourceLocation? ResolveSourceLocation(XElement methodElement, Dictionary<string, string> files)
  {
    var fileRef = methodElement.ElementByLocalName("FileRef");
    var fileId = fileRef?.AttributeByLocalName("uid")?.Value;
    if (fileId is null || !files.TryGetValue(fileId, out var path))
    {
      return null;
    }

    var sequencePoints = methodElement.ElementByLocalName("SequencePoints")?.ElementsByLocalName("SequencePoint");
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

  private static ParsedCodeElement CreateNode(CodeElementKind kind, string name, string? fqn, string? parentFqn, SourceLocation? source, MemberKind memberKind = MemberKind.Unknown)
      => new(kind, name, fqn)
      {
        ParentFullyQualifiedName = parentFqn,
        Source = source,
        MemberKind = memberKind
      };

  private static class OpenCoverMethodNodeFactory
  {
    internal static ParsedCodeElement Create(
        XElement methodElement,
        ParsedCodeElement classNode,
        Dictionary<string, string> files)
    {
    var methodName = methodElement.ElementByLocalName("Name")?.Value ?? "<unknown-method>";
      var methodNameForExtraction = methodName.Replace("::", ".", StringComparison.Ordinal);
      var normalizedMethodFqn = NormalizeMethodName(methodName, classNode.FullyQualifiedName);
      var methodDisplayName = SymbolNormalizer.ExtractMethodName(methodNameForExtraction) ?? "<unknown-method>";
      var sourceLocation = ResolveSourceLocation(methodElement, files);

      return CreateNode(
          CodeElementKind.Member,
          methodDisplayName,
          normalizedMethodFqn,
          classNode.FullyQualifiedName,
          sourceLocation,
          MemberKind.Method);
    }
  }

  /// <summary>
  /// Normalizes a method name from OpenCover format to a unified format.
  /// </summary>
  /// <param name="methodName">The method name from OpenCover (e.g., "void Namespace.Type.Method(System.Object, System.String)" or "void Method(System.Object)").</param>
  /// <param name="typeFqn">The fully qualified name of the declaring type (e.g., "Namespace.Type").</param>
  /// <returns>
  /// Normalized fully qualified method name with parameters replaced by "..." (e.g., "Namespace.Type.Method(...)").
  /// </returns>
  /// <remarks>
  /// OpenCover provides method names in the format: "ReturnType FullMethodSignature(ParameterTypes)".
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
