using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BenScr.Graphs.Tests;

public sealed class ReferenceTests
{
    [Theory]
    [InlineData(13)]
    [InlineData(97)]
    [InlineData(2026)]
    public void RandomGraphsAgreeWithFloydWarshallAndRecursiveReference(int seed)
    {
        var random = new System.Random(seed);
        for (int trial = 0; trial < 120; trial++)
        {
            int count = random.Next(1, 18);
            var edges = new List<(int, int)>();
            var adjacency = new bool[count, count];
            var orderedNeighbors = Enumerable.Range(0, count).Select(_ => new List<int>()).ToArray();
            for (int i = 0; i < count * 3; i++)
            {
                int source = random.Next(count);
                int target = random.Next(count);
                edges.Add((source, target));
                if (!adjacency[source, target])
                {
                    orderedNeighbors[source].Add(target);
                }
                adjacency[source, target] = true;
            }
            var graph = GraphTests.Graph(count, edges.ToArray());
            bool[,] reachable = (bool[,])adjacency.Clone();
            int[,] distance = new int[count, count];
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    distance[i, j] = i == j ? 0 : adjacency[i, j] ? 1 : 1000;
                }
            }
            for (int k = 0; k < count; k++)
            {
                for (int i = 0; i < count; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        reachable[i, j] |= reachable[i, k] && reachable[k, j];
                        distance[i, j] = Math.Min(distance[i, j], distance[i, k] + distance[k, j]);
                    }
                }
            }
            for (int start = 0; start < count; start++)
            {
                int[] expectedSet = Enumerable.Range(0, count).Where(v => v == start || reachable[start, v]).ToArray();
                var breadth = graph.BreadthFirst(start);
                Assert.Equal(expectedSet, breadth.Order());
                Assert.Equal(start, breadth[0]);
                int[] distances = breadth.Select(v => distance[start, v]).ToArray();
                Assert.Equal(distances.Order(), distances);
                var recursive = new List<int>();
                var seen = new HashSet<int>();
                void Visit(int current)
                {
                    if (!seen.Add(current))
                    {
                        return;
                    }
                    recursive.Add(current);
                    foreach (int next in orderedNeighbors[current])
                    {
                        Visit(next);
                    }
                }
                Visit(start);
                Assert.Equal(recursive, graph.DepthFirst(start));
            }
            bool cyclic = Enumerable.Range(0, count).Any(i => reachable[i, i]);
            Assert.Equal(!cyclic, graph.TryTopologicalSort(out var order));
            if (cyclic)
            {
                Assert.Empty(order);
            }
            else
            {
                AssertOrder(count, edges, order);
            }

            var expectedComponents = new List<int[]>();
            var assigned = new bool[count];
            for (int vertex = 0; vertex < count; vertex++)
            {
                if (assigned[vertex])
                {
                    continue;
                }
                int[] members = Enumerable.Range(0, count).Where(v => v == vertex || (reachable[vertex, v] && reachable[v, vertex])).ToArray();
                foreach (int member in members)
                {
                    assigned[member] = true;
                }
                expectedComponents.Add(members);
            }
            var actualComponents = graph.StronglyConnectedComponents();
            Assert.Equal(expectedComponents.Count, actualComponents.Count);
            for (int component = 0; component < expectedComponents.Count; component++)
            {
                Assert.Equal(expectedComponents[component], actualComponents[component]);
            }
        }
    }

    [Fact]
    public void EveryFiveVertexForwardEdgeSubsetHasValidTopologicalOrder()
    {
        var possible = new List<(int, int)>();
        for (int i = 0; i < 5; i++)
        {
            for (int j = i + 1; j < 5; j++)
            {
                possible.Add((i, j));
            }
        }
        for (int mask = 0; mask < 1 << possible.Count; mask++)
        {
            var edges = possible.Where((_, index) => (mask & (1 << index)) != 0).Reverse().ToArray();
            var graph = GraphTests.Graph(5, edges);
            Assert.True(graph.TryTopologicalSort(out var order));
            AssertOrder(5, edges, order);
            Assert.All(graph.StronglyConnectedComponents(), c => Assert.Single(c));
        }
    }

    [Fact]
    public void DeepChainAndCycleDoNotUseCallStack()
    {
        const int Count = 50_000;
        var vertices = Enumerable.Range(0, Count).ToArray();
        var edges = Enumerable.Range(0, Count - 1).Select(i => new DirectedEdge<int>(i, i + 1)).ToList();
        var chain = new DirectedGraph<int>(vertices, edges);
        Assert.Equal(vertices, chain.DepthFirst(0));
        Assert.Equal(vertices, chain.BreadthFirst(0));
        Assert.True(chain.TryTopologicalSort(out var order));
        Assert.Equal(vertices, order);
        var singles = chain.StronglyConnectedComponents();
        Assert.Equal(Count, singles.Count);
        Assert.All(singles, c => Assert.Single(c));
        edges.Add(new(Count - 1, 0));
        var cycle = new DirectedGraph<int>(vertices, edges);
        Assert.False(cycle.TryTopologicalSort(out var empty));
        Assert.Empty(empty);
        Assert.Equal(vertices, cycle.DepthFirst(0));
        Assert.Equal(vertices, Assert.Single(cycle.StronglyConnectedComponents()));
    }

    [Fact]
    public void ComponentPresentationDoesNotDependOnEdgePermutation()
    {
        (int, int)[] edges = { (0, 3), (3, 6), (6, 0), (1, 2), (2, 1), (4, 5), (5, 4), (3, 2), (2, 5) };
        var forward = GraphTests.Graph(7, edges).StronglyConnectedComponents();
        var reversed = GraphTests.Graph(7, edges.Reverse().ToArray()).StronglyConnectedComponents();
        Assert.Equal(forward.Select(c => string.Join(",", c)), reversed.Select(c => string.Join(",", c)));
    }

    private static void AssertOrder(int count, IEnumerable<(int Source, int Target)> edges, IReadOnlyList<int> order)
    {
        Assert.Equal(Enumerable.Range(0, count), order.Order());
        int[] positions = new int[count];
        for (int i = 0; i < count; i++)
        {
            positions[order[i]] = i;
        }
        Assert.All(edges, edge => Assert.True(positions[edge.Source] < positions[edge.Target]));
    }
}
