using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Cli.Infrastructure;
using MetricsReporter.Services.Scripts;

namespace MetricsReporter.Cli.Commands;

/// <summary>
/// Executes generate scripts while managing logger scope creation.
/// </summary>
internal sealed class GenerateScriptExecutionClient : IGenerateScriptExecutionClient
{
  private readonly ScriptRunExecutor _scriptRunner;
  private readonly IGenerateScriptLoggerFactory _loggerFactory;

  public GenerateScriptExecutionClient(ScriptExecutionService scriptExecutor, IGenerateScriptLoggerFactory loggerFactory)
  {
    _scriptRunner = new ScriptRunExecutor(scriptExecutor ?? throw new System.ArgumentNullException(nameof(scriptExecutor)));
    _loggerFactory = loggerFactory ?? throw new System.ArgumentNullException(nameof(loggerFactory));
  }

  /// <inheritdoc />
  public async Task<int?> ExecuteAsync(GenerateScriptRunRequest request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);

    using var scope = _loggerFactory.CreateScope(request);
    return await _scriptRunner.ExecuteAsync(request, scope.Logger, cancellationToken).ConfigureAwait(false);
  }
}

