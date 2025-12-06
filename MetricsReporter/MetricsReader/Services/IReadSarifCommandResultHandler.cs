namespace MetricsReporter.MetricsReader.Services;

using System.Collections.Generic;
using MetricsReporter.MetricsReader.Settings;

/// <summary>
/// Handles result formatting and output for ReadSarif command.
/// </summary>
internal interface IReadSarifCommandResultHandler
{
  /// <summary>
  /// Writes an error message when a metric does not expose SARIF rule breakdown data.
  /// </summary>
  /// <param name="metricName">The metric name that was invalid.</param>
  void WriteInvalidMetricError(string metricName);

  /// <summary>
  /// Writes a message when no violations are found.
  /// </summary>
  /// <param name="metricName">The metric name.</param>
  /// <param name="namespace">The namespace filter.</param>
  /// <param name="symbolKind">The symbol kind.</param>
  /// <param name="ruleId">The optional rule ID filter.</param>
  void WriteNoViolationsFound(string metricName, string @namespace, string symbolKind, string? ruleId);

  /// <summary>
  /// Writes the violation groups response.
  /// </summary>
  /// <param name="settings">The command settings.</param>
  /// <param name="groups">The violation groups to write.</param>
  void WriteResponse(SarifMetricSettings settings, IEnumerable<SarifViolationGroup> groups);
}


