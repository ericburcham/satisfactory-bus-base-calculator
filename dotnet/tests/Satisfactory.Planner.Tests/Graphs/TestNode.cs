using Satisfactory.Graphs;

namespace Satisfactory.Planner.Tests.Graphs;

/// <summary>
/// A minimal graph node for the graph tests; <see cref="Tag"/> distinguishes instances that share an id.
/// </summary>
public sealed record TestNode(string Id, string Tag = "") : IGraphNode
{
    public override string ToString() => Id;
}
