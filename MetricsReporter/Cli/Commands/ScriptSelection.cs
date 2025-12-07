using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Configuration;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Selects scripts to run based on the requested metrics and script configuration.
/// </summary>
internal static class ScriptSelection
{
  /// <summary>
  /// Selects scripts applicable to the read command.
  /// </summary>
  /// <param name="scripts">Resolved scripts.</param>
  /// <param name="metrics">Metrics requested for the command.</param>
  /// <returns>Scripts to run.</returns>
  public static string[] SelectReadScripts(ResolvedScripts scripts, IEnumerable<string> metrics)
  {
    return SelectByMetric(scripts.ReadAny, scripts.ReadByMetric, metrics);
  }

  /// <summary>
  /// Selects scripts applicable to the test command.
  /// </summary>
  /// <param name="scripts">Resolved scripts.</param>
  /// <param name="metrics">Metrics requested for the command.</param>
  /// <returns>Scripts to run.</returns>
  public static string[] SelectTestScripts(ResolvedScripts scripts, IEnumerable<string> metrics)
  {
    return SelectByMetric(scripts.TestAny, scripts.TestByMetric, metrics);
  }

  private static string[] SelectByMetric(
    IEnumerable<string> genericScripts,
    IEnumerable<MetricScript> metricScripts,
    IEnumerable<string> metrics)
  {
    var metricSet = new HashSet<string>(metrics, StringComparer.OrdinalIgnoreCase);
    var matchedMetricScripts = metricScripts
      .Where(entry => entry.Path is not null && entry.Metrics.Any(metric => metricSet.Contains(metric)))
      .Select(entry => entry.Path!)
      .ToArray();

    return genericScripts.Concat(matchedMetricScripts).ToArray();
  }
}

