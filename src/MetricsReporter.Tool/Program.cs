using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MetricsReporter.Cli.Commands;
using MetricsReporter.Configuration;
using MetricsReporter.Services.Processes;
using MetricsReporter.Services.Scripts;
using MetricsReporter.Tool.Infrastructure;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddSingleton<MetricsReporterConfigLoader>();
services.AddSingleton<IProcessRunner, ProcessRunner>();
services.AddSingleton<ScriptExecutionService>();
services.AddSingleton<GenerateCommand>();
services.AddSingleton<ReadCommand>();
services.AddSingleton<ReadSarifCommand>();
services.AddSingleton<TestCommand>();

var registrar = new ServiceCollectionTypeRegistrar(services);
var app = new CommandApp(registrar);
app.Configure(config =>
{
  config.SetApplicationName("metricsreporter");
  config.ValidateExamples();

  config.AddCommand<GenerateCommand>("generate")
    .WithDescription("Aggregates metrics from AltCover/Roslyn/SARIF inputs and produces metrics-report.json/metrics-report.html.")
    .WithExample("generate", "--metrics-dir", "build/Metrics", "--altcover", "coverage.xml", "--roslyn", "metrics.xml", "--sarif", "analyzers.sarif", "--output-json", "build/Metrics/Report/MetricsReport.g.json")
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

return await app.RunAsync(args).ConfigureAwait(false);


