using System;
using System.Collections.Generic;
using System.Linq;
using BenScr.Graphs;

var build = new DirectedGraph<string>(
    new[] { "Compile", "Test", "Package", "Docs" },
    new[] { new DirectedEdge<string>("compile", "test"), new("Test", "Package"), new("Compile", "Test") },
    StringComparer.OrdinalIgnoreCase);
Require(build.EdgeCount == 2 && build.ContainsVertex("COMPILE"), "Comparer and deduplication");
Require(build.BreadthFirst("compile").SequenceEqual(new[] { "Compile", "Test", "Package" }), "Breadth-first traversal");
Require(build.DepthFirst("Compile").SequenceEqual(new[] { "Compile", "Test", "Package" }), "Depth-first traversal");
Require(build.TryTopologicalSort(out var order) && order.SequenceEqual(new[] { "Compile", "Docs", "Test", "Package" }), "Topological order");
Console.WriteLine(string.Join(" -> ", order));

var cyclic = new DirectedGraph<int>(new[] { 0, 1, 2, 3 },
    new[] { new DirectedEdge<int>(0, 1), new(1, 2), new(2, 1) });
Require(!cyclic.TryTopologicalSort(out var none) && none.Count == 0, "Cycle detection");
IReadOnlyList<IReadOnlyList<int>> components = cyclic.StronglyConnectedComponents();
Require(components.Select(c => string.Join(",", c)).SequenceEqual(new[] { "0", "1,2", "3" }), "Strong components");
var empty = new DirectedGraph<int>(Array.Empty<int>(), Array.Empty<DirectedEdge<int>>());
Require(empty.TryTopologicalSort(out var emptyOrder) && emptyOrder.Count == 0, "Empty graph");
Console.WriteLine("Comparer, edge deduplication, BFS, DFS, topological order, cycles and components passed.");

static void Require(bool condition, string scenario)
{
    if (!condition)
    {
        throw new InvalidOperationException("Consumer failed: " + scenario);
    }
}
