namespace MetricsReporter.Processing.Parsers;

using System.Globalization;
using System.Xml.Linq;

internal static class XmlExtensions
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

