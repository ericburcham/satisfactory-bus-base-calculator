using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>
/// Test double for <see cref="ITierBuildOrder"/>: returns a new (empty) list per call, logs <c>order</c> and records
/// each graph with the list it returned.
/// </summary>
public sealed class FakeTierBuildOrder(List<string> log) : ITierBuildOrder
{
    private readonly List<(DirectedAcyclicGraph<TierGraphNode> Graph, IReadOnlyList<MachineGroupNode> Order)> _calls = [];

    public IReadOnlyList<(DirectedAcyclicGraph<TierGraphNode> Graph, IReadOnlyList<MachineGroupNode> Order)> Calls => _calls;

    public IReadOnlyList<MachineGroupNode> Order(DirectedAcyclicGraph<TierGraphNode> graph)
    {
        log.Add("order");
        var order = new List<MachineGroupNode>();
        _calls.Add((graph, order));
        return order;
    }
}
