namespace MetricsReporter.Processing.Parsers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

/// <summary>
/// Provides namespace-agnostic XML helpers for OpenCover parsing.
/// </summary>
internal static class OpenCoverXmlExtensions
{
  internal static XElement? ElementByLocalName(this XElement element, string localName)
    => element.Elements().FirstOrDefault(child => MatchesLocalName(child, localName));

  internal static IEnumerable<XElement> ElementsByLocalName(this XElement element, string localName)
    => element.Elements().Where(child => MatchesLocalName(child, localName));

  internal static IEnumerable<XElement> DescendantsByLocalName(this XElement element, string localName)
    => element.Descendants().Where(child => MatchesLocalName(child, localName));

  internal static XAttribute? AttributeByLocalName(this XElement element, string localName)
    => element.Attributes().FirstOrDefault(attribute => MatchesLocalName(attribute, localName));

  private static bool MatchesLocalName(XElement element, string localName)
    => string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase);

  private static bool MatchesLocalName(XAttribute attribute, string localName)
    => string.Equals(attribute.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase);
}
