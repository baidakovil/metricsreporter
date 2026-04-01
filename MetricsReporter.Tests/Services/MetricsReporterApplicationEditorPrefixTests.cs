namespace MetricsReporter.Tests.Services;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MetricsReporter.Model;
using MetricsReporter.Services;
using NUnit.Framework;

/// <summary>
/// Verifies HTML generation respects the configured editor link prefix.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class MetricsReporterApplicationEditorPrefixTests
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
  public async Task RunAsync_WithInputJsonAndEditorPrefix_WritesHtmlWithConfiguredPrefix()
  {
    // This exercises the production path used by `metricsreporter generate` when HTML is produced.
    // The report is only correct if the configured prefix survives all layers and reaches the rendered rows.
    var inputJsonPath = Path.Combine(_tempDirectory, "report.json");
    var outputHtmlPath = Path.Combine(_tempDirectory, "report.html");
    var logPath = Path.Combine(_tempDirectory, "metrics.log");

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata(),
      Solution = new SolutionMetricsNode
      {
        Name = "Sample",
        Assemblies =
        [
          new AssemblyMetricsNode
          {
            Name = "Sample.Assembly",
            Namespaces =
            [
              new NamespaceMetricsNode
              {
                Name = "Sample.Namespace",
                Types =
                [
                  new TypeMetricsNode
                  {
                    Name = "Sample.Type",
                    Members =
                    [
                      new MemberMetricsNode
                      {
                        Name = "Run",
                        FullyQualifiedName = "Sample.Namespace.Sample.Type.Run",
                        Source = new SourceLocation
                        {
                          Path = "C:\\src\\Sample.cs",
                          StartLine = 42,
                          EndLine = 45
                        }
                      }
                    ]
                  }
                ]
              }
            ]
          }
        ]
      }
    };

    var json = System.Text.Json.JsonSerializer.Serialize(report, MetricsReporter.Serialization.JsonSerializerOptionsFactory.Create());
    File.WriteAllText(inputJsonPath, json);

    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      InputJsonPath = inputJsonPath,
      OutputHtmlPath = outputHtmlPath,
      LogFilePath = logPath,
      Verbosity = "quiet",
      EditorPrefix = "cursor://"
    };

    var application = new MetricsReporterApplication();

    var exitCode = await application.RunAsync(options, CancellationToken.None);

    exitCode.Should().Be(MetricsReporterExitCode.Success);
    File.Exists(outputHtmlPath).Should().BeTrue();

    var html = File.ReadAllText(outputHtmlPath);
    html.Should().Contain("data-editor-prefix=\"cursor://file/\"");
    html.Should().NotContain("data-editor-prefix=\"vscode://file/\"");
    html.Should().Contain("data-editor-url=\"cursor://file/C:/src/Sample.cs:42\"");
  }
}