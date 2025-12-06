namespace MetricsReporter.MetricsReader;

using System;
using System.Threading.Tasks;
using Spectre.Console.Cli;

/// <summary>
/// Hosts the Spectre.Console CLI that powers metrics-reader commands.
/// </summary>
internal static class MetricsReaderConsoleHost
{
  public static async Task<int> ExecuteAsync(string[] args)
  {
    using var cancellationHandler = new CancellationHandler();
    try
    {
      var app = new CommandApp();
      MetricsReaderCommandConfigurator.Configure(app);
      return await app.RunAsync(args).ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
      Console.Error.WriteLine("metrics-reader execution cancelled.");
      return 1;
    }
  }
}


