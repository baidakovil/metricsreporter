namespace MetricsReporter.Aggregation;

using System;
using System.Collections.Generic;
using System.Linq;
using MetricsReporter.Model;
using MetricsReporter.Processing;

/// <summary>
/// Applies SARIF-derived metrics into the aggregated solution tree using the line index.
/// </summary>
/// <remarks>
/// Moving this logic out of <see cref="MetricsAggregationService.AggregationWorkspace"/>
/// keeps the workspace focused on orchestration and reduces the cyclomatic complexity of the workspace type.
/// </remarks>
internal sealed class SarifMetricsApplier
{
  private readonly LineIndex _lineIndex;
  private readonly AssemblyFilter _assemblyFilter;
  private readonly Action<MetricsNode, MetricIdentifier, MetricValue> _mergeMetric;

  /// <summary>
  /// Initializes a new instance of the <see cref="SarifMetricsApplier"/> class.
  /// </summary>
  public SarifMetricsApplier(
      LineIndex lineIndex,
      AssemblyFilter assemblyFilter,
      Action<MetricsNode, MetricIdentifier, MetricValue> mergeMetric)
  {
    _lineIndex = lineIndex ?? throw new ArgumentNullException(nameof(lineIndex));
    _assemblyFilter = assemblyFilter ?? throw new ArgumentNullException(nameof(assemblyFilter));
    _mergeMetric = mergeMetric ?? throw new ArgumentNullException(nameof(mergeMetric));
  }

  /// <summary>
  /// Applies all SARIF metrics from <paramref name="document"/> into the provided <paramref name="solution"/>.
  /// </summary>
  public void Apply(ParsedMetricsDocument document, SolutionMetricsNode solution)
  {
    ArgumentNullException.ThrowIfNull(document);

    foreach (var metric in SarifMetricExtractor.Extract(document))
    {
      var target = ResolveTarget(solution, metric);
      _mergeMetric(target, metric.Identifier, metric.Value);
    }
  }

  private MetricsNode ResolveTarget(SolutionMetricsNode solution, SarifMetric metric)
  {
    MetricsNode? target = null;

    if (metric.Line.HasValue)
    {
      target = _lineIndex.FindNode(metric.NormalizedPath, metric.Line.Value);
    }

    if (target is null && _lineIndex.TryGetAssembly(metric.NormalizedPath, out var assembly))
    {
      if (_assemblyFilter.ShouldExcludeAssembly(assembly.FullyQualifiedName))
      {
        return solution;
      }

      target = assembly;
    }

    return target ?? solution;
  }

  private sealed class SarifMetricExtractor
  {
    public static IEnumerable<SarifMetric> Extract(ParsedMetricsDocument document)
    {
      foreach (var element in document.Elements)
      {
        if (!IsValidElement(element))
        {
          continue;
        }

        var metricPair = ExtractFirstMetric(element);
        if (metricPair is null)
        {
          continue;
        }

        yield return CreateSarifMetric(element, metricPair.Value);
      }
    }

    private static bool IsValidElement(ParsedCodeElement element)
        => element.Source?.Path is not null && element.Metrics.Count > 0;

    private static KeyValuePair<MetricIdentifier, MetricValue>? ExtractFirstMetric(ParsedCodeElement element)
    {
      var firstMetric = element.Metrics.First();
      return firstMetric.Value.Value is not null ? firstMetric : null;
    }

    private static SarifMetric CreateSarifMetric(
        ParsedCodeElement element,
        KeyValuePair<MetricIdentifier, MetricValue> metric)
    {
      var source = element.Source!;
      var line = GetLineFromSource(source);
      var path = source.Path;
      ArgumentNullException.ThrowIfNull(path);
      var normalizedPath = PathNormalizer.Normalize(path);
      return new SarifMetric(normalizedPath, line, metric.Key, metric.Value);
    }

    private static int? GetLineFromSource(SourceLocation source)
        => source.StartLine ?? source.EndLine;
  }

  private sealed record SarifMetric(
      string NormalizedPath,
      int? Line,
      MetricIdentifier Identifier,
      MetricValue Value);
}


