using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MetricsReporter.Cli.Commands;
using MetricsReporter.Configuration;
using MetricsReporter.Services.Processes;
using MetricsReporter.Services.Scripts;
using MetricsReporter.Tool.Infrastructure;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace MetricsReporter.Tool;

/// <summary>
/// Application entry point and composition root for the metricsreporter CLI.
/// </summary>
internal static class Program
{
  /// <summary>
  /// Application entry point.
  /// </summary>
  /// <param name="args">Command-line arguments.</param>
  /// <returns>Exit code produced by Spectre.Console command execution.</returns>
  public static async Task<int> Main(string[] args)
  {
    var services = ServiceCollectionFactory.Create();
    var registrar = new ServiceCollectionTypeRegistrar(services);
    var app = new CommandApp(registrar);

    CommandAppConfigurator.Configure(app);

    return await app.RunAsync(args).ConfigureAwait(false);
  }
}

internal static class ServiceCollectionFactory
{
  public static ServiceCollection Create()
  {
    var services = new ServiceCollection();
    services.AddLogging(builder =>
    {
      builder.SetMinimumLevel(LogLevel.Information);
      builder.AddSimpleConsole(options =>
      {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
        options.UseUtcTimestamp = true;
        options.IncludeScopes = true;
      });
    });
    services.AddSingleton<MetricsReporterConfigLoader>();
    services.AddSingleton<IProcessRunner, ProcessRunner>();
    services.AddSingleton<ScriptExecutionService>();
    services.AddSingleton<GenerateCommand>();
    services.AddSingleton<ReadCommand>();
    services.AddSingleton<ReadSarifCommand>();
    services.AddSingleton<TestCommand>();

    return services;
  }
}

internal static class CommandAppConfigurator
{
  public static void Configure(CommandApp app)
  {
    app.Configure(config =>
    {
      config.SetApplicationName("metricsreporter");
      config.ValidateExamples();

      config.AddCommand<GenerateCommand>("generate")
        .WithDescription("Aggregates metrics from OpenCover/Roslyn/SARIF inputs and produces metrics-report.json/metrics-report.html.")
        .WithExample("generate", "--metrics-dir", "build/Metrics", "--opencover", "coverage.xml", "--roslyn", "metrics.xml", "--sarif", "analyzers.sarif", "--output-json", "build/Metrics/Report/MetricsReport.g.json")
        .WithExample("generate", "--input-json", "build/Metrics/Report/MetricsReport.g.json", "--output-html", "build/Metrics/Report/MetricsReport.html");

      config.AddCommand<ReadCommand>("read")
        .WithDescription("Reads metric violations for a namespace. Returns the most severe violation by default; use --all for full listing.")
        .WithExample("read", "--report", "build/Metrics/Report/MetricsReport.g.json", "--namespace", "Sample.Loader", "--metric", "Complexity");

      config.AddCommand<ReadSarifCommand>("readsarif")
        .WithDescription("Aggregates SARIF-based metrics (SarifCaRuleViolations, SarifIdeRuleViolations) by rule ID for the specified namespace.")
        .WithExample("readsarif", "--report", "build/Metrics/Report/MetricsReport.g.json", "--namespace", "Sample.Loader")
        .WithExample("readsarif", "--report", "build/Metrics/Report/MetricsReport.g.json", "--namespace", "Sample.Loader", "--metric", "SarifIdeRuleViolations", "--all");

      config.AddCommand<TestCommand>("test")
        .WithDescription("Checks whether a symbol satisfies the specified metric after refactoring.")
        .WithExample("test", "--report", "build/Metrics/Report/MetricsReport.g.json", "--symbol", "Sample.Loader.SomeType.SomeMethod(...)", "--metric", "Complexity");
    });
  }
}