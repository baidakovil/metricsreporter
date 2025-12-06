namespace MetricsReporter.Processing;

using System.Collections.Generic;
using MetricsReporter.Model;

/// <summary>
/// Represents the parsing result of a single metrics source.
/// </summary>
public sealed class ParsedMetricsDocument
{
  /// <summary>
  /// Solution name supplied by the source or by command line arguments.
  /// </summary>
  public string SolutionName { get; init; } = string.Empty;

  /// <summary>
  /// Code elements discovered in the source.
  /// </summary>
  public IList<ParsedCodeElement> Elements { get; init; } = [];

  /// <summary>
  /// Rule descriptions extracted from SARIF files.
  /// Keyed by rule ID (e.g., "CA1502", "IDE0051").
  /// Only populated for SARIF documents.
  /// </summary>
  public IDictionary<string, RuleDescription> RuleDescriptions { get; init; }
      = new Dictionary<string, RuleDescription>();

  /// <summary>
  /// Absolute path to the source file that produced this document.
  /// </summary>
  public string SourcePath { get; init; } = string.Empty;
}


