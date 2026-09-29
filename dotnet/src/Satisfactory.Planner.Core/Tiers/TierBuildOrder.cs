using System.Diagnostics;
using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Core.Tiers;

/// <summary>
/// The build order of a tier's new machine groups (Python <c>tier_order</c>): simplest first, and every group after
/// the groups that make its inputs.
/// </summary>
/// <remarks>
/// The depth of a group is the number of groups on the longest chain of new machines ending at it (1 when no new
/// machine feeds it), which is the Python <c>d(rc)</c>. The groups, taken in graph insertion order (the Python
/// <c>new</c> order), are stably sorted by (depth, ingredient count, product name, recipe name), names compared
/// ordinally, and numbered from 1. Sorting by depth first makes the order topological.
/// </remarks>
public sealed class TierBuildOrder : ITierBuildOrder
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public IReadOnlyList<MachineGroupNode> Order(DirectedAcyclicGraph<TierGraphNode> graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        DirectedAcyclicGraph<TierGraphNode> machineGraph = graph.Subgraph(node => node is MachineGroupNode);
        IReadOnlyDictionary<string, int> depths = machineGraph.LongestPathDepths();
        MachineGroupNode[] order = machineGraph.Nodes
            .Cast<MachineGroupNode>()
            .OrderBy(node => depths[node.Id])
            .ThenBy(node => node.Recipe.Ingredients.Count)
            .ThenBy(node => node.ProductName, StringComparer.Ordinal)
            .ThenBy(node => node.Recipe.Name, StringComparer.Ordinal)
            .ToArray();

        for (int i = 0; i < order.Length; i++)
        {
            order[i].Step = i + 1;
        }

        Debug.Assert(machineGraph.IsTopologicalOrder(order), "The build order must be a topological order of the machines.");
        return order;
    }
}
