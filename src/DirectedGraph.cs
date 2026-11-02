using System;
using System.Collections.Generic;

namespace BenScr.Graphs;

/// <summary>An immutable directed graph with explicit vertices, self-loops and deduplicated edges.</summary>
/// <remarks>Input collections are copied. Vertex objects themselves are not cloned: their equality and hash
/// codes must remain stable. Concurrent reads are supported if the vertex type and comparer support them.
/// Inputs must not be mutated during construction. All traversals are iterative and results are read-only.</remarks>
public sealed class DirectedGraph<T> where T : notnull
{
    /// <summary>The inclusive maximum input vertex count.</summary>
    public const int MaximumVertices = 1_000_000;
    /// <summary>The inclusive maximum input edge count, including duplicates.</summary>
    public const int MaximumInputEdges = 4_000_000;

    private readonly T[] vertices;
    private readonly Dictionary<T, int> indices;
    private readonly int[][] successors;
    private readonly int[][] predecessors;

    /// <summary>Copies a graph, preserving vertex order and the first occurrence of each outgoing edge.</summary>
    /// <param name="vertices">Explicit unique vertices, including isolated vertices; at most <see cref="MaximumVertices"/>.</param>
    /// <param name="edges">Edges whose endpoints occur in vertices; at most <see cref="MaximumInputEdges"/> entries.</param>
    /// <param name="comparer">Vertex equality, or null for the default comparer. Must obey the equality/hash contract.</param>
    /// <exception cref="ArgumentNullException">An input collection, vertex or endpoint is null.</exception>
    /// <exception cref="ArgumentException">Vertices are duplicated under the comparer, or an endpoint is absent.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An input count exceeds the documented limit.</exception>
    public DirectedGraph(IReadOnlyList<T> vertices, IReadOnlyList<DirectedEdge<T>> edges, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(edges);
        int vertexCount = vertices.Count;
        int inputEdgeCount = edges.Count;
        if (vertexCount < 0 || vertexCount > MaximumVertices)
        {
            throw new ArgumentOutOfRangeException(nameof(vertices), "The vertex count exceeds the supported range.");
        }
        if (inputEdgeCount < 0 || inputEdgeCount > MaximumInputEdges)
        {
            throw new ArgumentOutOfRangeException(nameof(edges), "The input edge count exceeds the supported range.");
        }
        this.vertices = new T[vertexCount];
        indices = new Dictionary<T, int>(vertexCount, comparer);
        for (int index = 0; index < vertexCount; index++)
        {
            T vertex = vertices[index];
            ArgumentNullException.ThrowIfNull(vertex, nameof(vertices));
            if (!indices.TryAdd(vertex, index))
            {
                throw new ArgumentException("Vertices must be unique under the selected comparer.", nameof(vertices));
            }
            this.vertices[index] = vertex;
        }

        var outgoing = new List<int>?[vertexCount];
        var incoming = new List<int>?[vertexCount];
        var uniqueEdges = new HashSet<ulong>();
        for (int index = 0; index < inputEdgeCount; index++)
        {
            DirectedEdge<T> edge = edges[index];
            ArgumentNullException.ThrowIfNull(edge.Source, nameof(edges));
            ArgumentNullException.ThrowIfNull(edge.Target, nameof(edges));
            if (!indices.TryGetValue(edge.Source, out int source) || !indices.TryGetValue(edge.Target, out int target))
            {
                throw new ArgumentException("Every edge endpoint must occur in the vertex list.", nameof(edges));
            }
            ulong key = ((ulong)(uint)source << 32) | (uint)target;
            if (uniqueEdges.Add(key))
            {
                (outgoing[source] ??= new List<int>()).Add(target);
                (incoming[target] ??= new List<int>()).Add(source);
            }
        }

        successors = new int[vertexCount][];
        predecessors = new int[vertexCount][];
        for (int index = 0; index < vertexCount; index++)
        {
            successors[index] = outgoing[index]?.ToArray() ?? Array.Empty<int>();
            predecessors[index] = incoming[index]?.ToArray() ?? Array.Empty<int>();
        }
        EdgeCount = uniqueEdges.Count;
        Vertices = Array.AsReadOnly(this.vertices);
    }

    /// <summary>Gets the number of vertices, including isolated ones.</summary>
    public int VertexCount => vertices.Length;
    /// <summary>Gets the number of distinct directed edges; a self-loop counts once.</summary>
    public int EdgeCount { get; }
    /// <summary>Gets the read-only vertex list in original input order, retaining the original vertex representatives.</summary>
    public IReadOnlyList<T> Vertices { get; }

    /// <summary>Returns whether a non-null vertex is present under the graph's comparer.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="vertex"/> is null.</exception>
    public bool ContainsVertex(T vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        return indices.ContainsKey(vertex);
    }

    /// <summary>Returns whether an edge exists; absent endpoints return false. Null endpoints are rejected.</summary>
    public bool ContainsEdge(T source, T target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return indices.TryGetValue(source, out int sourceIndex) && indices.TryGetValue(target, out int targetIndex)
            && Array.IndexOf(successors[sourceIndex], targetIndex) >= 0;
    }

    /// <summary>Returns a read-only list of distinct successors in first edge-occurrence order.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="vertex"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="vertex"/> is absent.</exception>
    public IReadOnlyList<T> Successors(T vertex) => Materialize(successors[FindVertex(vertex)]);

