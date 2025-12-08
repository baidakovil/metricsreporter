namespace MetricsReporter.Cli.Infrastructure;

using System;
using MetricsReporter.MetricsReader;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.Model;

/// <summary>
/// Composes <see cref="MetricsReaderEngine" /> instances from a prepared context.
/// </summary>
internal static class MetricsReaderEngineBuilder
{
  internal static MetricsReaderEngine Build(MetricsReaderContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    var dependencies = CreateEngineDependencies(context);
    return CreateEngine(dependencies, context.Report);
  }

  private static EngineDependencies CreateEngineDependencies(MetricsReaderContext context)
  {
    var nodeEnumerator = new MetricsNodeEnumerator(context.Report);
    var snapshotBuilder = new SymbolSnapshotBuilder(context.ThresholdProvider, context.SuppressedSymbolIndex);
    var violationAggregator = new SarifViolationAggregator(context.SuppressedSymbolIndex);
    var violationOrderer = new SarifViolationOrderer();

    return new EngineDependencies(nodeEnumerator, snapshotBuilder, violationAggregator, violationOrderer);
  }

  private static MetricsReaderEngine CreateEngine(EngineDependencies dependencies, MetricsReport report)
  {
    return new MetricsReaderEngine(
        dependencies.NodeEnumerator,
        dependencies.SnapshotBuilder,
        dependencies.ViolationAggregator,
        dependencies.ViolationOrderer,
        report);
  }

  private sealed record EngineDependencies(
    MetricsNodeEnumerator NodeEnumerator,
    SymbolSnapshotBuilder SnapshotBuilder,
    SarifViolationAggregator ViolationAggregator,
    SarifViolationOrderer ViolationOrderer);
}

