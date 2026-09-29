using Satisfactory.Graphs;

namespace Satisfactory.Planner.Tests.Graphs;

[TestFixture]
public sealed class DirectedAcyclicGraphTests
{
    private static readonly TestNode A = new("a");
    private static readonly TestNode B = new("b");
    private static readonly TestNode C = new("c");
    private static readonly TestNode D = new("d");
    private static readonly string[] ExpectedDepthKeys = ["d", "a", "b", "c"];
    private static readonly int[] ExpectedDepthValues = [4, 1, 2, 3];
    private static readonly string[] ExpectedSubgraphLabels = ["ac", "dc", "ac2"];

    [Test]
    public void AddNode_DuplicateId_ReturnsFalseAndKeepsOriginal()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        var original = new TestNode("x", "original");
        var duplicate = new TestNode("x", "duplicate");

        bool first = graph.AddNode(original);
        bool second = graph.AddNode(duplicate);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(graph.Nodes, Has.Count.EqualTo(1));
            Assert.That(graph.GetNode("x"), Is.SameAs(original));
        });
    }

    [Test]
    public void NodeIdentity_IsOrdinal()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();

        graph.AddNode(new TestNode("x"));
        bool added = graph.AddNode(new TestNode("X"));

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.True);
            Assert.That(graph.Contains("x"), Is.True);
            Assert.That(graph.Contains("X"), Is.True);
            Assert.That(graph.Contains("y"), Is.False);
        });
    }

    [Test]
    public void GetNode_MissingId_ThrowsKeyNotFound()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddNode(A);

        Assert.That(() => graph.GetNode("missing"), Throws.TypeOf<KeyNotFoundException>().With.Message.Contains("missing"));
    }

    [Test]
    public void TryGetNode_ReturnsStoredNodeOrFalse()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddNode(A);

        bool found = graph.TryGetNode("a", out TestNode? node);
        bool missing = graph.TryGetNode("missing", out TestNode? none);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(node, Is.SameAs(A));
            Assert.That(missing, Is.False);
            Assert.That(none, Is.Null);
        });
    }

    [Test]
    public void Nodes_KeepInsertionOrder()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddNode(C);
        graph.AddNode(A);
        graph.AddNode(D);
        graph.AddEdge(B, A);

        Assert.That(graph.Nodes, Is.EqualTo(new[] { C, A, D, B }));
    }

    [Test]
    public void AddEdge_AddsMissingNodesFromThenTo()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();

        graph.AddEdge(B, A);

        Assert.That(graph.Nodes, Is.EqualTo(new[] { B, A }));
    }

    [Test]
    public void AddEdge_ResolvesEndpointsToStoredInstances()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        var storedA = new TestNode("a", "stored");
        graph.AddNode(storedA);

        GraphEdge<TestNode> edge = graph.AddEdge(new TestNode("a", "other"), B, "l");

        Assert.Multiple(() =>
        {
            Assert.That(edge.From, Is.SameAs(storedA));
            Assert.That(edge.To, Is.SameAs(B));
            Assert.That(edge.Label, Is.EqualTo("l"));
            Assert.That(graph.Edges[0], Is.SameAs(edge));
            Assert.That(graph.Nodes, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void Edges_KeepInsertionOrderAndParallelEdges()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        GraphEdge<TestNode> first = graph.AddEdge(A, B);
        GraphEdge<TestNode> other = graph.AddEdge(C, B);
        GraphEdge<TestNode> parallel = graph.AddEdge(A, B);
        GraphEdge<TestNode> labelled = graph.AddEdge(A, B, "x");

        Assert.Multiple(() =>
        {
            Assert.That(graph.Edges, Has.Count.EqualTo(4));
            Assert.That(graph.Edges[0], Is.SameAs(first));
            Assert.That(graph.Edges[1], Is.SameAs(other));
            Assert.That(graph.Edges[2], Is.SameAs(parallel));
            Assert.That(graph.Edges[3], Is.SameAs(labelled));
            Assert.That(graph.OutgoingEdges(A), Is.EqualTo(new[] { first, parallel, labelled }).AsCollection);
            Assert.That(graph.IncomingEdges(B), Is.EqualTo(new[] { first, other, parallel, labelled }).AsCollection);
            Assert.That(graph.OutgoingEdges(B), Is.Empty);
            Assert.That(graph.IncomingEdges(A), Is.Empty);
        });
    }

    [Test]
    public void SuccessorsAndPredecessors_OneEntryPerEdgeInEdgeOrder()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddEdge(A, C);
        graph.AddEdge(A, B);
        graph.AddEdge(A, C);
        graph.AddEdge(D, C);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Successors(A), Is.EqualTo(new[] { C, B, C }));
            Assert.That(graph.Predecessors(C), Is.EqualTo(new[] { A, A, D }));
            Assert.That(graph.Successors(C), Is.Empty);
            Assert.That(graph.Predecessors(A), Is.Empty);
        });
    }

    [Test]
    public void NeighbourQueries_MatchById()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddEdge(A, B);

        Assert.That(graph.Successors(new TestNode("a", "copy")), Is.EqualTo(new[] { B }));
    }

    [Test]
    public void NeighbourQueries_MissingNode_ThrowKeyNotFound()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddNode(A);

        Assert.Multiple(() =>
        {
            Assert.That(() => graph.Successors(B), Throws.TypeOf<KeyNotFoundException>());
            Assert.That(() => graph.Predecessors(B), Throws.TypeOf<KeyNotFoundException>());
            Assert.That(() => graph.OutgoingEdges(B), Throws.TypeOf<KeyNotFoundException>());
            Assert.That(() => graph.IncomingEdges(B), Throws.TypeOf<KeyNotFoundException>());
        });
    }

    [Test]
    public void SelfLoop_IsRejected()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();

        bool wouldCycle = graph.WouldCreateCycle(A, A);
        bool added = graph.TryAddEdge(A, A, null, out GraphEdge<TestNode>? edge);
        var exception = Assert.Throws<CycleDetectedException>(() => graph.AddEdge(A, new TestNode("a", "copy")));

        Assert.Multiple(() =>
        {
            Assert.That(wouldCycle, Is.True);
            Assert.That(added, Is.False);
            Assert.That(edge, Is.Null);
            Assert.That(exception.FromId, Is.EqualTo("a"));
            Assert.That(exception.ToId, Is.EqualTo("a"));
            Assert.That(exception.Message, Does.Contain("\"a\" -> \"a\""));
            Assert.That(graph.Nodes, Is.Empty);
            Assert.That(graph.Edges, Is.Empty);
        });
    }

    [Test]
    public void ThreeNodeCycle_IsRejected()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddEdge(A, B);
        graph.AddEdge(B, C);

        bool wouldCycle = graph.WouldCreateCycle(C, A);
        bool added = graph.TryAddEdge(C, A, "back", out GraphEdge<TestNode>? edge);
        var exception = Assert.Throws<CycleDetectedException>(() => graph.AddEdge(C, A));

        Assert.Multiple(() =>
        {
            Assert.That(wouldCycle, Is.True);
            Assert.That(added, Is.False);
            Assert.That(edge, Is.Null);
            Assert.That(exception.FromId, Is.EqualTo("c"));
            Assert.That(exception.ToId, Is.EqualTo("a"));
            Assert.That(exception.Message, Does.Contain("\"c\" -> \"a\""));
            Assert.That(graph.Edges, Has.Count.EqualTo(2));
            Assert.That(graph.OutgoingEdges(C), Is.Empty);
        });
    }

    [Test]
    public void TryAddEdge_Rejected_AddsNoNodes()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();

        bool added = graph.TryAddEdge(D, D, null, out _);

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.False);
            Assert.That(graph.Contains("d"), Is.False);
        });
    }

    [Test]
    public void TryAddEdge_Accepted_ReturnsEdge()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();

        bool added = graph.TryAddEdge(A, B, "l", out GraphEdge<TestNode>? edge);

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.True);
            Assert.That(edge, Is.Not.Null);
            Assert.That(edge!.Label, Is.EqualTo("l"));
            Assert.That(graph.Edges, Is.EqualTo(new[] { edge }));
        });
    }

    [Test]
    public void WouldCreateCycle_FalseForForwardAndUnknownEdges()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddEdge(A, B);
        graph.AddEdge(B, C);

        Assert.Multiple(() =>
        {
            Assert.That(graph.WouldCreateCycle(A, C), Is.False);
            Assert.That(graph.WouldCreateCycle(A, B), Is.False);
            Assert.That(graph.WouldCreateCycle(C, D), Is.False);
            Assert.That(graph.WouldCreateCycle(D, A), Is.False);
            Assert.That(graph.WouldCreateCycle(B, A), Is.True);
        });
    }

    [Test]
    public void HasPath_FollowsDirectedEdges()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddEdge(A, B);
        graph.AddEdge(B, C);
        graph.AddNode(D);

        Assert.Multiple(() =>
        {
            Assert.That(graph.HasPath(A, C), Is.True);
            Assert.That(graph.HasPath(A, B), Is.True);
            Assert.That(graph.HasPath(C, A), Is.False);
            Assert.That(graph.HasPath(A, D), Is.False);
            Assert.That(graph.HasPath(A, A), Is.True);
            Assert.That(graph.HasPath(A, new TestNode("missing")), Is.False);
            Assert.That(graph.HasPath(new TestNode("missing"), new TestNode("missing")), Is.False);
        });
    }

    [Test]
    public void RootsAndLeaves_InInsertionOrder()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddNode(D);
        graph.AddEdge(C, B);
        graph.AddEdge(A, B);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Roots(), Is.EqualTo(new[] { D, C, A }));
            Assert.That(graph.Leaves(), Is.EqualTo(new[] { D, B }));
        });
    }

    [Test]
    public void LongestPathDepths_Diamond()
    {
        var graph = Diamond();

        IReadOnlyDictionary<string, int> depths = graph.LongestPathDepths();

        Assert.That(depths, Is.EqualTo(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 2, ["d"] = 3 }));
    }

    [Test]
    public void LongestPathDepths_UsesLongestOfSeveralPaths_AndEnumeratesInInsertionOrder()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddNode(D);
        graph.AddEdge(A, D);
        graph.AddEdge(A, B);
        graph.AddEdge(B, C);
        graph.AddEdge(C, D);
        graph.AddEdge(A, D);

        IReadOnlyDictionary<string, int> depths = graph.LongestPathDepths();

        Assert.Multiple(() =>
        {
            Assert.That(depths.Keys, Is.EqualTo(ExpectedDepthKeys));
            Assert.That(depths.Values, Is.EqualTo(ExpectedDepthValues));
        });
    }

    [Test]
    public void LongestPathDepths_Chain()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddEdge(C, B);
        graph.AddEdge(B, A);
        graph.AddNode(D);

        IReadOnlyDictionary<string, int> depths = graph.LongestPathDepths();

        Assert.That(depths, Is.EqualTo(new Dictionary<string, int> { ["c"] = 1, ["b"] = 2, ["a"] = 3, ["d"] = 1 }));
    }

    [Test]
    public void LongestPathDepths_EmptyGraph_IsEmpty()
    {
        Assert.That(new DirectedAcyclicGraph<TestNode>().LongestPathDepths(), Is.Empty);
    }

    [Test]
    public void TopologicalSort_WithoutTieBreaker_UsesInsertionOrderAmongReadyNodes()
    {
        // Insertion order: d, c, b, a. Edges d -> a, c -> b.
        // Ready {d, c} -> d; ready {c, a} -> c (inserted before a); ready {b, a} -> b; then a.
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddNode(D);
        graph.AddNode(C);
        graph.AddNode(B);
        graph.AddNode(A);
        graph.AddEdge(D, A);
        graph.AddEdge(C, B);

        IReadOnlyList<TestNode> order = graph.TopologicalSort();

        Assert.That(order, Is.EqualTo(new[] { D, C, B, A }));
    }

    [Test]
    public void TopologicalSort_Diamond_WithoutTieBreaker()
    {
        Assert.That(Diamond().TopologicalSort(), Is.EqualTo(new[] { A, B, C, D }));
    }

    [Test]
    public void TopologicalSort_WithTieBreaker_PicksSmallestReadyNode()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddNode(A);
        graph.AddNode(B);
        graph.AddNode(C);
        graph.AddEdge(B, D);
        IComparer<TestNode> descending = Comparer<TestNode>.Create((x, y) => string.CompareOrdinal(y.Id, x.Id));

        IReadOnlyList<TestNode> order = graph.TopologicalSort(descending);

        // Ready {a, b, c} -> c, then b; releasing d gives ready {a, d} -> d, then a.
        Assert.That(order, Is.EqualTo(new[] { C, B, D, A }));
    }

    [Test]
    public void TopologicalSort_TieBreakerEquality_FallsBackToInsertionOrder()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddNode(C);
        graph.AddNode(A);
        graph.AddNode(B);
        IComparer<TestNode> allEqual = Comparer<TestNode>.Create((_, _) => 0);

        Assert.That(graph.TopologicalSort(allEqual), Is.EqualTo(new[] { C, A, B }));
    }

    [Test]
    public void TopologicalSort_TieBreakerNeverViolatesEdges()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddEdge(D, A);
        graph.AddEdge(C, A);
        IComparer<TestNode> ascending = Comparer<TestNode>.Create((x, y) => string.CompareOrdinal(x.Id, y.Id));

        IReadOnlyList<TestNode> order = graph.TopologicalSort(ascending);

        Assert.Multiple(() =>
        {
            Assert.That(order, Is.EqualTo(new[] { C, D, A }));
            Assert.That(graph.IsTopologicalOrder(order), Is.True);
        });
    }

    [Test]
    public void TopologicalSort_ParallelEdges_ReleaseNodeAfterAllEdges()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddNode(B);
        graph.AddEdge(A, B);
        graph.AddEdge(A, B);
        graph.AddEdge(C, B);

        Assert.That(graph.TopologicalSort(), Is.EqualTo(new[] { A, C, B }));
    }

    [Test]
    public void IsTopologicalOrder_AcceptsValidOrders()
    {
        var graph = Diamond();

        Assert.Multiple(() =>
        {
            Assert.That(graph.IsTopologicalOrder(new[] { A, B, C, D }), Is.True);
            Assert.That(graph.IsTopologicalOrder(new[] { A, C, B, D }), Is.True);
            Assert.That(graph.IsTopologicalOrder(new[] { new TestNode("a", "copy"), C, B, D }), Is.True);
            Assert.That(new DirectedAcyclicGraph<TestNode>().IsTopologicalOrder([]), Is.True);
        });
    }

    [Test]
    public void IsTopologicalOrder_RejectsInvalidOrders()
    {
        var graph = Diamond();

        Assert.Multiple(() =>
        {
            Assert.That(graph.IsTopologicalOrder(new[] { B, A, C, D }), Is.False, "backward edge");
            Assert.That(graph.IsTopologicalOrder(new[] { A, B, D, C }), Is.False, "backward edge to sink");
            Assert.That(graph.IsTopologicalOrder(new[] { A, B, C }), Is.False, "missing node");
            Assert.That(graph.IsTopologicalOrder(new[] { A, B, C, D, D }), Is.False, "duplicate node");
            Assert.That(graph.IsTopologicalOrder(new[] { A, B, B, D }), Is.False, "duplicate instead of missing");
            Assert.That(graph.IsTopologicalOrder(new[] { A, B, C, D, new TestNode("e") }), Is.False, "unknown node");
        });
    }

    [Test]
    public void Subgraph_KeepsChosenNodesAndTheirEdgesInOrder()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddNode(D);
        graph.AddEdge(A, B, "ab");
        graph.AddEdge(B, C, "bc");
        graph.AddEdge(A, C, "ac");
        graph.AddEdge(D, C, "dc");
        graph.AddEdge(A, C, "ac2");

        DirectedAcyclicGraph<TestNode> subgraph = graph.Subgraph(node => node.Id != "b");

        Assert.Multiple(() =>
        {
            Assert.That(subgraph.Nodes, Is.EqualTo(new[] { D, A, C }));
            Assert.That(subgraph.Edges.Select(edge => edge.Label), Is.EqualTo(ExpectedSubgraphLabels));
            Assert.That(subgraph.Edges[0], Is.SameAs(graph.Edges[2]));
            Assert.That(subgraph.IncomingEdges(C).Select(edge => edge.Label), Is.EqualTo(ExpectedSubgraphLabels));
            Assert.That(subgraph.Contains("b"), Is.False);
            Assert.That(graph.Nodes, Has.Count.EqualTo(4), "source graph unchanged");
            Assert.That(graph.Edges, Has.Count.EqualTo(5), "source graph unchanged");
        });
    }

    [Test]
    public void Subgraph_IsIndependentOfSource()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddEdge(A, B);

        DirectedAcyclicGraph<TestNode> subgraph = graph.Subgraph(_ => true);
        subgraph.AddEdge(B, C);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Contains("c"), Is.False);
            Assert.That(subgraph.WouldCreateCycle(C, A), Is.True);
        });
    }

    [Test]
    public void ScalesToThousandsOfNodes()
    {
        const int Count = 3000;
        var graph = new DirectedAcyclicGraph<TestNode>();
        var nodes = Enumerable.Range(0, Count).Select(i => new TestNode("n" + i.ToString(System.Globalization.CultureInfo.InvariantCulture))).ToArray();
        for (int i = 1; i < Count; i++)
        {
            graph.AddEdge(nodes[i - 1], nodes[i]);
            if (i >= 2)
            {
                graph.AddEdge(nodes[i - 2], nodes[i]);
            }
        }

        IReadOnlyDictionary<string, int> depths = graph.LongestPathDepths();
        IReadOnlyList<TestNode> order = graph.TopologicalSort();

        Assert.Multiple(() =>
        {
            Assert.That(depths["n" + (Count - 1).ToString(System.Globalization.CultureInfo.InvariantCulture)], Is.EqualTo(Count));
            Assert.That(graph.IsTopologicalOrder(order), Is.True);
            Assert.That(graph.WouldCreateCycle(nodes[^1], nodes[0]), Is.True);
        });
    }

    [Test]
    public void NullArguments_Throw()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddNode(A);

        Assert.Multiple(() =>
        {
            Assert.That(() => graph.AddNode(null!), Throws.ArgumentNullException);
            Assert.That(() => graph.AddNode(new TestNode(null!)), Throws.ArgumentException);
            Assert.That(() => graph.GetNode(null!), Throws.ArgumentNullException);
            Assert.That(() => graph.TryGetNode(null!, out _), Throws.ArgumentNullException);
            Assert.That(() => graph.Contains(null!), Throws.ArgumentNullException);
            Assert.That(() => graph.AddEdge(null!, A), Throws.ArgumentNullException);
            Assert.That(() => graph.AddEdge(A, null!), Throws.ArgumentNullException);
            Assert.That(() => graph.TryAddEdge(null!, A, null, out _), Throws.ArgumentNullException);
            Assert.That(() => graph.TryAddEdge(A, null!, null, out _), Throws.ArgumentNullException);
            Assert.That(() => graph.WouldCreateCycle(null!, A), Throws.ArgumentNullException);
            Assert.That(() => graph.WouldCreateCycle(A, null!), Throws.ArgumentNullException);
            Assert.That(() => graph.HasPath(null!, A), Throws.ArgumentNullException);
            Assert.That(() => graph.HasPath(A, null!), Throws.ArgumentNullException);
            Assert.That(() => graph.Successors(null!), Throws.ArgumentNullException);
            Assert.That(() => graph.Predecessors(null!), Throws.ArgumentNullException);
            Assert.That(() => graph.OutgoingEdges(null!), Throws.ArgumentNullException);
            Assert.That(() => graph.IncomingEdges(null!), Throws.ArgumentNullException);
            Assert.That(() => graph.IsTopologicalOrder(null!), Throws.ArgumentNullException);
            Assert.That(() => graph.IsTopologicalOrder(new TestNode[] { null! }), Throws.ArgumentNullException);
            Assert.That(() => graph.Subgraph(null!), Throws.ArgumentNullException);
            Assert.That(graph.Nodes, Is.EqualTo(new[] { A }), "graph unchanged");
        });
    }

    [Test]
    public void Views_AreReadOnly()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddEdge(A, B);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Nodes, Is.Not.InstanceOf<List<TestNode>>());
            Assert.That(graph.Edges, Is.Not.InstanceOf<List<GraphEdge<TestNode>>>());
            Assert.That(graph.OutgoingEdges(A), Is.Not.InstanceOf<List<GraphEdge<TestNode>>>());
        });
    }

    [Test]
    public void IsNotSealed()
    {
        Assert.That(typeof(DirectedAcyclicGraph<TestNode>).IsSealed, Is.False);
    }

    private static DirectedAcyclicGraph<TestNode> Diamond()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        graph.AddEdge(A, B);
        graph.AddEdge(A, C);
        graph.AddEdge(B, D);
        graph.AddEdge(C, D);
        return graph;
    }
}
