namespace MetricsReporter.Model;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Provides descriptor information (such as units) for all supported metrics.
/// </summary>
internal static class MetricDescriptorCatalog
{
  private static readonly Dictionary<MetricIdentifier, MetricDescriptor> Descriptors =
      new Dictionary<MetricIdentifier, MetricDescriptor>
      {
        [MetricIdentifier.OpenCoverSequenceCoverage] = new() { Unit = "percent" },
        [MetricIdentifier.OpenCoverBranchCoverage] = new() { Unit = "percent" },
        [MetricIdentifier.OpenCoverCyclomaticComplexity] = new() { Unit = "count" },
        [MetricIdentifier.OpenCoverNPathComplexity] = new() { Unit = "count" },
        [MetricIdentifier.RoslynMaintainabilityIndex] = new() { Unit = "score" },
        [MetricIdentifier.RoslynCyclomaticComplexity] = new() { Unit = "count" },
        [MetricIdentifier.RoslynClassCoupling] = new() { Unit = "count" },
        [MetricIdentifier.RoslynDepthOfInheritance] = new() { Unit = "count" },
        [MetricIdentifier.RoslynSourceLines] = new() { Unit = "count" },
        [MetricIdentifier.RoslynExecutableLines] = new() { Unit = "count" },
        [MetricIdentifier.SarifCaRuleViolations] = new() { Unit = "count" },
        [MetricIdentifier.SarifIdeRuleViolations] = new() { Unit = "count" }
      };

  /// <summary>
  /// Creates a dictionary of descriptors keyed by metric identifier.
  /// </summary>
  public static IDictionary<MetricIdentifier, MetricDescriptor> CreateDescriptors()
      => Descriptors.ToDictionary(
          pair => pair.Key,
          pair => new MetricDescriptor { Unit = pair.Value.Unit });

  /// <summary>
  /// Tries to resolve a unit for a metric identifier.
  /// </summary>
  public static string? TryGetUnit(MetricIdentifier identifier)
      => Descriptors.TryGetValue(identifier, out var descriptor) ? descriptor.Unit : null;
}


