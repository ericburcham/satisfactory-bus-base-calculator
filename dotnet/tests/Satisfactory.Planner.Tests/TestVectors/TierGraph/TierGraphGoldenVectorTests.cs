using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Core.Tiers.Graph;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.TestVectors.TierGraph;

/// <summary>
/// Cross-checks <see cref="TierGraphBuilder"/> and <see cref="TierBuildOrder"/> against the golden
/// <c>tier_N.dot</c> files on the real game data, without the .NET planner: each tier's result is rebuilt from the
/// Python planner's decisions (<c>tier_graph_vectors.json</c>), and the graph is re-rendered here with the Python
/// <c>write_tier</c> DOT statements.
/// </summary>
[TestFixture]
public sealed class TierGraphGoldenVectorTests
{
    private static readonly string[] Header =
    [
        "digraph tier {",
        "  rankdir=LR; node [shape=box, style=\"rounded,filled\", fontname=\"Helvetica\", fontsize=10, fillcolor=\"#ffffff\"];",
        "  edge [color=\"#888888\", arrowsize=0.6];",
    ];

    private GameData _data = null!;
    private IExtractorCatalog _finalTierExtractors = null!;

    private static IEnumerable<int> AllTiers() =>
        Enumerable.Range(PlannerConstants.FirstTier, PlannerConstants.LastTier - PlannerConstants.FirstTier + 1);

    [OneTimeSetUp]
    public void LoadRealData()
    {
        _data = CoreRealGameData.Data;
        var contexts = new TierContextFactory(_data, new UnlockTierResolver(_data), MinerTier.Mk1, NodePurity.Normal);
        _finalTierExtractors = contexts.Create(PlannerConstants.LastTier).Extractors;
    }

    [Test]
    public void Vectors_CoverEveryTier()
    {
        Assert.That(TierGraphVectors.Instance.Tiers.Select(t => t.Tier), Is.EqualTo(AllTiers()));
    }

    [TestCaseSource(nameof(AllTiers))]
    public void BuildOrder_IsThePythonTierOrder(int tier)
    {
        (_, IReadOnlyList<MachineGroupNode> order) = BuildTier(tier);

        Assert.That(order.Select(m => m.Id), Is.EqualTo(Vector(tier).Order));
    }

    [TestCaseSource(nameof(AllTiers))]
    public void MachineStatements_AppearInTheGoldenFileInBuildOrder(int tier)
    {
        (_, IReadOnlyList<MachineGroupNode> order) = BuildTier(tier);
        List<string> golden = GoldenLines(tier);

        IEnumerable<string> goldenMachines = golden.Where(line => line.Contains(" [label=\"#", StringComparison.Ordinal));
        Assert.That(goldenMachines, Is.EqualTo(order.Select(MachineStatement)));
    }

    [TestCaseSource(nameof(AllTiers))]
    public void RenderedGraph_MatchesTheGoldenDotFile(int tier)
    {
        (DirectedAcyclicGraph<TierGraphNode> graph, IReadOnlyList<MachineGroupNode> order) = BuildTier(tier);
        List<string> rendered = Render(tier, graph, order);
        List<string> golden = GoldenLines(tier);

        Assert.Multiple(() =>
        {
            // Everything but the external input declarations is in a fixed order (the Python writes those from a set).
            Assert.That(rendered.Where(IsNotInputDeclaration), Is.EqualTo(golden.Where(IsNotInputDeclaration)));
            Assert.That(Sorted(rendered), Is.EqualTo(Sorted(golden)));
        });
    }

    private static bool IsNotInputDeclaration(string line) =>
        !line.StartsWith("  \"" + ExternalInputNode.IdPrefix, StringComparison.Ordinal) || line.Contains(" -> ", StringComparison.Ordinal);

    private static string[] Sorted(IEnumerable<string> lines) => lines.Order(StringComparer.Ordinal).ToArray();

    private static TierGraphVector Vector(int tier) => TierGraphVectors.Instance.Tiers.Single(t => t.Tier == tier);

    private static List<string> GoldenLines(int tier)
    {
        string text = File.ReadAllText(Path.Combine(RepositoryPaths.GoldenOutput, $"tier_{tier}.dot"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        return text.TrimEnd('\n').Split('\n').ToList();
    }

    private static string Escape(string label) => label.Replace("\n", "\\n", StringComparison.Ordinal);

    private static string MachineStatement(MachineGroupNode machine) =>
        $"  \"{machine.Id}\" [label=\"{Escape(machine.Label)}\"{(machine.MakesNewItem ? ", penwidth=2.5, color=\"#2a7a2a\"" : string.Empty)}];";

    /// <summary>The Python <c>write_tier</c> DOT statements (878-906) for the graph.</summary>
    private static List<string> Render(int tier, DirectedAcyclicGraph<TierGraphNode> graph, IReadOnlyList<MachineGroupNode> order)
    {
        var lines = new List<string>(Header)
        {
            $"  labelloc=t; label=\"{TierNames.For(tier)}: {order.Sum(m => m.NewMachines)} new machines\"; fontsize=18;",
        };
        foreach (MachineGroupNode machine in order)
        {
            lines.Add(MachineStatement(machine));
            lines.AddRange(graph.IncomingEdges(machine).Select(e => $"  \"{e.From.Id}\" -> \"{machine.Id}\";"));
        }

        lines.AddRange(graph.Nodes.OfType<ExternalInputNode>()
            .Where(n => graph.OutgoingEdges(n).Any(e => e.To is MachineGroupNode))
            .Select(n => $"  \"{n.Id}\" [label=\"{Escape(n.Label)}\", shape=ellipse, fillcolor=\"#eeeeee\"];"));
        if (graph.TryGetNode(SinkNode.SinkId, out TierGraphNode? sink))
        {
            lines.Add($"  \"{sink.Id}\" [label=\"{sink.Label}\", shape=doubleoctagon, fillcolor=\"#ffd6d6\"];");
            foreach (GraphEdge<TierGraphNode> edge in graph.IncomingEdges(sink))
            {
                if (edge.From is ExternalInputNode input && !graph.OutgoingEdges(input).Any(e => e.To is MachineGroupNode))
                {
                    lines.Add($"  \"{input.Id}\" [label=\"{Escape(input.Label)}\", shape=ellipse, fillcolor=\"#eeeeee\"];");
                }

                lines.Add(
                    $"  \"{edge.From.Id}\" -> \"SINK\" [color=\"#cc3333\", style=dashed, label=\"{edge.Label}\", fontcolor=\"#cc3333\"];");
            }
        }

        lines.Add("}");
        return lines;
    }

    private (DirectedAcyclicGraph<TierGraphNode> Graph, IReadOnlyList<MachineGroupNode> Order) BuildTier(int tier)
    {
        TierGraphVector vector = Vector(tier);
        TierResult result = TierResultFactory.Create(
            _data, vector.Tier, vector.NewMachines, vector.NewItems, vector.Sink, vector.Recipes);
        DirectedAcyclicGraph<TierGraphNode> graph = new TierGraphBuilder().Build(result, _data, _finalTierExtractors);
        IReadOnlyList<MachineGroupNode> order = new TierBuildOrder().Order(graph);
        return (graph, order);
    }
}
