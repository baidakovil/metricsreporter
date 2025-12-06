namespace MetricsReporter;

/// <summary>
/// Process exit codes returned by the console aggregator.
/// </summary>
public enum MetricsReporterExitCode
{
  /// <summary>
  /// Execution completed successfully.
  /// </summary>
  Success = 0,

  /// <summary>
  /// Parsing error in one of the input files.
  /// </summary>
  ParsingError = 1,

  /// <summary>
  /// Input/output error.
  /// </summary>
  IoError = 2,

  /// <summary>
  /// Validation error or inconsistent data.
  /// </summary>
  ValidationError = 3
}


