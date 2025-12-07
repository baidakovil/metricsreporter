using System;
using System.Collections.Generic;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Describes a generate script execution request including script selection and logging options.
/// </summary>
/// <param name="ShouldRunScripts">Indicates whether scripts are enabled.</param>
/// <param name="HasScripts">Indicates whether any scripts are configured.</param>
/// <param name="LogPath">Path to the script log file.</param>
/// <param name="Verbosity">Verbosity level for logging.</param>
/// <param name="Scripts">Scripts to execute.</param>
/// <param name="WorkingDirectory">Working directory for script execution.</param>
/// <param name="Timeout">Execution timeout.</param>
/// <param name="LogTruncationLimit">Maximum log lines to retain.</param>
internal sealed record GenerateScriptRunRequest(
  bool ShouldRunScripts,
  bool HasScripts,
  string LogPath,
  string Verbosity,
  IReadOnlyList<string> Scripts,
  string WorkingDirectory,
  TimeSpan Timeout,
  int LogTruncationLimit);

