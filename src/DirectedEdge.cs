namespace BenScr.Graphs;

/// <summary>A directed edge from <paramref name="Source"/> to <paramref name="Target"/>.</summary>
/// <param name="Source">The source vertex; must be non-null when supplied to a graph.</param>
/// <param name="Target">The destination vertex; must be non-null when supplied to a graph.</param>
/// <remarks>The graph uses its own comparer to identify endpoints, independently of this value's equality.</remarks>
public readonly record struct DirectedEdge<T>(T Source, T Target) where T : notnull;
