namespace MetricsReporter.MetricsReader;

using MetricsReporter.MetricsReader.Commands;
using Spectre.Console.Cli;

/// <summary>
/// Configures commands for the metrics-reader CLI application.
/// </summary>
internal static class MetricsReaderCommandConfigurator
{
  /// <summary>
  /// Configures the command application with all available metrics-reader commands.
  /// </summary>
  /// <param name="app">The command application to configure.</param>
  public static void Configure(CommandApp app)
  {
    app.Configure(config =>
    {
      config.SetApplicationName("metrics-reader");
      config.ValidateExamples();

      config.AddCommand<ReadAnyCommand>("readany")
        .WithDescription("Reads metric violations for a namespace. Returns the most severe violation by default. Pass --all to list all matches.")
        .WithExample("readany", "--namespace", "Sample.Loader", "--metric", "Complexity")
        .WithExample("readany", "--namespace", "Sample.Loader", "--metric", "Complexity", "--all", "--symbol-kind", "Member");

      config.AddCommand<ReadSarifCommand>("readsarif")
        .WithDescription("Aggregates SARIF-based metrics (SarifCaRuleViolations, SarifIdeRuleViolations) by rule ID for the specified namespace. --metric defaults to Any.")
        .WithExample("readsarif", "--namespace", "Sample.Loader")
        .WithExample("readsarif", "--namespace", "Sample.Loader", "--metric", "SarifIdeRuleViolations", "--all", "--symbol-kind", "Member")
        .WithExample("readsarif", "--namespace", "Sample.Loader", "--metric", "Any", "--ruleid", "CA1506");

      config.AddCommand<TestMetricCommand>("test")
        .WithDescription("Checks whether a symbol satisfies the specified metric after refactoring.")
        .WithExample("test", "--symbol", "Sample.Loader.SomeType.SomeMethod(...)", "--metric", "Complexity");
    });
  }
}


