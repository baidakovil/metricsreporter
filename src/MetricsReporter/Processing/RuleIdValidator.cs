namespace MetricsReporter.Processing;

using System.Text.RegularExpressions;

/// <summary>
/// Validates rule identifiers for SARIF metrics (CA and IDE rules).
/// </summary>
internal static class RuleIdValidator
{
  /// <summary>
  /// Regular expression pattern for CA rules: CA followed by exactly 4 digits.
  /// </summary>
  private static readonly Regex CaRulePattern = new(@"^CA\d{4}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

  /// <summary>
  /// Regular expression pattern for IDE rules: IDE followed by exactly 4 digits.
  /// </summary>
  private static readonly Regex IdeRulePattern = new(@"^IDE\d{4}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

  /// <summary>
  /// Validates that a rule identifier matches the expected format for CA or IDE rules.
  /// </summary>
  /// <param name="ruleId">The rule identifier to validate (e.g., <c>CA1502</c>, <c>IDE0051</c>).</param>
  /// <returns>
  /// <see langword="true"/> if the rule ID matches the pattern <c>CA####</c> or <c>IDE####</c>
  /// where <c>####</c> is a 4-digit number; otherwise, <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// This validation ensures that only properly formatted rule IDs are stored in the breakdown.
  /// Invalid rule IDs are silently rejected to prevent schema violations.
  /// </remarks>
  public static bool IsValidRuleId(string? ruleId)
  {
    if (string.IsNullOrWhiteSpace(ruleId))
    {
      return false;
    }

    return CaRulePattern.IsMatch(ruleId) || IdeRulePattern.IsMatch(ruleId);
  }
}


