namespace MetricsReporter.MetricsReader.Services;

using System;

/// <summary>
/// Matches fully qualified names against namespace filters.
/// </summary>
internal static class NamespaceMatcher
{
  /// <summary>
  /// Checks if a fully qualified name matches a namespace filter.
  /// </summary>
  /// <param name="fullyQualifiedName">The fully qualified name to check.</param>
  /// <param name="namespaceFilter">The namespace filter to match against.</param>
  /// <returns><see langword="true"/> if the name matches the filter; otherwise, <see langword="false"/>.</returns>
  public static bool Matches(string? fullyQualifiedName, string namespaceFilter)
  {
    if (string.IsNullOrWhiteSpace(namespaceFilter))
    {
      return true;
    }

    if (string.IsNullOrWhiteSpace(fullyQualifiedName))
    {
      return false;
    }

    if (!fullyQualifiedName.StartsWith(namespaceFilter, StringComparison.Ordinal))
    {
      return false;
    }

    if (fullyQualifiedName.Length == namespaceFilter.Length)
    {
      return true;
    }

    var separator = fullyQualifiedName[namespaceFilter.Length];
    return separator == '.' || separator == '+' || separator == ':';
  }
}


