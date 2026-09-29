using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Core.Tiers;

/// <summary>Orders a tier's new machine groups so that every group comes after the groups that feed it.</summary>
public interface ITierBuildOrder
{
    /// <summary>Computes the build order and assigns each machine group its <see cref="MachineGroupNode.Step"/>.</summary>
    /// <param name="graph">A graph built by <see cref="ITierGraphBuilder"/>.</param>
    /// <returns>The machine groups in build order; step numbers start at 1.</returns>
    IReadOnlyList<MachineGroupNode> Order(DirectedAcyclicGraph<TierGraphNode> graph);
}
