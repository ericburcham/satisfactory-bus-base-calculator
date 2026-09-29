using Satisfactory.Graphs;

namespace Satisfactory.Planner.Core.Tiers.Graph;

/// <summary>A node in a tier graph.</summary>
public abstract class TierGraphNode : IGraphNode
{
    /// <summary>Initializes a new node.</summary>
    /// <param name="id">The node id, used verbatim as the DOT node name.</param>
    protected TierGraphNode(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        Id = id;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <summary>Gets the kind of node.</summary>
    public abstract TierGraphNodeKind Kind { get; }

    /// <summary>Gets the display label; lines are separated by <c>'\n'</c> characters.</summary>
    public abstract string Label { get; }

    /// <inheritdoc />
    public override string ToString() => Id;
}
