using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BenScr.Graphs.Tests;

public sealed class GraphTests
{
    [Fact]
    public void EmptyGraphHasSuccessfulEmptyOrderAndPartition()
    {
        var graph = new DirectedGraph<int>(Array.Empty<int>(), Array.Empty<DirectedEdge<int>>());
        Assert.Equal(0, graph.VertexCount);
        Assert.Equal(0, graph.EdgeCount);
        Assert.Empty(graph.Vertices);
        Assert.True(graph.TryTopologicalSort(out var order));
        Assert.Empty(order);
        Assert.Empty(graph.StronglyConnectedComponents());
        Assert.False(graph.ContainsVertex(0));
        Assert.False(graph.ContainsEdge(0, 0));
        Assert.Throws<ArgumentException>(() => graph.BreadthFirst(0));
    }

    [Fact]
    public void IsolatedVerticesPreserveInputOrder()
    {
        var graph = new DirectedGraph<int>(new[] { 9, 2, 7 }, Array.Empty<DirectedEdge<int>>());
        Assert.True(graph.TryTopologicalSort(out var order));
        Assert.Equal(new[] { 9, 2, 7 }, order);
        Assert.Equal(new[] { 2 }, graph.BreadthFirst(2));
        Assert.Equal(new[] { 7 }, graph.DepthFirst(7));
        Assert.Empty(graph.Successors(9));
        Assert.Equal(new[] { "9", "2", "7" }, graph.StronglyConnectedComponents().Select(c => string.Join(",", c)));
    }

    [Fact]
    public void ParallelEdgesCollapseButOppositeEdgesRemain()
    {
        var graph = Graph(3, (0, 1), (0, 1), (1, 0), (0, 2), (0, 1));
        Assert.Equal(3, graph.EdgeCount);
        Assert.Equal(new[] { 1, 2 }, graph.Successors(0));
        Assert.True(graph.ContainsEdge(0, 1));
        Assert.True(graph.ContainsEdge(1, 0));
        Assert.False(graph.ContainsEdge(2, 0));
        Assert.False(graph.ContainsEdge(5, 0));
        Assert.False(graph.ContainsEdge(0, 5));
    }

    [Fact]
    public void SelfLoopIsSingleEdgeAndCycle()
    {
        var graph = Graph(1, (0, 0), (0, 0));
        Assert.Equal(1, graph.EdgeCount);
        Assert.Equal(new[] { 0 }, graph.DepthFirst(0));
        Assert.Equal(new[] { 0 }, graph.BreadthFirst(0));
        Assert.False(graph.TryTopologicalSort(out var order));
        Assert.Empty(order);
        Assert.Equal(new[] { 0 }, Assert.Single(graph.StronglyConnectedComponents()));
    }

    [Fact]
    public void BreadthFirstUsesDiscoveryOrder()
    {
        var graph = Graph(8, (0, 2), (0, 1), (2, 5), (2, 4), (1, 3), (4, 6), (3, 6));
        Assert.Equal(new[] { 0, 2, 1, 5, 4, 3, 6 }, graph.BreadthFirst(0));
    }

    [Fact]
    public void DepthFirstMatchesRecursiveSuccessorOrderWithCrossEdge()
    {
        // Pre-marking all siblings on push would incorrectly put 2 after 3.
        var graph = Graph(5, (0, 1), (0, 2), (1, 2), (1, 3), (2, 4));
        Assert.Equal(new[] { 0, 1, 2, 4, 3 }, graph.DepthFirst(0));
    }

    [Fact]
    public void TraversalsTerminateOnCyclesAndExcludeDisconnectedVertices()
    {
        var graph = Graph(6, (0, 1), (1, 2), (2, 0), (2, 2), (3, 4));
        Assert.Equal(new[] { 0, 1, 2 }, graph.DepthFirst(0));
        Assert.Equal(new[] { 0, 1, 2 }, graph.BreadthFirst(0));
    }

    [Fact]
    public void TopologicalQueueUsesDocumentedFifoOrder()
    {
        var graph = Graph(6, (0, 4), (0, 3), (1, 5), (4, 5));
        Assert.True(graph.TryTopologicalSort(out var order));
        Assert.Equal(new[] { 0, 1, 2, 4, 3, 5 }, order);
    }

