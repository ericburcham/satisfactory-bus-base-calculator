using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>Everything the report writers need about one tier besides its <c>TierResult</c>.</summary>
/// <param name="OutputFolder">The folder the report files are written to.</param>
/// <param name="Data">The game data, for item names.</param>
/// <param name="Graph">The tier graph built by <c>ITierGraphBuilder</c> (with the final tier's extractors).</param>
/// <param name="BuildOrder">
/// The tier's machine groups in build order (from <c>ITierBuildOrder</c>, which also set their
/// <see cref="MachineGroupNode.Step"/>).
/// </param>
/// <param name="Totals">The new machines and power of the tier, summed in build order.</param>
public sealed record TierReportContext(
    string OutputFolder,
    GameData Data,
    DirectedAcyclicGraph<TierGraphNode> Graph,
    IReadOnlyList<MachineGroupNode> BuildOrder,
    TierTotals Totals);
