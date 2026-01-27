namespace MetricsReporter.Processing.Parsers;

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using MetricsReporter.Model;

/// <summary>
/// Maps OpenCover summary data to metric values for parsed elements.
/// </summary>
internal static class OpenCoverMetricMapper
{
  internal static void PopulateSummaryMetrics(IDictionary<MetricIdentifier, MetricValue> target, XElement? summary)
  {
    if (summary is null)
    {
      return;
    }

    AddMetric(target, MetricIdentifier.OpenCoverSequenceCoverage, summary.AttributeByLocalName("sequenceCoverage"));

    // WHY: Branch coverage is only applicable when there are actual branch points to measure.
    // If numBranchPoints is 0 or missing, branch coverage should not be included in the report
    // to avoid misleading 0% coverage values for code that has no branches.
    var numBranchPoints = summary.AttributeByLocalName("numBranchPoints")?.GetDecimalValue();
    if (numBranchPoints.HasValue && numBranchPoints.Value > 0)
    {
      AddMetric(target, MetricIdentifier.OpenCoverBranchCoverage, summary.AttributeByLocalName("branchCoverage"));
    }

    AddMetric(target, MetricIdentifier.OpenCoverCyclomaticComplexity, summary.AttributeByLocalName("maxCyclomaticComplexity"));
    AddMetric(target, MetricIdentifier.OpenCoverNPathComplexity, summary.AttributeByLocalName("maxNPathComplexity"));
  }

  internal static void PopulateMethodMetrics(IDictionary<MetricIdentifier, MetricValue> target, XElement methodElement)
  {
    AddMetric(target, MetricIdentifier.OpenCoverSequenceCoverage, methodElement.AttributeByLocalName("sequenceCoverage"));

    // WHY: Branch coverage is only applicable when there are actual BranchPoint elements to measure.
    // If the BranchPoints element is empty or missing, branch coverage should not be included
    // to avoid misleading 0% coverage values for methods that have no branches (e.g., simple getters,
    // methods with only linear code paths). This prevents false warnings when sequence coverage is 100%
    // but branch coverage shows 0% due to the absence of branches rather than uncovered branches.
    var branchPoints = methodElement.ElementByLocalName("BranchPoints");
    if (branchPoints is not null && branchPoints.ElementsByLocalName("BranchPoint").Any())
    {
      AddMetric(target, MetricIdentifier.OpenCoverBranchCoverage, methodElement.AttributeByLocalName("branchCoverage"));
    }

    AddMetric(target, MetricIdentifier.OpenCoverCyclomaticComplexity, methodElement.AttributeByLocalName("cyclomaticComplexity"));
    AddMetric(target, MetricIdentifier.OpenCoverNPathComplexity, methodElement.AttributeByLocalName("nPathComplexity"));
  }

  private static void AddMetric(IDictionary<MetricIdentifier, MetricValue> target, MetricIdentifier identifier, XAttribute? attribute)
  {
    if (attribute is null)
    {
      return;
    }

    var value = attribute.GetDecimalValue();
    if (value is null)
    {
      return;
    }

    target[identifier] = new MetricValue
    {
      Value = value,
      Status = ThresholdStatus.NotApplicable
    };
  }
}
