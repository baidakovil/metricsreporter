namespace MetricsReporter.Processing.Parsers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using MetricsReporter.Model;
using MetricsReporter.Processing;

/// <summary>
/// Builds the ParsedMetricsDocument model from Roslyn code metrics XML.
/// </summary>
internal sealed class RoslynMetricsDocumentWalker
{
  private static readonly XNamespace XmlNamespace = XNamespace.None;

  private static readonly Dictionary<string, MetricIdentifier> MetricMap =
      new Dictionary<string, MetricIdentifier>(StringComparer.OrdinalIgnoreCase)
      {
        ["MaintainabilityIndex"] = MetricIdentifier.RoslynMaintainabilityIndex,
        ["CyclomaticComplexity"] = MetricIdentifier.RoslynCyclomaticComplexity,
        ["ClassCoupling"] = MetricIdentifier.RoslynClassCoupling,
        ["DepthOfInheritance"] = MetricIdentifier.RoslynDepthOfInheritance,
        ["SourceLines"] = MetricIdentifier.RoslynSourceLines,
        ["ExecutableLines"] = MetricIdentifier.RoslynExecutableLines
      };

  /// <summary>
  /// Converts the supplied XML document into an in-memory metrics representation.
  /// </summary>
  /// <param name="document">Roslyn metrics XML document.</param>
  /// <returns>A populated <see cref="ParsedMetricsDocument" /> instance.</returns>
  public static ParsedMetricsDocument Parse(XDocument document)
  {
    ArgumentNullException.ThrowIfNull(document);

    var (solutionName, elements) = ExtractDocumentModel(document);
    return new ParsedMetricsDocument
    {
      SolutionName = solutionName,
      Elements = elements
    };
  }

  private static (string SolutionName, List<ParsedCodeElement> Elements) ExtractDocumentModel(XDocument document)
  {
    var targetsParent = document
        .Element(XmlNamespace + "CodeMetricsReport")
        ?.Element(XmlNamespace + "Targets");

    var elements = new List<ParsedCodeElement>();
    var solutionName = string.Empty;

    if (targetsParent is null)
    {
      return (solutionName, elements);
    }

    foreach (var target in targetsParent.Elements(XmlNamespace + "Target"))
    {
      solutionName = target.Attribute("Name")?.Value ?? solutionName;

      if (target.Element(XmlNamespace + "Assembly") is { } assemblyElement)
      {
        elements.AddRange(ParseAssembly(assemblyElement));
      }
    }

    return (solutionName, elements);
  }

  private static IEnumerable<ParsedCodeElement> ParseAssembly(XElement assemblyElement)
  {
    var assemblyNode = RoslynAssemblyNodeFactory.Create(assemblyElement);
    yield return assemblyNode;

    foreach (var namespaceElement in RoslynNamespaceElementReader.ReadNamespaces(assemblyElement))
    {
      foreach (var parsedNamespace in ParseNamespace(namespaceElement, assemblyNode))
      {
        yield return parsedNamespace;
      }
    }
  }

  private static IEnumerable<ParsedCodeElement> ParseNamespace(XElement namespaceElement, ParsedCodeElement assemblyNode)
  {
    var namespaceNode = RoslynNamespaceNodeFactory.Create(namespaceElement, assemblyNode, out var namespaceName);
    yield return namespaceNode;

    var typesElement = namespaceElement.Element(XmlNamespace + "Types");
    if (typesElement is null)
    {
      yield break;
    }

    var assemblyName = assemblyNode.FullyQualifiedName ?? assemblyNode.Name;
    foreach (var typeElement in typesElement.Elements())
    {
      foreach (var typeNode in ParseType(typeElement, namespaceNode, namespaceName, assemblyName))
      {
        yield return typeNode;
      }
    }
  }

  private static IEnumerable<ParsedCodeElement> ParseType(
      XElement typeElement,
      ParsedCodeElement namespaceNode,
      string namespaceName,
      string? assemblyName)
  {
    var typeNode = RoslynTypeNodeFactory.Create(typeElement, namespaceNode, namespaceName, assemblyName);
    yield return typeNode;

    foreach (var memberNode in RoslynMemberElementParser.CreateMembers(typeElement, typeNode))
    {
      yield return memberNode;
    }
  }

  private static Dictionary<MetricIdentifier, MetricValue> ExtractMetrics(XElement? metricsElement)
  {
    return RoslynMetricSectionParser.Parse(metricsElement);
  }

  private static class RoslynAssemblyNodeFactory
  {
    internal static ParsedCodeElement Create(XElement assemblyElement)
    {
      var assemblyName = assemblyElement.Attribute("Name")?.Value ?? "<unknown-assembly>";
      var assemblyFqn = ExtractAssemblyShortName(assemblyName);

      return new ParsedCodeElement(CodeElementKind.Assembly, assemblyFqn, assemblyFqn)
      {
        Metrics = ExtractMetrics(assemblyElement.Element(XmlNamespace + "Metrics"))
      };
    }
  }

