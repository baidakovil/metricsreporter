namespace MetricsReporter.Aggregation;
/// <summary>
/// Calculates deltas between current and baseline metric values.
/// </summary>
internal static class DeltaCalculator
{
  /// <summary>
  /// Calculates the delta between current and baseline values.
  /// </summary>
  /// <param name="currentValue">Current metric value.</param>
  /// <param name="baselineValue">Baseline metric value.</param>
  /// <returns>Delta value, or <see langword="null"/> if delta cannot be calculated or is zero.</returns>
  public static decimal? Calculate(decimal? currentValue, decimal? baselineValue)
  {
    if (!currentValue.HasValue || baselineValue is not decimal baseline)
    {
      return null;
    }
    var delta = currentValue.Value - baseline;
    return delta == 0 ? null : delta;
  }
}






