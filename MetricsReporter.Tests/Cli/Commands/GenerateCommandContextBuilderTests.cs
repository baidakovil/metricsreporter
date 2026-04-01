namespace MetricsReporter.Tests.Cli.Commands;

using System;
using System.IO;
using FluentAssertions;
using MetricsReporter.Cli.Commands;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using NUnit.Framework;

/// <summary>
/// Verifies that generate command configuration is propagated into runtime options.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class GenerateCommandContextBuilderTests
{
  private string _tempDirectory = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDirectory = Path.Combine(Path.GetTempPath(), $"metricsreporter-tests-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDirectory);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDirectory))
    {
      Directory.Delete(_tempDirectory, recursive: true);
    }
  }

  [Test]
  public void Build_WithEditorPrefixInConfig_SetsOptionsEditorPrefix()
  {
    // This verifies the real generate-command configuration path.
    // The feature is broken if the config loads but MetricsReporterOptions drops the editor prefix.
    var reportPath = Path.Combine(_tempDirectory, "report.json");
    var configPath = Path.Combine(_tempDirectory, ".metricsreporter.json");
    var outputHtmlPath = Path.Combine(_tempDirectory, "report.html");

    File.WriteAllText(reportPath, "{}");
    File.WriteAllText(
      configPath,
      $$"""
      {
        "general": {
          "runScripts": false,
          "aggregateAfterScripts": false,
          "editorPrefix": "cursor://"
        },
        "paths": {
          "inputJson": "report.json",
          "outputHtml": "report.html"
        },
        "scripts": {
        }
      }
      """);

    var builder = new GenerateCommandContextBuilder(new MetricsReporterConfigLoader());
    var settings = new GenerateSettings
    {
      ConfigPath = configPath,
      WorkingDirectory = _tempDirectory
    };

    var result = builder.Build(settings);

    result.Succeeded.Should().BeTrue();
    result.Context.Should().NotBeNull();
    result.Context!.Options.EditorPrefix.Should().Be("cursor://");
    result.Context.Options.OutputHtmlPath.Should().Be(outputHtmlPath);
  }
}