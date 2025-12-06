namespace MetricsReporter.Tests.MetricsReader;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MetricsReporter.Model;
using MetricsReporter.Serialization;

/// <summary>
/// Provides reusable builders for metrics-reader command tests.
/// </summary>
internal static class MetricsReaderCommandTestData
{
  public static MetricsReport CreateReport(
    IEnumerable<TypeMetricsNode> types,
    IEnumerable<SuppressedSymbolInfo>? suppressed = null,
    string? namespaceName = "Rca.Loader.Services",
    string? assemblyName = "Rca.Loader")
  {
    var namespaceNode = new NamespaceMetricsNode
    {
      Name = namespaceName ?? string.Empty,
      FullyQualifiedName = namespaceName,
      Types = types.ToList()
    };

    var assemblyNode = new AssemblyMetricsNode
    {
      Name = assemblyName ?? string.Empty,
      FullyQualifiedName = assemblyName,
      Namespaces = new List<NamespaceMetricsNode> { namespaceNode }
    };

    return new MetricsReport
    {
      Metadata = new ReportMetadata
      {
        ThresholdsByLevel = CreateDefaultThresholds(),
        SuppressedSymbols = suppressed?.ToList() ?? new List<SuppressedSymbolInfo>()
      },
      Solution = new SolutionMetricsNode
      {
        Name = "rca-plugin",
        Assemblies = new List<AssemblyMetricsNode> { assemblyNode }
      }
    };
  }

  public static TypeMetricsNode CreateTypeNode(
    string fullyQualifiedName,
    decimal value,
    ThresholdStatus status,
    IEnumerable<MemberMetricsNode>? members = null)
  {
    return new TypeMetricsNode
    {
      Name = fullyQualifiedName.Split('.').Last(),
      FullyQualifiedName = fullyQualifiedName,
      Source = new SourceLocation
      {
        Path = $"src/{fullyQualifiedName.Replace('.', Path.DirectorySeparatorChar)}.cs"
      },
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynCyclomaticComplexity] = new MetricValue
        {
          Value = value,
          Delta = value / 2,
          Status = status,
          Unit = "count"
        }
      },
      Members = members?.ToList() ?? new List<MemberMetricsNode>()
    };
  }

  public static TypeMetricsNode CreateTypeNode(
    string fullyQualifiedName,
    IDictionary<MetricIdentifier, MetricValue> metrics,
    IEnumerable<MemberMetricsNode>? members = null)
  {
    return new TypeMetricsNode
    {
      Name = fullyQualifiedName.Split('.').Last(),
      FullyQualifiedName = fullyQualifiedName,
      Source = new SourceLocation
      {
        Path = $"src/{fullyQualifiedName.Replace('.', Path.DirectorySeparatorChar)}.cs"
      },
      Metrics = metrics,
      Members = members?.ToList() ?? new List<MemberMetricsNode>()
    };
  }

  public static MemberMetricsNode CreateMemberNode(
    string fullyQualifiedName,
    decimal value,
    ThresholdStatus status)
  {
    return new MemberMetricsNode
    {
      Name = fullyQualifiedName.Split('.').Last(),
      FullyQualifiedName = fullyQualifiedName,
      Source = new SourceLocation
      {
        Path = $"src/{fullyQualifiedName.Replace('.', Path.DirectorySeparatorChar)}.cs"
      },
      Metrics = new Dictionary<MetricIdentifier, MetricValue>
      {
        [MetricIdentifier.RoslynCyclomaticComplexity] = new MetricValue
        {
          Value = value,
          Status = status,
          Unit = "count"
        }
      }
    };
  }

  public static MemberMetricsNode CreateMemberNode(
    string fullyQualifiedName,
    IDictionary<MetricIdentifier, MetricValue> metrics)
  {
    return new MemberMetricsNode
    {
      Name = fullyQualifiedName.Split('.').Last(),
      FullyQualifiedName = fullyQualifiedName,
      Source = new SourceLocation
      {
        Path = $"src/{fullyQualifiedName.Replace('.', Path.DirectorySeparatorChar)}.cs"
      },
      Metrics = metrics
    };
  }

  public static string WriteReportTo(string directory, MetricsReport report)
  {
    var path = Path.Combine(directory, $"MetricsReport_{Guid.NewGuid():N}.json");
    var options = JsonSerializerOptionsFactory.Create();
    var payload = JsonSerializer.Serialize(report, options);
    File.WriteAllText(path, payload);
    return path;
  }

  public static IDictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>> CreateDefaultThresholds()
  {
    return new Dictionary<MetricIdentifier, IDictionary<MetricSymbolLevel, MetricThreshold>>
    {
      [MetricIdentifier.RoslynCyclomaticComplexity] = new Dictionary<MetricSymbolLevel, MetricThreshold>
      {
        [MetricSymbolLevel.Type] = new MetricThreshold { Warning = 10, Error = 20, HigherIsBetter = false },
        [MetricSymbolLevel.Member] = new MetricThreshold { Warning = 15, Error = 25, HigherIsBetter = false }
      }
    };
  }
}