  private static class RoslynNamespaceElementReader
  {
    internal static IEnumerable<XElement> ReadNamespaces(XElement assemblyElement)
    {
      return assemblyElement.Element(XmlNamespace + "Namespaces")?.Elements(XmlNamespace + "Namespace")
             ?? Enumerable.Empty<XElement>();
    }
  }

  private static class RoslynNamespaceNodeFactory
  {
    internal static ParsedCodeElement Create(
        XElement namespaceElement,
        ParsedCodeElement assemblyNode,
        out string namespaceName)
    {
      namespaceName = namespaceElement.Attribute("Name")?.Value ?? "<global>";
      return new ParsedCodeElement(CodeElementKind.Namespace, namespaceName, namespaceName)
      {
        ParentFullyQualifiedName = assemblyNode.FullyQualifiedName,
        ContainingAssemblyName = assemblyNode.FullyQualifiedName ?? assemblyNode.Name,
        Metrics = ExtractMetrics(namespaceElement.Element(XmlNamespace + "Metrics"))
      };
    }
  }

  private static class RoslynTypeNodeFactory
  {
    internal static ParsedCodeElement Create(
        XElement typeElement,
        ParsedCodeElement namespaceNode,
        string namespaceName,
        string? assemblyName)
    {
      var typeName = typeElement.Attribute("Name")?.Value ?? "<unknown-type>";
      var typeFqn = BuildTypeFqn(namespaceName, typeName);
      var source = CreateSourceLocation(typeElement.Attribute("File")?.Value, typeElement.Attribute("Line")?.Value);

      return new ParsedCodeElement(CodeElementKind.Type, typeName, typeFqn)
      {
        ParentFullyQualifiedName = namespaceNode.FullyQualifiedName,
        ContainingAssemblyName = assemblyName,
        Metrics = ExtractMetrics(typeElement.Element(XmlNamespace + "Metrics")),
        Source = source
      };
    }

    private static string BuildTypeFqn(string namespaceName, string typeName)
    {
      // Normalize generic type parameters to ensure consistent FQN format
      // This ensures that types like "MetricsReaderCommandBase<TSettings>" are normalized
      // to "MetricsReaderCommandBase" for consistent matching with suppressions
      var normalizedTypeName = SymbolNormalizer.NormalizeTypeName(typeName) ?? typeName;

      if (string.IsNullOrWhiteSpace(namespaceName) || namespaceName == "<global>")
      {
        return normalizedTypeName;
      }

      return $"{namespaceName}.{normalizedTypeName}";
    }
  }

  private static class RoslynMemberElementParser
  {
    internal static IEnumerable<ParsedCodeElement> CreateMembers(XElement typeElement, ParsedCodeElement typeNode)
    {
      var members = typeElement.Element(XmlNamespace + "Members")?.Elements() ?? Enumerable.Empty<XElement>();
      foreach (var memberNode in members.Select(member => ParseMember(member, typeNode)))
      {
        yield return memberNode;
      }
    }

    private static ParsedCodeElement ParseMember(XElement memberElement, ParsedCodeElement typeNode)
    {
      return RoslynMemberNodeFactory.Create(memberElement, typeNode);
    }
  }

  private static class RoslynMemberNodeFactory
  {
    internal static ParsedCodeElement Create(XElement memberElement, ParsedCodeElement typeNode)
    {
      var context = RoslynMemberContextFactory.Create(memberElement, typeNode);

      return new ParsedCodeElement(CodeElementKind.Member, context.MethodDisplayName, context.NormalizedMemberFqn)
      {
        ParentFullyQualifiedName = typeNode.FullyQualifiedName,
        ContainingAssemblyName = typeNode.ContainingAssemblyName,
        Metrics = context.Metrics,
        Source = context.Source
      };
    }

    private static class RoslynMemberContextFactory
    {
      internal static RoslynMemberContext Create(XElement memberElement, ParsedCodeElement typeNode)
      {
        var memberName = memberElement.Attribute("Name")?.Value ?? "<unknown-member>";
        var memberDisplayName = ExtractMemberDisplayName(memberName);

        var memberFqn = BuildMemberFqn(typeNode.FullyQualifiedName, memberDisplayName, typeNode.Name);
        var normalizedMemberFqn = SymbolNormalizer.NormalizeFullyQualifiedMethodName(memberFqn);

        var methodNameOnly = ExtractDisplayMethodName(memberName, memberDisplayName, typeNode.Name);
        var source = CreateSourceLocation(memberElement.Attribute("File")?.Value, memberElement.Attribute("Line")?.Value);

        var metrics = ExtractMetrics(memberElement.Element(XmlNamespace + "Metrics"));
        return new RoslynMemberContext(methodNameOnly, normalizedMemberFqn, source, metrics);
      }
    }

