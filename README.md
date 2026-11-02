# BenScr.Graphs

Immutable directed graphs, deterministic traversal, topological sorting and strongly connected
components for .NET 10. There are no runtime package dependencies.

Created by Ben Schneider — https://github.com/Ben-Scr

```shell
dotnet add package BenScr.Graphs --version 1.0.0
```

```csharp
using BenScr.Graphs;

var graph = new DirectedGraph<string>(
    new[] { "Compile", "Test", "Package", "Docs" },
    new[]
    {
        new DirectedEdge<string>("Compile", "Test"),
        new DirectedEdge<string>("Test", "Package")
    });

if (graph.TryTopologicalSort(out var order))
{
    // Compile, Docs, Test, Package
    Console.WriteLine(string.Join(" -> ", order));
}

// Compile, Test, Package; Docs is isolated.
IReadOnlyList<string> reachable = graph.BreadthFirst("Compile");
IReadOnlyList<IReadOnlyList<string>> components = graph.StronglyConnectedComponents();
```

## Construction and ownership

The explicit vertex list includes isolated vertices. Vertices must be unique under the selected
`IEqualityComparer<T>` (the default comparer is used when null). Every edge endpoint must already
occur in that list. Duplicate edges collapse into one edge; opposite edges are distinct. Self-loops
are allowed and form cycles. Endpoints equivalent under a custom comparer resolve to the original
vertex representative from the vertex list.

Input collections are copied, so changing their entries after construction cannot change the graph.
Vertex objects are **not deep-cloned**. Their equality and hash codes must remain stable for the graph's
lifetime. Inputs must not be mutated during construction. Concurrent reads are supported when the
vertex type and comparer are also safe for concurrent reads. No locks, callbacks, background tasks,
mutable graph operations or recursion are involved in graph algorithms.

Null collections, vertices and endpoints throw `ArgumentNullException`. Duplicate vertices and
unknown edge endpoints throw `ArgumentException`. Counts outside 0–1,000,000 vertices or
0–4,000,000 input edges throw `ArgumentOutOfRangeException`; duplicates count toward the input limit.
These are upper bounds, not a guarantee that a process has enough memory for the requested graph.

## Queries and ordering

| API | Contract |
| --- | --- |
| `Vertices` | Original vertex order, read-only. |
| `ContainsVertex`, `ContainsEdge` | Missing vertices return false; null arguments throw. |
| `Successors(vertex)` | Distinct successors in first edge-occurrence order. |
| `BreadthFirst(start)` | Start first, then FIFO discovery in successor order. |
| `DepthFirst(start)` | Recursive-equivalent preorder in successor order, implemented with explicit frames. |
| `TryTopologicalSort(out order)` | All vertices on success; false and an empty result for any cycle, including a self-loop. |
| `StronglyConnectedComponents()` | Maximal mutually reachable groups covering every vertex exactly once. |

Traversal and `Successors` reject absent vertices with `ArgumentException`. Every reachable vertex
appears once. Results use original vertex representatives, are read-only, and do not expose graph
storage for mutation.

Topological sorting uses Kahn's algorithm with a FIFO queue. Initial zero-indegree vertices follow
vertex order; newly eligible vertices enter the queue in successor order. This is deterministic,
but does not promise the lexicographically smallest topological order. An empty graph sorts successfully.
A graph with any cycle returns no partial prefix, even when other components are acyclic.

Strong components use two iterative Kosaraju passes. Components are presented in the order of their
first vertex in the input; members also follow vertex input order. This presentation is independent
of edge permutation and is **not a topological component order**. A single isolated vertex is a
component, as is a single vertex with a self-loop.

## Cost and scope

Let V be all vertices, E distinct edges and Eᵣ edges out of reachable vertices. Construction uses
expected O(V + input edges) work with well-behaved hashing and O(V + E) retained storage. It stores
both forward and reverse adjacency lists. `ContainsVertex` is expected O(1), `ContainsEdge` and
`Successors` are O(outdegree). BFS/DFS take O(V + Eᵣ) work and O(V) auxiliary storage because their
visited array covers the graph. Topological sort and strong components use O(V + E) work and O(V)
auxiliary storage, excluding returned objects. Custom comparers with pathological hashes can make
dictionary lookup slower. No allocation-free or hard real-time guarantees are made.

This package provides unweighted directed graphs. It does not provide weighted shortest paths,
automatic dependency discovery, persistent storage or dynamic graph editing. Construct a new graph
when the structure changes.

## Build and validation

Install the SDK pinned in `global.json` (10.0.303) and PowerShell 7, then run from a clean checkout:

```powershell
./tools/Verify.ps1
```

The script restores locked dependencies, builds with warnings as errors, checks formatting and the
public API baseline, runs tests and the example, packs the library, audits archive contents and
portable PDBs/Source Link, and runs a fresh package consumer with a separate empty cache.
CI runs the same process on Windows and Ubuntu. Tests include 360 random-graph comparisons against
Floyd–Warshall reachability, 1,024 DAG edge subsets, recursive reference preorder for small graphs,
and 50,000-vertex chains and cycles. See [VERIFICATION.md](VERIFICATION.md) for evidence scope and
[RELEASE.md](RELEASE.md) for publication checks.

The package includes XML documentation, a separate symbol package, the unchanged common
`BenScr_Icon_Master.png`, and the original custom [license](LICENSE.txt).
