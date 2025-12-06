namespace MetricsReporter.MetricsReader.Services;

using MetricsReporter.Model;

/// <summary>
/// Checks if symbols are suppressed for specific metrics or rules.
/// </summary>
internal interface ISuppressedSymbolChecker
{
  /// <summary>
  /// Checks if a symbol is suppressed for the given metric and optional rule ID.
  /// </summary>
  /// <param name="fullyQualifiedName">The fully qualified name of the symbol.</param>
  /// <param name="metric">The metric identifier.</param>
  /// <param name="ruleId">Optional rule ID for SARIF-based suppressions.</param>
  /// <returns><see langword="true"/> if the symbol is suppressed; otherwise, <see langword="false"/>.</returns>
  bool IsSuppressed(string? fullyQualifiedName, MetricIdentifier metric, string? ruleId = null);
}


