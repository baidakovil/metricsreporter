namespace MetricsReporter.Aggregation;

using MetricsReporter.Model;

/// <summary>
/// Holds a namespace node and a reference to the assembly that owns it.
/// </summary>
internal sealed record NamespaceEntry(NamespaceMetricsNode Node, AssemblyMetricsNode Assembly);


