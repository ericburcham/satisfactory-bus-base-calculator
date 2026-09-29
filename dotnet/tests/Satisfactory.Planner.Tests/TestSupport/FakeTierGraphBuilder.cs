using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>
/// Test double for <see cref="ITierGraphBuilder"/>: returns a fresh empty graph per call, logs <c>graph:{tier}</c> and
/// records the arguments of every call.
/// </summary>
public sealed class FakeTierGraphBuilder(List<string> log) : ITierGraphBuilder
{
    private readonly List<(TierResult Result, GameData Data, IExtractorCatalog Extractors, DirectedAcyclicGraph<TierGraphNode> Graph)> _calls = [];

    public IReadOnlyList<(TierResult Result, GameData Data, IExtractorCatalog Extractors, DirectedAcyclicGraph<TierGraphNode> Graph)> Calls => _calls;

    public DirectedAcyclicGraph<TierGraphNode> Build(TierResult result, GameData data, IExtractorCatalog extractors)
    {
        log.Add($"graph:{result.Tier}");
        var graph = new DirectedAcyclicGraph<TierGraphNode>();
        _calls.Add((result, data, extractors, graph));
        return graph;
    }
}
