namespace MetricsReporter.Aggregation;

using MetricsReporter.Model;

/// <summary>
/// Carries a type node alongside the assembly that owns it.
/// </summary>
internal sealed record TypeEntry(TypeMetricsNode Node, AssemblyMetricsNode Assembly);


