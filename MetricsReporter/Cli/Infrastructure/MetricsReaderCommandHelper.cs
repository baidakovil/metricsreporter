using System;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.MetricsReader;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

namespace MetricsReporter.Cli.Infrastructure;

/// <summary>
/// Helper methods for creating metrics reader engines used by CLI commands.
/// </summary>
internal static class MetricsReaderCommandHelper
{
  public static async Task<MetricsReaderEngine> CreateEngineAsync(MetricsReaderSettingsBase settings, CancellationToken cancellationToken)
  {
    using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, MetricsReaderCancellation.Token);
    var factory = CreateFactory();
    var context = await factory.CreateAsync(settings, linkedSource.Token).ConfigureAwait(false);
    return CreateEngine(context);
  }

  private static MetricsReaderContextFactory CreateFactory()
  {
    var reportLoader = new JsonReportLoaderAdapter();
    var thresholdsParser = new ThresholdsParserAdapter();
    var thresholdsFileLoader = new ThresholdsFileLoader(thresholdsParser);
    return new MetricsReaderContextFactory(reportLoader, thresholdsFileLoader);
  }

  private static MetricsReaderEngine CreateEngine(MetricsReaderContext context)
  {
    return MetricsReaderEngineBuilder.Build(context);
  }
}

