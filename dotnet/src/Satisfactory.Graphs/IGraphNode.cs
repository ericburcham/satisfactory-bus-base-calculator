namespace Satisfactory.Graphs;

/// <summary>
/// A node that can be stored in a <see cref="DirectedAcyclicGraph{TNode}"/>.
/// </summary>
/// <remarks>
/// Node identity is the <see cref="Id"/>, compared ordinally: two nodes with the same id are the same graph node.
/// </remarks>
public interface IGraphNode
{
    /// <summary>
    /// Gets the identifier that uniquely identifies the node within a graph (compared ordinally). Must not be
    /// <see langword="null"/>.
    /// </summary>
    string Id { get; }
}
