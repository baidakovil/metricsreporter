namespace MetricsReporter.Tests.MetricsReader;

using System;
using System.IO;
using NUnit.Framework;
using MetricsReporter.MetricsReader.Settings;
using MetricsReporter.Model;

/// <summary>
/// Provides common helpers for metrics-reader command tests.
/// </summary>
internal abstract class MetricsReaderCommandTestsBase
{
  protected string WorkingDirectory { get; private set; } = null!;

  [SetUp]
  public virtual void SetUp()
  {
    WorkingDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(WorkingDirectory);
  }

  [TearDown]
  public virtual void TearDown()
  {
    if (Directory.Exists(WorkingDirectory))
    {
      Directory.Delete(WorkingDirectory, recursive: true);
    }
  }

  protected string WriteReport(MetricsReport report)
    => MetricsReaderCommandTestData.WriteReportTo(WorkingDirectory, report);

  protected static NamespaceMetricSettings CreateNamespaceSettings(
    string reportPath,
    string @namespace,
    bool includeSuppressed = false,
    MetricsReaderSymbolKind symbolKind = MetricsReaderSymbolKind.Any,
    string? thresholdsFile = null,
    bool showAll = false,
    string metricName = "Complexity",
    string? ruleId = null,
    bool noUpdate = true,
    MetricsReaderGroupByOption? groupBy = null)
  {
    var settings = new NamespaceMetricSettings
    {
      ReportPath = reportPath,
      Namespace = @namespace,
      Metric = metricName,
      IncludeSuppressed = includeSuppressed,
      SymbolKind = symbolKind,
      ThresholdsFile = thresholdsFile,
      ShowAll = showAll,
      RuleId = ruleId,
      NoUpdate = noUpdate,
      GroupBy = groupBy
    };

    var validation = settings.Validate();
    Assert.That(validation.Successful, Is.True, validation.Message ?? "Settings validation failed.");
    return settings;
  }

  protected static SarifMetricSettings CreateSarifSettings(
    string reportPath,
    string @namespace,
    bool includeSuppressed = false,
    MetricsReaderSymbolKind symbolKind = MetricsReaderSymbolKind.Any,
    string? thresholdsFile = null,
    bool showAll = false,
    string? metricName = null,
    string? ruleId = null,
    bool noUpdate = true,
    MetricsReaderGroupByOption? groupBy = null)
  {
    var settings = new SarifMetricSettings
    {
      ReportPath = reportPath,
      Namespace = @namespace,
      Metric = metricName,
      IncludeSuppressed = includeSuppressed,
      SymbolKind = symbolKind,
      ThresholdsFile = thresholdsFile,
      ShowAll = showAll,
      RuleId = ruleId,
      NoUpdate = noUpdate,
      GroupBy = groupBy
    };

    var validation = settings.Validate();
    Assert.That(validation.Successful, Is.True, validation.Message ?? "Settings validation failed.");
    return settings;
  }

  protected static TestMetricSettings CreateTestSettings(
    string reportPath,
    string symbol,
    bool includeSuppressed = false,
    string metric = "Complexity",
    string? thresholdsFile = null,
    bool noUpdate = true)
  {
    var settings = new TestMetricSettings
    {
      ReportPath = reportPath,
      Symbol = symbol,
      Metric = metric,
      IncludeSuppressed = includeSuppressed,
      ThresholdsFile = thresholdsFile,
      NoUpdate = noUpdate
    };

    var validation = settings.Validate();
    Assert.That(validation.Successful, Is.True, validation.Message ?? "Settings validation failed.");
    return settings;
  }

  protected string WriteThresholdOverride(decimal warning, decimal error)
  {
    var thresholdJson = $@"
{{
  ""metrics"": [
    {{
      ""name"": ""RoslynCyclomaticComplexity"",
      ""symbolThresholds"": {{
        ""Type"": {{
          ""warning"": {warning.ToString(System.Globalization.CultureInfo.InvariantCulture)},
          ""error"": {error.ToString(System.Globalization.CultureInfo.InvariantCulture)}
        }},
        ""Member"": {{
          ""warning"": {warning.ToString(System.Globalization.CultureInfo.InvariantCulture)},
          ""error"": {error.ToString(System.Globalization.CultureInfo.InvariantCulture)}
        }}
      }}
    }}
  ]
}}";

    var path = Path.Combine(WorkingDirectory, $"Thresholds_{Guid.NewGuid():N}.json");
    File.WriteAllText(path, thresholdJson);
    return path;
  }
}