    [Fact]
    public void CycleInDisconnectedComponentDiscardsEntireTopologicalPrefix()
    {
        var graph = Graph(5, (0, 1), (2, 3), (3, 2));
        Assert.False(graph.TryTopologicalSort(out var order));
        Assert.Empty(order);
    }

    [Fact]
    public void ComponentsIncludeSingletonsAndUseVertexOrder()
    {
        var graph = Graph(9, (0, 4), (4, 0), (0, 7), (1, 5), (5, 2), (2, 1), (2, 7), (7, 8), (8, 7), (3, 3));
        Assert.Equal(new[] { "0,4", "1,2,5", "3", "6", "7,8" }, graph.StronglyConnectedComponents().Select(c => string.Join(",", c)));
    }

    [Fact]
    public void ComparerResolvesEdgesAndQueriesToOriginalRepresentatives()
    {
        var graph = new DirectedGraph<string>(new[] { "Start", "END" }, new[] { new DirectedEdge<string>("START", "end"), new("start", "END") }, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(1, graph.EdgeCount);
        Assert.True(graph.ContainsVertex("sTaRt"));
        Assert.True(graph.ContainsEdge("start", "end"));
        Assert.Equal(new[] { "Start", "END" }, graph.BreadthFirst("START"));
        Assert.Equal(new[] { "END" }, graph.Successors("start"));
    }

    [Fact]
    public void DefaultStringComparerIsCaseSensitive()
    {
        var graph = new DirectedGraph<string>(new[] { "a", "A" }, Array.Empty<DirectedEdge<string>>());
        Assert.Equal(2, graph.VertexCount);
        Assert.False(graph.ContainsVertex("b"));
    }

    [Fact]
    public void HashCollisionsDoNotMergeDistinctVertices()
    {
        var graph = new DirectedGraph<int>(new[] { 0, 1, 2 }, new[] { new DirectedEdge<int>(0, 1), new(1, 2) }, new CollisionComparer());
        Assert.Equal(new[] { 0, 1, 2 }, graph.DepthFirst(0));
        Assert.True(graph.TryTopologicalSort(out var order));
        Assert.Equal(new[] { 0, 1, 2 }, order);
    }

    [Fact]
    public void InputsAreCopiedAndResultsCannotMutateGraph()
    {
        int[] vertices = { 0, 1, 2 };
        DirectedEdge<int>[] edges = { new(0, 1), new(1, 2) };
        var graph = new DirectedGraph<int>(vertices, edges);
        vertices[1] = 77;
        edges[0] = new(2, 0);
        Assert.Equal(new[] { 0, 1, 2 }, graph.Vertices);
        Assert.Equal(new[] { 0, 1, 2 }, graph.DepthFirst(0));
        Assert.Throws<NotSupportedException>(() => ((IList<int>)graph.Vertices)[0] = 9);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)graph.Successors(0))[0] = 9);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)graph.BreadthFirst(0))[0] = 9);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)graph.DepthFirst(0))[0] = 9);
        Assert.True(graph.TryTopologicalSort(out var order));
        Assert.Throws<NotSupportedException>(() => ((IList<int>)order)[0] = 9);
        var components = graph.StronglyConnectedComponents();
        Assert.Throws<NotSupportedException>(() => ((IList<IReadOnlyList<int>>)components)[0] = Array.Empty<int>());
        Assert.Throws<NotSupportedException>(() => ((IList<int>)components[0])[0] = 9);
    }

    [Fact]
    public void NullCollectionsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new DirectedGraph<string>(null!, Array.Empty<DirectedEdge<string>>()));
        Assert.Throws<ArgumentNullException>(() => new DirectedGraph<string>(Array.Empty<string>(), null!));
    }

    [Fact]
    public void NullVerticesAndEndpointsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new DirectedGraph<string>(new[] { "a", null! }, Array.Empty<DirectedEdge<string>>()));
        Assert.Throws<ArgumentNullException>(() => new DirectedGraph<string>(new[] { "a" }, new[] { new DirectedEdge<string>(null!, "a") }));
        Assert.Throws<ArgumentNullException>(() => new DirectedGraph<string>(new[] { "a" }, new[] { new DirectedEdge<string>("a", null!) }));
        Assert.Throws<ArgumentNullException>(() => new DirectedGraph<string>(new[] { "a" }, new[] { default(DirectedEdge<string>) }));
    }

    [Fact]
    public void DuplicateVerticesAreRejectedUnderSelectedComparer()
    {
        Assert.Throws<ArgumentException>(() => GraphWithVertices(new[] { 1, 1 }));
        Assert.Throws<ArgumentException>(() => new DirectedGraph<string>(new[] { "a", "A" }, Array.Empty<DirectedEdge<string>>(), StringComparer.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 3)]
    [InlineData(-1, 3)]
    public void UnknownEndpointsAreRejected(int source, int target)
    {
        Assert.Throws<ArgumentException>(() => Graph(3, (source, target)));
    }

    [Fact]
    public void NullQueryArgumentsAreRejected()
    {
        var graph = new DirectedGraph<string>(new[] { "a" }, Array.Empty<DirectedEdge<string>>());
        Assert.Throws<ArgumentNullException>(() => graph.ContainsVertex(null!));
        Assert.Throws<ArgumentNullException>(() => graph.ContainsEdge(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => graph.ContainsEdge("a", null!));
        Assert.Throws<ArgumentNullException>(() => graph.Successors(null!));
        Assert.Throws<ArgumentNullException>(() => graph.BreadthFirst(null!));
        Assert.Throws<ArgumentNullException>(() => graph.DepthFirst(null!));
    }

    [Fact]
    public void MissingTraversalVerticesAreRejected()
    {
        var graph = Graph(2);
        Assert.Throws<ArgumentException>(() => graph.Successors(2));
        Assert.Throws<ArgumentException>(() => graph.BreadthFirst(2));
        Assert.Throws<ArgumentException>(() => graph.DepthFirst(2));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public void VertexCountLimitsAreCheckedBeforeReadingInput(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DirectedGraph<int>(new CountOnly<int>(count), Array.Empty<DirectedEdge<int>>()));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4_000_001)]
    public void EdgeCountLimitsAreCheckedBeforeReadingInput(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DirectedGraph<int>(Array.Empty<int>(), new CountOnly<DirectedEdge<int>>(count)));
    }

    [Fact]
    public void EdgeValueSupportsValueEqualityAndDeconstruction()
    {
        var edge = new DirectedEdge<int>(2, 5);
        var (source, target) = edge;
        Assert.Equal(2, source);
        Assert.Equal(5, target);
        Assert.Equal(new DirectedEdge<int>(2, 5), edge);
        Assert.NotEqual(new DirectedEdge<int>(5, 2), edge);
    }

    [Fact]
    public void RepeatedAndConcurrentReadOperationsAreIndependent()
    {
        var graph = Graph(6, (0, 1), (1, 2), (2, 1), (3, 4), (4, 5));
        Parallel.For(0, 100, _ =>
        {
            Assert.Equal(new[] { 0, 1, 2 }, graph.BreadthFirst(0));
            Assert.Equal(new[] { 3, 4, 5 }, graph.DepthFirst(3));
            Assert.False(graph.TryTopologicalSort(out var order));
            Assert.Empty(order);
            Assert.Equal(new[] { "0", "1,2", "3", "4", "5" }, graph.StronglyConnectedComponents().Select(c => string.Join(",", c)));
        });
    }

    internal static DirectedGraph<int> Graph(int count, params (int Source, int Target)[] edges) =>
        new(Enumerable.Range(0, count).ToArray(), edges.Select(e => new DirectedEdge<int>(e.Source, e.Target)).ToArray());

    private static DirectedGraph<int> GraphWithVertices(int[] vertices) => new(vertices, Array.Empty<DirectedEdge<int>>());

    private sealed class CollisionComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;
        public int GetHashCode(int value) => 0;
    }

    private sealed class CountOnly<T>(int count) : IReadOnlyList<T>
    {
        public int Count => count;
        public T this[int index] => throw new InvalidOperationException("Must validate count before reading.");
        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException("Must not enumerate.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
