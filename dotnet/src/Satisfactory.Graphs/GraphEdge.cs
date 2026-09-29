namespace Satisfactory.Graphs;

/// <summary>
/// An immutable directed edge between two nodes of a <see cref="DirectedAcyclicGraph{TNode}"/>, with an optional
/// label.
/// </summary>
/// <typeparam name="TNode">The node type.</typeparam>
/// <param name="From">The node the edge leaves.</param>
/// <param name="To">The node the edge enters.</param>
/// <param name="Label">An optional label carried by the edge; <see langword="null"/> when there is none.</param>
/// <remarks>
/// Edges created by <see cref="DirectedAcyclicGraph{TNode}.AddEdge(TNode, TNode, string?)"/> reference the node
/// instances stored in the graph. A graph may hold several edges with equal endpoints and label (parallel edges);
/// each is kept as its own entry.
/// </remarks>
public sealed record GraphEdge<TNode>(TNode From, TNode To, string? Label = null)
    where TNode : IGraphNode
{
    /// <summary>
    /// Gets the node the edge leaves.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public TNode From { get; init; } = NotNull(From, nameof(From));

    /// <summary>
    /// Gets the node the edge enters.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public TNode To { get; init; } = NotNull(To, nameof(To));

    private static TNode NotNull(TNode node, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(node, parameterName);
        return node;
    }
}
