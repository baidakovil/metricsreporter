namespace MetricsReporter.MetricsReader.Settings;

/// <summary>
/// Helper extensions for <see cref="MetricsReaderGroupByOption"/>.
/// </summary>
internal static class MetricsReaderGroupByOptionExtensions
{
  /// <summary>
  /// Returns the CLI-friendly value for the provided option.
  /// </summary>
  public static string ToWireValue(this MetricsReaderGroupByOption option)
    => option switch
    {
      MetricsReaderGroupByOption.Metric => "metric",
      MetricsReaderGroupByOption.Namespace => "namespace",
      MetricsReaderGroupByOption.Type => "type",
      MetricsReaderGroupByOption.Method => "method",
      MetricsReaderGroupByOption.RuleId => "ruleId",
      _ => "none"
    };
}

