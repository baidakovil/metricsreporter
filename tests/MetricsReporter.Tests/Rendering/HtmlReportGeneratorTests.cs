namespace MetricsReporter.Tests.Rendering;

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

[TestFixture]
[Category("Unit")]
public sealed class HtmlReportGeneratorTests
{
  [Test]
  public void Generate_BuildsHtmlWithStatusAndNewIndicators()
  {
    var member = new MemberMetricsNode
    {
      Name = "DoWork",
      FullyQualifiedName = "Sample.Namespace.SampleType.DoWork()",
      IsNew = true,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynMaintainabilityIndex] = new MetricValue
        {
          Value = 40,
          Status = ThresholdStatus.Error,
          Unit = "score"
        }
      }
    };

    var type = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode> { member }
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { type }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { @namespace }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        GeneratedAtUtc = DateTime.UtcNow,
        Paths = new ReportPaths
        {
          MetricsDirectory = @"C:\Repo\build\Metrics",
          Report = @"C:\Repo\build\Metrics\Report\metrics-report.json",
          Html = @"C:\Repo\build\Metrics\Report\metrics-report.html"
        },
        ThresholdsByLevel = new Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>>(),
        ThresholdDescriptions = new Dictionary<MetricIdentifier, string?>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode> { assembly }
      }
    };

    var html = HtmlReportGenerator.Generate(report);

    html.Should().Contain("SampleSolution");
    html.Should().Contain("badge-new");
    html.Should().Contain("status-error");
    html.Should().Contain("DoWork");
  }

  [Test]
  public void Generate_UsesMetricDescriptorsForUnitFormatting()
  {
    var member = new MemberMetricsNode
    {
      Name = "Coverage",
      FullyQualifiedName = "Sample.Namespace.SampleType.Coverage()",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.AltCoverSequenceCoverage] = new MetricValue { Value = 75, Status = ThresholdStatus.Warning }
      }
    };

    var type = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode> { member }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode>
      {
        new()
        {
          Name = "Sample.Namespace",
          FullyQualifiedName = "Sample.Namespace",
          Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
          Types = new List<TypeMetricsNode> { type }
        }
      }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        GeneratedAtUtc = DateTime.UtcNow,
        Paths = new ReportPaths(),
        ThresholdsByLevel = new Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>>(),
        ThresholdDescriptions = new Dictionary<MetricIdentifier, string?>(),
        MetricDescriptors = new Dictionary<MetricIdentifier, MetricDescriptor>
        {
          [MetricIdentifier.AltCoverSequenceCoverage] = new() { Unit = "percent" }
        }
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode> { assembly }
      }
    };

    var html = HtmlReportGenerator.Generate(report);
    html.Should().Contain("75%");
  }

  [Test]
  public void Generate_WithSourceLocation_AddsOpenFileAction()
  {
    var typeSource = new SourceLocation
    {
      Path = @"C:\Repo\SampleType.cs",
      StartLine = 10,
      EndLine = 60
    };

    var member = new MemberMetricsNode
    {
      Name = "DoWork",
      FullyQualifiedName = "Sample.Namespace.SampleType.DoWork(...)",
      Source = new SourceLocation
      {
        Path = @"C:\Repo\SampleType.cs",
        StartLine = 42,
        EndLine = 45
      },
      Metrics = new Dictionary<MetricIdentifier, MetricValue>()
    };

    var type = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Source = typeSource,
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode> { member }
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { type }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { @namespace }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        GeneratedAtUtc = DateTime.UtcNow,
        Paths = new ReportPaths(),
        ThresholdsByLevel = new Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>>(),
        ThresholdDescriptions = new Dictionary<MetricIdentifier, string?>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode> { assembly }
      }
    };

    var html = HtmlReportGenerator.Generate(report);
    var typeRow = ExtractFirstTypeRow(html);

    typeRow.Should().Contain("data-action=\"open\"");
    typeRow.Should().Contain("data-source-path=\"C:\\Repo\\SampleType.cs\"");
    typeRow.Should().Contain("data-source-line=\"10\"");
    typeRow.Should().Contain("sourcePath");
    typeRow.Should().Contain("C:\\\\Repo\\\\SampleType.cs");
  }

  [Test]
  public void Generate_WithSourceLocation_EmitsEndLineAttribute()
  {
    var type = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Source = new SourceLocation
      {
        Path = @"C:\Repo\SampleType.cs",
        StartLine = 12,
        EndLine = 64
      },
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode>()
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { type }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { @namespace }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        GeneratedAtUtc = DateTime.UtcNow,
        Paths = new ReportPaths(),
        ThresholdsByLevel = new Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>>(),
        ThresholdDescriptions = new Dictionary<MetricIdentifier, string?>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode> { assembly }
      }
    };

    var html = HtmlReportGenerator.Generate(report);
    var typeRow = ExtractFirstTypeRow(html);

    typeRow.Should().Contain("data-source-end-line=\"64\"");
  }

  [Test]
  public void Generate_WithoutSourceLocation_OmitsOpenFileAction()
  {
    var type = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode>()
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { type }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { @namespace }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        GeneratedAtUtc = DateTime.UtcNow,
        Paths = new ReportPaths(),
        ThresholdsByLevel = new Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>>(),
        ThresholdDescriptions = new Dictionary<MetricIdentifier, string?>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode> { assembly }
      }
    };

    var html = HtmlReportGenerator.Generate(report);
    var typeRow = ExtractFirstTypeRow(html);

    typeRow.Should().NotContain("data-action=\"open\"");
    typeRow.Should().NotContain("data-source-path");
    typeRow.Should().NotContain("sourcePath");
  }

  [Test]
  public void Generate_WithCoverageHtmlDir_GeneratesLinksForTypes()
  {
    // Arrange
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempDir);

    try
    {
      var type = new TypeMetricsNode
      {
        Name = "SampleType",
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Members = new List<MemberMetricsNode>()
      };

      var @namespace = new NamespaceMetricsNode
      {
        Name = "Sample.Namespace",
        FullyQualifiedName = "Sample.Namespace",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Types = new List<TypeMetricsNode> { type }
      };

      var assembly = new AssemblyMetricsNode
      {
        Name = "Sample.Assembly",
        FullyQualifiedName = "Sample.Assembly",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Namespaces = new List<NamespaceMetricsNode> { @namespace }
      };

      var report = new MetricsReport
      {
        Metadata = new ReportMetadata
        {
          GeneratedAtUtc = DateTime.UtcNow,
          Paths = new ReportPaths
          {
            MetricsDirectory = @"C:\Repo\build\Metrics",
            Report = @"C:\Repo\build\Metrics\Report\metrics-report.json",
            Html = @"C:\Repo\build\Metrics\Report\metrics-report.html"
          },
          ThresholdsByLevel = new Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>>(),
          ThresholdDescriptions = new Dictionary<MetricIdentifier, string?>()
        },
        Solution = new SolutionMetricsNode
        {
          Name = "SampleSolution",
          FullyQualifiedName = "SampleSolution",
          Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
          Assemblies = new List<AssemblyMetricsNode> { assembly }
        }
      };

      // Create coverage HTML file
      var htmlFileName = "Sample.Assembly_SampleType.html";
      var htmlFilePath = Path.Combine(tempDir, htmlFileName);
      File.WriteAllText(htmlFilePath, "<html></html>");

      // Act
      var html = HtmlReportGenerator.Generate(report, tempDir);

      // Assert
      html.Should().Contain("SampleType");
      html.Should().Contain("coverage-link-type");
      html.Should().Contain(htmlFileName);
      html.Should().Contain("file://");
    }
    finally
    {
      if (Directory.Exists(tempDir))
      {
        Directory.Delete(tempDir, recursive: true);
      }
    }
  }

  [Test]
  public void Generate_WithCoverageHtmlDirButMissingFile_DoesNotGenerateLinks()
  {
    // Arrange
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempDir);

    try
    {
      var type = new TypeMetricsNode
      {
        Name = "SampleType",
        FullyQualifiedName = "Sample.Namespace.SampleType",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Members = new List<MemberMetricsNode>()
      };

      var @namespace = new NamespaceMetricsNode
      {
        Name = "Sample.Namespace",
        FullyQualifiedName = "Sample.Namespace",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Types = new List<TypeMetricsNode> { type }
      };

      var assembly = new AssemblyMetricsNode
      {
        Name = "Sample.Assembly",
        FullyQualifiedName = "Sample.Assembly",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Namespaces = new List<NamespaceMetricsNode> { @namespace }
      };

      var report = new MetricsReport
      {
        Metadata = new ReportMetadata
        {
          GeneratedAtUtc = DateTime.UtcNow,
          Paths = new ReportPaths
          {
            MetricsDirectory = @"C:\Repo\build\Metrics",
            Report = @"C:\Repo\build\Metrics\Report\metrics-report.json",
            Html = @"C:\Repo\build\Metrics\Report\metrics-report.html"
          },
          ThresholdsByLevel = new Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>>(),
          ThresholdDescriptions = new Dictionary<MetricIdentifier, string?>()
        },
        Solution = new SolutionMetricsNode
        {
          Name = "SampleSolution",
          FullyQualifiedName = "SampleSolution",
          Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
          Assemblies = new List<AssemblyMetricsNode> { assembly }
        }
      };

      // Do NOT create coverage HTML file
      // Act
      var html = HtmlReportGenerator.Generate(report, tempDir);

      // Assert
      html.Should().Contain("SampleType");
      // Verify that no link with coverage-link-type class is generated
      html.Should().NotMatchRegex(@"<a[^>]*coverage-link-type[^>]*>");
      html.Should().NotContain("file://");
    }
    finally
    {
      if (Directory.Exists(tempDir))
      {
        Directory.Delete(tempDir, recursive: true);
      }
    }

  }

  [Test]
  public void Generate_WithoutCoverageHtmlDir_DoesNotGenerateLinks()
  {
    // Arrange
    var type = new TypeMetricsNode
    {
      Name = "SampleType",
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Members = new List<MemberMetricsNode>()
    };

    var @namespace = new NamespaceMetricsNode
    {
      Name = "Sample.Namespace",
      FullyQualifiedName = "Sample.Namespace",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Types = new List<TypeMetricsNode> { type }
    };

    var assembly = new AssemblyMetricsNode
    {
      Name = "Sample.Assembly",
      FullyQualifiedName = "Sample.Assembly",
      Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
      Namespaces = new List<NamespaceMetricsNode> { @namespace }
    };

    var report = new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        GeneratedAtUtc = DateTime.UtcNow,
        Paths = new ReportPaths
        {
          MetricsDirectory = @"C:\Repo\build\Metrics",
          Report = @"C:\Repo\build\Metrics\Report\metrics-report.json",
          Html = @"C:\Repo\build\Metrics\Report\metrics-report.html"
        },
        ThresholdsByLevel = new Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>>(),
        ThresholdDescriptions = new Dictionary<MetricIdentifier, string?>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "SampleSolution",
        FullyQualifiedName = "SampleSolution",
        Metrics = new Dictionary<MetricIdentifier, MetricValue>(),
        Assemblies = new List<AssemblyMetricsNode> { assembly }
      }
    };

    // Act
    var html = HtmlReportGenerator.Generate(report, null);

    // Assert
    html.Should().Contain("SampleType");
    // Verify that no link with coverage-link-type class is generated
    html.Should().NotMatchRegex(@"<a[^>]*coverage-link-type[^>]*>");
  }

  private static string ExtractFirstTypeRow(string html)
  {
    const string marker = "data-role=\"type\"";
    var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
    if (markerIndex < 0)
    {
      return string.Empty;
    }

    var rowStart = html.LastIndexOf("<tr", markerIndex, StringComparison.Ordinal);
    if (rowStart < 0)
    {
      rowStart = markerIndex;
    }

    var rowEnd = html.IndexOf("</tr>", markerIndex, StringComparison.Ordinal);
    if (rowEnd < 0)
    {
      rowEnd = html.Length;
    }

    return html[rowStart..rowEnd];
  }
}


