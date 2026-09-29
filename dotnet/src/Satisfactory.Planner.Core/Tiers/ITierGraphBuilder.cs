using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Core.Tiers;

/// <summary>Builds the graph of one tier's new machines, their external inputs and the AWESOME Sink.</summary>
public interface ITierGraphBuilder
{
    /// <summary>Builds the tier graph.</summary>
    /// <param name="result">The tier to draw.</param>
    /// <param name="data">The game data, for item and building names.</param>
    /// <param name="extractors">
    /// The extractor catalog that decides whether an external input is a raw resource or comes from the bus.
    /// The Python writes every tier after the last one was planned, so callers pass the final tier's catalog.
    /// </param>
    /// <returns>The graph; machine nodes come first, in <see cref="TierResult.NewMachines"/> order.</returns>
    DirectedAcyclicGraph<TierGraphNode> Build(TierResult result, GameData data, IExtractorCatalog extractors);
}