    /// <summary>Returns the reachable vertices in breadth-first order, including start first.</summary>
    /// <remarks>Successors are discovered in first edge-occurrence order; each vertex appears once.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="start"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="start"/> is absent.</exception>
    public IReadOnlyList<T> BreadthFirst(T start)
    {
        int first = FindVertex(start);
        var seen = new bool[VertexCount];
        var queue = new Queue<int>();
        var result = new List<int>();
        seen[first] = true;
        queue.Enqueue(first);
        while (queue.TryDequeue(out int current))
        {
            result.Add(current);
            foreach (int next in successors[current])
            {
                if (!seen[next])
                {
                    seen[next] = true;
                    queue.Enqueue(next);
                }
            }
        }
        return Materialize(result);
    }

    /// <summary>Returns recursive-equivalent depth-first preorder, implemented with an explicit stack.</summary>
    /// <remarks>Start appears first; successors are explored in first edge-occurrence order. Each reachable vertex appears once.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="start"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="start"/> is absent.</exception>
    public IReadOnlyList<T> DepthFirst(T start)
    {
        int first = FindVertex(start);
        var seen = new bool[VertexCount];
        var stack = new Stack<(int Vertex, int Next)>();
        var result = new List<int>();
        seen[first] = true;
        result.Add(first);
        stack.Push((first, 0));
        while (stack.TryPop(out var frame))
        {
            if (frame.Next < successors[frame.Vertex].Length)
            {
                int next = successors[frame.Vertex][frame.Next];
                stack.Push((frame.Vertex, frame.Next + 1));
                if (!seen[next])
                {
                    seen[next] = true;
                    result.Add(next);
                    stack.Push((next, 0));
                }
            }
        }
        return Materialize(result);
    }

    /// <summary>Produces a full topological order for an acyclic graph; otherwise returns false and an empty list.</summary>
    /// <remarks>Kahn's algorithm uses a FIFO queue: initial roots follow vertex order, newly eligible vertices follow
    /// successor order. Self-loops are cycles. Empty graphs succeed. A failed sort never exposes a partial order.</remarks>
    public bool TryTopologicalSort(out IReadOnlyList<T> order)
    {
        var remaining = new int[VertexCount];
        var ready = new Queue<int>();
        var result = new List<int>(VertexCount);
        for (int vertex = 0; vertex < VertexCount; vertex++)
        {
            remaining[vertex] = predecessors[vertex].Length;
            if (remaining[vertex] == 0)
            {
                ready.Enqueue(vertex);
            }
        }
        while (ready.TryDequeue(out int vertex))
        {
            result.Add(vertex);
            foreach (int next in successors[vertex])
            {
                if (--remaining[next] == 0)
                {
                    ready.Enqueue(next);
                }
            }
        }
        if (result.Count != VertexCount)
        {
            order = Array.Empty<T>();
            return false;
        }
        order = Materialize(result);
        return true;
    }

    /// <summary>Partitions all vertices into maximal mutually reachable components, using iterative Kosaraju passes.</summary>
    /// <remarks>Components are ordered by their first vertex's position in the input. Members use vertex input order.
    /// This is a stable presentation order, not a topological order. Both outer and inner lists are read-only.</remarks>
    public IReadOnlyList<IReadOnlyList<T>> StronglyConnectedComponents()
    {
        var seen = new bool[VertexCount];
        var finish = new List<int>(VertexCount);
        var frames = new Stack<(int Vertex, int Next)>();
        for (int vertex = 0; vertex < VertexCount; vertex++)
        {
            if (seen[vertex])
            {
                continue;
            }
            seen[vertex] = true;
            frames.Push((vertex, 0));
            while (frames.TryPop(out var frame))
            {
                if (frame.Next == successors[frame.Vertex].Length)
                {
                    finish.Add(frame.Vertex);
                    continue;
                }
                int next = successors[frame.Vertex][frame.Next];
                frames.Push((frame.Vertex, frame.Next + 1));
                if (!seen[next])
                {
                    seen[next] = true;
                    frames.Push((next, 0));
                }
            }
        }

        var componentOf = new int[VertexCount];
        Array.Fill(componentOf, -1);
        var stack = new Stack<int>();
        int componentCount = 0;
        for (int index = finish.Count - 1; index >= 0; index--)
        {
            int vertex = finish[index];
            if (componentOf[vertex] >= 0)
            {
                continue;
            }
            componentOf[vertex] = componentCount;
            stack.Push(vertex);
            while (stack.TryPop(out int current))
            {
                foreach (int next in predecessors[current])
                {
                    if (componentOf[next] < 0)
                    {
                        componentOf[next] = componentCount;
                        stack.Push(next);
                    }
                }
            }
            componentCount++;
        }

        // Reorder once by original vertex indices, independently of DFS tree shape.
        var groups = new List<T>?[componentCount];
        var orderedGroups = new List<List<T>>(componentCount);
        for (int vertex = 0; vertex < VertexCount; vertex++)
        {
            int component = componentOf[vertex];
            if (groups[component] is null)
            {
                groups[component] = new List<T>();
                orderedGroups.Add(groups[component]!);
            }
            groups[component]!.Add(vertices[vertex]);
        }
        var result = new IReadOnlyList<T>[componentCount];
        for (int index = 0; index < componentCount; index++)
        {
            result[index] = orderedGroups[index].AsReadOnly();
        }
        return Array.AsReadOnly(result);
    }

    private int FindVertex(T vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        if (!indices.TryGetValue(vertex, out int index))
        {
            throw new ArgumentException("The vertex is not present in the graph.", nameof(vertex));
        }
        return index;
    }

    private IReadOnlyList<T> Materialize(IReadOnlyList<int> order)
    {
        var result = new T[order.Count];
        for (int index = 0; index < order.Count; index++)
        {
            result[index] = vertices[order[index]];
        }
        return Array.AsReadOnly(result);
    }
}
