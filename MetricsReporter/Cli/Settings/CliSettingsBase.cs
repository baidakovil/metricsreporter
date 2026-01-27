using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetricsReporter.Cli.Settings;

/// <summary>
/// Base settings shared by all metricsreporter commands.
/// </summary>
internal abstract class CliSettingsBase : CommandSettings
{
  /// <summary>
  /// Gets the optional path to the configuration file.
  /// </summary>
  [CommandOption("--config <PATH>")]
  [Description("Optional path to .metricsreporter.json. If omitted, the file is discovered by walking up from the working directory.")]
  public string? ConfigPath { get; init; }

  /// <summary>
  /// Gets the verbosity level.
  /// </summary>
  [CommandOption("--verbosity <quiet|minimal|normal|detailed>")]
  [Description("Controls informational logging. Errors are always logged.")]
  public string? Verbosity { get; init; }

  /// <summary>
  /// Gets the timeout in seconds for external scripts.
  /// </summary>
  [CommandOption("--timeout <SECONDS>")]
  [Description("Timeout in seconds for external scripts. Defaults to configuration or 900 seconds.")]
  public int? TimeoutSeconds { get; init; }

  /// <summary>
  /// Gets the working directory used for relative paths and scripts.
  /// </summary>
  [CommandOption("--working-dir <PATH>")]
  [Description("Working directory used for resolving relative paths.")]
  public string? WorkingDirectory { get; init; }

  /// <summary>
  /// Gets a value indicating whether to run configured scripts (true/false).
  /// </summary>
  [CommandOption("--run-scripts <true|false>")]
  [Description("Controls execution of configured scripts. Defaults to true.")]
  public bool? RunScripts { get; init; }

  /// <summary>
  /// Gets a value indicating whether aggregation should run after scripts complete (true/false).
  /// </summary>
  [CommandOption("--aggregate-after-scripts <true|false>")]
  [Description("Controls aggregation after scripts. Defaults to true for read/test/readsarif; ignored for generate.")]
  public bool? AggregateAfterScripts { get; init; }

  /// <summary>
  /// Gets the maximum number of stdout/stderr characters to log when scripts fail.
  /// </summary>
  [CommandOption("--log-truncation-limit <INT>")]
  [Description("Maximum number of stdout/stderr characters logged on script failures. Defaults to 4000.")]
  public int? LogTruncationLimit { get; init; }

  /// <summary>
  /// Gets optional metric alias mappings provided as JSON (e.g. { \"OpenCoverBranchCoverage\": [\"branch\"] }).
  /// </summary>
  [CommandOption("--metric-aliases <JSON>")]
  [Description("JSON object mapping metric identifiers to alias arrays. CLI overrides env/config.")]
  public string? MetricAliases { get; init; }

  /// <inheritdoc />
  public override ValidationResult Validate()
  {
    if (TimeoutSeconds.HasValue && TimeoutSeconds.Value <= 0)
    {
      return ValidationResult.Error("--timeout must be greater than zero.");
    }

    if (LogTruncationLimit.HasValue && LogTruncationLimit.Value <= 0)
    {
      return ValidationResult.Error("--log-truncation-limit must be greater than zero.");
    }

    return ValidationResult.Success();
  }
}

