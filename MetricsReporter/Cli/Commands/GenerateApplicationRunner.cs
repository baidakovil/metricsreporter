using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Services;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Runs the MetricsReporter application for the generate command using resolved options.
/// </summary>
internal sealed class GenerateApplicationRunner
{
  private readonly MetricsReporterApplication _application;

  public GenerateApplicationRunner()
  {
    _application = new MetricsReporterApplication();
  }

  /// <summary>
  /// Executes the application and returns the resulting exit code.
  /// </summary>
  /// <param name="options">Generation options resolved from CLI and configuration.</param>
  /// <param name="cancellationToken">Cancellation token controlling execution.</param>
  /// <returns>Process exit code.</returns>
  public async Task<int> RunAsync(MetricsReporterOptions options, CancellationToken cancellationToken)
  {
    var exitCode = await _application.RunAsync(options, cancellationToken).ConfigureAwait(false);
    return (int)exitCode;
  }
}

