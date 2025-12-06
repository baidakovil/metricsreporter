namespace MetricsReporter.Model;

/// <summary>
/// Represents a single SARIF rule violation extracted from analyzer output.
/// </summary>
/// <remarks>
/// The violation metadata is intentionally lightweight so it can be rendered
/// inside tooltips and consumed by automation without requiring the original
/// SARIF payload.
/// </remarks>
public sealed class SarifRuleViolationDetail
{
  /// <summary>
  /// Analyzer message text associated with the violation. May be <see langword="null"/>.
  /// </summary>
  public string? Message { get; init; }

  /// <summary>
  /// File URI reported by SARIF (<c>artifactLocation.uri</c>). May be <see langword="null"/>.
  /// </summary>
  public string? Uri { get; init; }

  /// <summary>
  /// First line of the violation range. May be <see langword="null"/> when SARIF omits line info.
  /// </summary>
  public int? StartLine { get; init; }

  /// <summary>
  /// Last line of the violation range. Falls back to <see cref="StartLine"/> when SARIF omits an explicit end line.
  /// </summary>
  public int? EndLine { get; init; }
}



