using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Satisfactory.Graphs;

/// <summary>
/// A directed acyclic graph whose nodes are identified by <see cref="IGraphNode.Id"/> (ordinal comparison).
/// </summary>
/// <typeparam name="TNode">The node type.</typeparam>
/// <remarks>
/// <para>
/// Nodes and edges keep their insertion order, and every enumeration the graph returns follows it. Parallel edges
/// (several edges between the same two nodes, with or without equal labels) are allowed and each is kept.
/// Self-loops and edges that would close a cycle are rejected, so the graph is acyclic at all times.
/// </para>
/// <para>
/// Adjacency is stored per node (outgoing and incoming edge lists in edge insertion order), so neighbour queries do
/// not scan the edge list. Cycle checks and reachability are a depth-first search, O(V + E). The class is not thread
/// safe.
/// </para>
/// </remarks>
public class DirectedAcyclicGraph<TNode>
    where TNode : IGraphNode
{
    private readonly List<TNode> _nodes = [];
    private readonly Dictionary<string, int> _indexById = new(StringComparer.Ordinal);
    private readonly List<GraphEdge<TNode>> _edges = [];
    private readonly List<List<GraphEdge<TNode>>> _outgoing = [];
    private readonly List<List<GraphEdge<TNode>>> _incoming = [];
    private readonly ReadOnlyCollection<TNode> _nodesView;
    private readonly ReadOnlyCollection<GraphEdge<TNode>> _edgesView;

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="DirectedAcyclicGraph{TNode}"/> class.
    /// </summary>
    public DirectedAcyclicGraph()
    {
        _nodesView = _nodes.AsReadOnly();
        _edgesView = _edges.AsReadOnly();
    }

    /// <summary>
    /// Gets the nodes in insertion order (a live read-only view).
    /// </summary>
    public IReadOnlyList<TNode> Nodes => _nodesView;

    /// <summary>
    /// Gets the edges in insertion order, parallel edges included (a live read-only view).
    /// </summary>
    public IReadOnlyList<GraphEdge<TNode>> Edges => _edgesView;

    /// <summary>
    /// Adds a node unless a node with the same id is already present.
    /// </summary>
    /// <param name="node">The node to add.</param>
    /// <returns><see langword="true"/> if the node was added; <see langword="false"/> if a node with the same id
    /// already exists (the existing node is kept and <paramref name="node"/> is ignored).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The node's <see cref="IGraphNode.Id"/> is <see langword="null"/>.
    /// </exception>
    public bool AddNode(TNode node)
    {
        string id = IdOf(node);
        if (_indexById.ContainsKey(id))
        {
            return false;
        }

        _indexById.Add(id, _nodes.Count);
        _nodes.Add(node);
        _outgoing.Add([]);
        _incoming.Add([]);
        return true;
    }

    /// <summary>
    /// Gets the stored node with the specified id.
    /// </summary>
    /// <param name="id">The node id (ordinal).</param>
    /// <returns>The stored node.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">No node has the id <paramref name="id"/>.</exception>
    public TNode GetNode(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _indexById.TryGetValue(id, out int index)
            ? _nodes[index]
            : throw new KeyNotFoundException($"The graph has no node with id \"{id}\".");
    }

    /// <summary>
    /// Gets the stored node with the specified id, if present.
    /// </summary>
    /// <param name="id">The node id (ordinal).</param>
    /// <param name="node">The stored node when found; otherwise the default value.</param>
    /// <returns><see langword="true"/> if a node with the id exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    public bool TryGetNode(string id, [MaybeNullWhen(false)] out TNode node)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (_indexById.TryGetValue(id, out int index))
        {
            node = _nodes[index];
            return true;
        }

        node = default;
        return false;
    }

    /// <summary>
    /// Determines whether the graph holds a node with the specified id.
    /// </summary>
    /// <param name="id">The node id (ordinal).</param>
    /// <returns><see langword="true"/> if a node with the id exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    public bool Contains(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _indexById.ContainsKey(id);
    }

    /// <summary>
    /// Adds an edge <paramref name="from"/> -&gt; <paramref name="to"/>. Endpoints not yet in the graph are added
    /// first (from, then to); the edge references the stored node instance for each id.
    /// </summary>
    /// <param name="from">The node the edge leaves.</param>
    /// <param name="to">The node the edge enters.</param>
    /// <param name="label">An optional edge label.</param>
    /// <returns>The edge that was added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="from"/> or <paramref name="to"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A node's <see cref="IGraphNode.Id"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="CycleDetectedException">The edge is a self-loop or would close a cycle; the graph is left
    /// unchanged (no node is added).</exception>
    public GraphEdge<TNode> AddEdge(TNode from, TNode to, string? label = null)
    {
        return TryAddEdge(from, to, label, out GraphEdge<TNode>? edge)
            ? edge
            : throw new CycleDetectedException(from.Id, to.Id);
    }

    /// <summary>
    /// Adds an edge <paramref name="from"/> -&gt; <paramref name="to"/> unless it would be a self-loop or close a
    /// cycle. Endpoints not yet in the graph are added first (from, then to) when the edge is accepted.
    /// </summary>
    /// <param name="from">The node the edge leaves.</param>
    /// <param name="to">The node the edge enters.</param>
    /// <param name="label">An optional edge label.</param>
    /// <param name="edge">The edge that was added, or <see langword="null"/> when it was rejected.</param>
    /// <returns><see langword="true"/> if the edge was added; <see langword="false"/> if it was rejected, in which
    /// case the graph is unchanged (no node is added).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="from"/> or <paramref name="to"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A node's <see cref="IGraphNode.Id"/> is <see langword="null"/>.
    /// </exception>
    public bool TryAddEdge(TNode from, TNode to, string? label, [NotNullWhen(true)] out GraphEdge<TNode>? edge)
    {
        if (WouldCreateCycle(from, to))
        {
            edge = null;
            return false;
        }

        AddNode(from);
        AddNode(to);
        int fromIndex = _indexById[from.Id];
        int toIndex = _indexById[to.Id];
        edge = new GraphEdge<TNode>(_nodes[fromIndex], _nodes[toIndex], label);
        AppendEdge(fromIndex, toIndex, edge);
        return true;
    }

    /// <summary>
    /// Determines whether adding the edge <paramref name="from"/> -&gt; <paramref name="to"/> would create a cycle:
    /// true for a self-loop (equal ids) and when <paramref name="from"/> is already reachable from
    /// <paramref name="to"/>. An endpoint not in the graph cannot close a cycle.
    /// </summary>
    /// <param name="from">The node the edge would leave.</param>
    /// <param name="to">The node the edge would enter.</param>
    /// <returns><see langword="true"/> if the edge would be rejected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="from"/> or <paramref name="to"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A node's <see cref="IGraphNode.Id"/> is <see langword="null"/>.
    /// </exception>
    public bool WouldCreateCycle(TNode from, TNode to)
    {
        string fromId = IdOf(from);
        string toId = IdOf(to);
        return string.Equals(fromId, toId, StringComparison.Ordinal) || HasPath(toId, fromId);
    }

    /// <summary>
    /// Determines whether <paramref name="to"/> is reachable from <paramref name="from"/> along directed edges.
    /// Reachability is reflexive: a node in the graph has a (zero-length) path to itself.
    /// </summary>
    /// <param name="from">The start node.</param>
    /// <param name="to">The target node.</param>
    /// <returns><see langword="true"/> if both nodes are in the graph and a directed path leads from
    /// <paramref name="from"/> to <paramref name="to"/>; <see langword="false"/> otherwise (including when either
    /// node is not in the graph).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="from"/> or <paramref name="to"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A node's <see cref="IGraphNode.Id"/> is <see langword="null"/>.
    /// </exception>
    public bool HasPath(TNode from, TNode to) => HasPath(IdOf(from), IdOf(to));

    /// <summary>
    /// Gets the direct successors of a node: the target of each outgoing edge, in edge insertion order. A node
    /// appears once per parallel edge.
    /// </summary>
    /// <param name="node">A node of the graph (matched by id).</param>
    /// <returns>The successors, one entry per outgoing edge.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The node is not in the graph.</exception>
    public IReadOnlyList<TNode> Successors(TNode node) =>
        _outgoing[IndexOf(node)].Select(edge => edge.To).ToArray();

    /// <summary>
    /// Gets the direct predecessors of a node: the source of each incoming edge, in edge insertion order. A node
    /// appears once per parallel edge.
    /// </summary>
    /// <param name="node">A node of the graph (matched by id).</param>
    /// <returns>The predecessors, one entry per incoming edge.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The node is not in the graph.</exception>
    public IReadOnlyList<TNode> Predecessors(TNode node) =>
        _incoming[IndexOf(node)].Select(edge => edge.From).ToArray();

    /// <summary>
    /// Gets the edges leaving a node, in edge insertion order, parallel edges included.
    /// </summary>
    /// <param name="node">A node of the graph (matched by id).</param>
    /// <returns>A live read-only view of the outgoing edges.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The node is not in the graph.</exception>
    public IReadOnlyList<GraphEdge<TNode>> OutgoingEdges(TNode node) => _outgoing[IndexOf(node)].AsReadOnly();

    /// <summary>
    /// Gets the edges entering a node, in edge insertion order, parallel edges included.
    /// </summary>
    /// <param name="node">A node of the graph (matched by id).</param>
    /// <returns>A live read-only view of the incoming edges.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The node is not in the graph.</exception>
    public IReadOnlyList<GraphEdge<TNode>> IncomingEdges(TNode node) => _incoming[IndexOf(node)].AsReadOnly();

    /// <summary>
    /// Gets the nodes without incoming edges, in node insertion order.
    /// </summary>
    /// <returns>The root nodes.</returns>
    public IReadOnlyList<TNode> Roots() =>
        Enumerable.Range(0, _nodes.Count).Where(i => _incoming[i].Count == 0).Select(i => _nodes[i]).ToArray();

    /// <summary>
    /// Gets the nodes without outgoing edges, in node insertion order.
    /// </summary>
    /// <returns>The leaf nodes.</returns>
    public IReadOnlyList<TNode> Leaves() =>
        Enumerable.Range(0, _nodes.Count).Where(i => _outgoing[i].Count == 0).Select(i => _nodes[i]).ToArray();

    /// <summary>
    /// Computes the longest-path depth of every node: a root has depth 1, any other node has depth
    /// 1 + the maximum depth of its predecessors. This is the number of nodes on the longest path ending at the node.
    /// </summary>
    /// <returns>A dictionary from node id to depth, enumerating in node insertion order.</returns>
    public IReadOnlyDictionary<string, int> LongestPathDepths()
    {
        int[] depth = new int[_nodes.Count];
        foreach (int index in TopologicalIndices(tieBreaker: null))
        {
            int maxPredecessor = 0;
            foreach (GraphEdge<TNode> edge in _incoming[index])
            {
                maxPredecessor = Math.Max(maxPredecessor, depth[_indexById[edge.From.Id]]);
            }

            depth[index] = 1 + maxPredecessor;
        }

        var result = new OrderedDictionary<string, int>(_nodes.Count, StringComparer.Ordinal);
        for (int i = 0; i < _nodes.Count; i++)
        {
            result.Add(_nodes[i].Id, depth[i]);
        }

        return result;
    }

    /// <summary>
    /// Returns a topological order of all nodes, computed with Kahn's algorithm.
    /// </summary>
    /// <param name="tieBreaker">Optional ordering among ready nodes; <see langword="null"/> for insertion order
    /// only.</param>
    /// <returns>Every node exactly once, each before all of its successors.</returns>
    /// <remarks>
    /// The ready set holds every node whose predecessors (counted per edge) have all been emitted. At each step the
    /// ready node emitted next is the smallest by <paramref name="tieBreaker"/>, with equal nodes (comparer result
    /// 0, or no comparer) ordered by node insertion index. Without a comparer the result is therefore: repeatedly
    /// emit the earliest-inserted node whose predecessors have all been emitted. The result is deterministic.
    /// </remarks>
    public IReadOnlyList<TNode> TopologicalSort(IComparer<TNode>? tieBreaker = null) =>
        TopologicalIndices(tieBreaker).Select(i => _nodes[i]).ToArray();

    /// <summary>
    /// Determines whether a sequence is a topological order of this graph: it contains every node of the graph
    /// exactly once (matched by id) and every edge goes from an earlier to a later position.
    /// </summary>
    /// <param name="order">The candidate order.</param>
    /// <returns><see langword="true"/> if <paramref name="order"/> is a topological order; <see langword="false"/>
    /// if it misses a node, repeats one, contains a node not in the graph, or puts an edge backwards.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="order"/> or one of its elements is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An element's <see cref="IGraphNode.Id"/> is <see langword="null"/>.
    /// </exception>
    public bool IsTopologicalOrder(IEnumerable<TNode> order)
    {
        ArgumentNullException.ThrowIfNull(order);
        int[] position = new int[_nodes.Count];
        Array.Fill(position, -1);
        int count = 0;
        foreach (TNode node in order)
        {
            if (!_indexById.TryGetValue(IdOf(node), out int index) || position[index] >= 0)
            {
                return false;
            }

            position[index] = count++;
        }

        if (count != _nodes.Count)
        {
            return false;
        }

        foreach (GraphEdge<TNode> edge in _edges)
        {
            if (position[_indexById[edge.From.Id]] >= position[_indexById[edge.To.Id]])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates a new graph with the nodes selected by <paramref name="keep"/> (in insertion order) and the edges
    /// whose endpoints are both kept (in insertion order, labels kept). Node instances are shared with this graph.
    /// </summary>
    /// <param name="keep">Called once per node, in insertion order; returns <see langword="true"/> to keep it.
    /// </param>
    /// <returns>The induced subgraph.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keep"/> is <see langword="null"/>.</exception>
    public DirectedAcyclicGraph<TNode> Subgraph(Func<TNode, bool> keep)
    {
        ArgumentNullException.ThrowIfNull(keep);
        var subgraph = new DirectedAcyclicGraph<TNode>();
        foreach (TNode node in _nodes)
        {
            if (keep(node))
            {
                subgraph.AddNode(node);
            }
        }

        foreach (GraphEdge<TNode> edge in _edges)
        {
            if (subgraph._indexById.TryGetValue(edge.From.Id, out int fromIndex)
                && subgraph._indexById.TryGetValue(edge.To.Id, out int toIndex))
            {
                // A subgraph of an acyclic graph is acyclic: no cycle check needed.
                subgraph.AppendEdge(fromIndex, toIndex, edge);
            }
        }

        return subgraph;
    }

    private static string IdOf(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.Id ?? throw new ArgumentException("The node's Id must not be null.", nameof(node));
    }

    private int IndexOf(TNode node)
    {
        string id = IdOf(node);
        return _indexById.TryGetValue(id, out int index)
            ? index
            : throw new KeyNotFoundException($"The graph has no node with id \"{id}\".");
    }

    private void AppendEdge(int fromIndex, int toIndex, GraphEdge<TNode> edge)
    {
        _edges.Add(edge);
        _outgoing[fromIndex].Add(edge);
        _incoming[toIndex].Add(edge);
    }

    private bool HasPath(string fromId, string toId)
    {
        if (!_indexById.TryGetValue(fromId, out int start) || !_indexById.TryGetValue(toId, out int target))
        {
            return false;
        }

        if (start == target)
        {
            return true;
        }

        bool[] visited = new bool[_nodes.Count];
        var stack = new Stack<int>();
        visited[start] = true;
        stack.Push(start);
        while (stack.Count > 0)
        {
            foreach (GraphEdge<TNode> edge in _outgoing[stack.Pop()])
            {
                int next = _indexById[edge.To.Id];
                if (next == target)
                {
                    return true;
                }

                if (!visited[next])
                {
                    visited[next] = true;
                    stack.Push(next);
                }
            }
        }

        return false;
    }

    private List<int> TopologicalIndices(IComparer<TNode>? tieBreaker)
    {
        int[] remaining = new int[_nodes.Count];
        var ready = new PriorityQueue<int, int>(new ReadyComparer(_nodes, tieBreaker));
        for (int i = 0; i < _nodes.Count; i++)
        {
            remaining[i] = _incoming[i].Count;
            if (remaining[i] == 0)
            {
                ready.Enqueue(i, i);
            }
        }

        var order = new List<int>(_nodes.Count);
        while (ready.TryDequeue(out int index, out _))
        {
            order.Add(index);
            foreach (GraphEdge<TNode> edge in _outgoing[index])
            {
                int next = _indexById[edge.To.Id];
                if (--remaining[next] == 0)
                {
                    ready.Enqueue(next, next);
                }
            }
        }

        return order;
    }

    /// <summary>
    /// Orders node indices by the optional node comparer, then by insertion index.
    /// </summary>
    private sealed class ReadyComparer(List<TNode> nodes, IComparer<TNode>? tieBreaker) : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            if (tieBreaker is not null)
            {
                int byNode = tieBreaker.Compare(nodes[x], nodes[y]);
                if (byNode != 0)
                {
                    return byNode;
                }
            }

            return x.CompareTo(y);
        }
    }
}