    private readonly record struct RoslynMemberContext(
        string MethodDisplayName,
        string? NormalizedMemberFqn,
        SourceLocation? Source,
        Dictionary<MetricIdentifier, MetricValue> Metrics);

    private static string ExtractDisplayMethodName(string rawMemberName, string memberDisplayName, string typeName)
    {
      var typeNameDot = typeName + ".";
      if (memberDisplayName.StartsWith(typeNameDot, StringComparison.Ordinal))
      {
        var afterTypeNameDot = memberDisplayName[typeNameDot.Length..];
        if (afterTypeNameDot.StartsWith(typeName + "(", StringComparison.Ordinal))
        {
          return typeName;
        }
      }

      return SymbolNormalizer.ExtractMethodName(rawMemberName)
          ?? SymbolNormalizer.ExtractMethodName(memberDisplayName)
          ?? memberDisplayName;
    }

    private static string ExtractMemberDisplayName(string rawName)
    {
      var depth = 0;
      var angleDepth = 0;
      var spaceIndex = -1;

      for (var i = 0; i < rawName.Length; i++)
      {
        var ch = rawName[i];

        if (ch == '<')
        {
          angleDepth++;
        }
        else if (ch == '>')
        {
          angleDepth--;
        }
        else if (ch == '(' && angleDepth == 0)
        {
          depth++;
        }
        else if (ch == ')' && angleDepth == 0)
        {
          depth--;
        }
        else if (ch == ' ' && depth == 0 && angleDepth == 0)
        {
          spaceIndex = i;
          break;
        }
      }

      return spaceIndex >= 0 ? rawName[(spaceIndex + 1)..] : rawName;
    }

    private static string? BuildMemberFqn(string? typeFqn, string memberDisplayName, string typeName)
    {
      if (typeFqn is null)
      {
        return memberDisplayName;
      }

      var suffix = memberDisplayName;

      var fullTypePrefix = typeName + ".";
      if (memberDisplayName.StartsWith(fullTypePrefix, StringComparison.Ordinal))
      {
        suffix = memberDisplayName[fullTypePrefix.Length..];
      }
      else
      {
        var lastDotIndex = typeName.LastIndexOf('.');
        var simpleTypeName = lastDotIndex >= 0 ? typeName[(lastDotIndex + 1)..] : typeName;
        var simpleTypePrefix = simpleTypeName + ".";

        if (memberDisplayName.StartsWith(simpleTypePrefix, StringComparison.Ordinal))
        {
          suffix = memberDisplayName[simpleTypePrefix.Length..];
        }
      }

      return $"{typeFqn}.{suffix}";
    }

  }

  private static string ExtractAssemblyShortName(string assemblyDisplayName)
  {
    var commaIndex = assemblyDisplayName.IndexOf(',', StringComparison.Ordinal);
    return commaIndex >= 0 ? assemblyDisplayName[..commaIndex] : assemblyDisplayName;
  }

  private static SourceLocation? CreateSourceLocation(string? file, string? line)
  {
    if (string.IsNullOrWhiteSpace(file))
    {
      return null;
    }

    int? lineNumber = null;
    if (int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLine))
    {
      lineNumber = parsedLine;
    }

    return new SourceLocation
    {
      Path = file,
      StartLine = lineNumber,
      EndLine = lineNumber
    };
  }

  private static class RoslynMetricSectionParser
  {
    internal static Dictionary<MetricIdentifier, MetricValue> Parse(XElement? metricsElement)
    {
      var metrics = new Dictionary<MetricIdentifier, MetricValue>();
      if (metricsElement is null)
      {
        return metrics;
      }

      foreach (var metricElement in metricsElement.Elements(XmlNamespace + "Metric"))
      {
        if (!TryGetMetricIdentifier(metricElement, out var identifier))
        {
          continue;
        }

        if (!RoslynMetricValueParser.TryParse(metricElement.Attribute("Value")?.Value, out var numericValue))
        {
          continue;
        }

        metrics[identifier] = MetricValueFactory.Create(numericValue);
      }

      return metrics;
    }

    private static bool TryGetMetricIdentifier(XElement metricElement, out MetricIdentifier identifier)
    {
      var name = metricElement.Attribute("Name")?.Value;
      if (name is null)
      {
        identifier = default;
        return false;
      }

      return MetricMap.TryGetValue(name, out identifier);
    }
  }

  private static class RoslynMetricValueParser
  {
    internal static bool TryParse(string? valueAttribute, out decimal value)
    {
      if (valueAttribute is null)
      {
        value = default;
        return false;
      }

      return decimal.TryParse(valueAttribute, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
  }

  private static class MetricValueFactory
  {
    internal static MetricValue Create(decimal numericValue)
    {
      return new MetricValue
      {
        Value = numericValue,
        Status = ThresholdStatus.NotApplicable
      };
    }
  }
}


