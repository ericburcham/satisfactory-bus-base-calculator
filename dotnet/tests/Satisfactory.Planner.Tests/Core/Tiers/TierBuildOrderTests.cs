using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Core.Tiers.Graph;
using Satisfactory.Planner.Tests.TestSupport;
using Satisfactory.Planner.Tests.TestVectors.TierGraph;

namespace Satisfactory.Planner.Tests.Core.Tiers;

[TestFixture]
public sealed class TierBuildOrderTests
{
    private const string Ore = "Desc_OreIron_C";
    private const string Water = "Desc_Water_C";
    private const string Ingot = "Desc_IronIngot_C";
    private const string Plate = "Desc_IronPlate_C";
    private const string Rod = "Desc_IronRod_C";
    private const string Screw = "Desc_IronScrew_C";
    private const string Frame = "Desc_ModularFrame_C";
    private const string Reinforced = "Desc_IronPlateReinforced_C";
    private const string Rubber = "Desc_Rubber_C";
    private const string Constructor = "Desc_ConstructorMk1_C";

    private static readonly GameData Data = new GameDataBuilder()
        .WithItem(Ore, "Iron Ore").WithResource(Ore)
        .WithFluid(Water, "Water").WithResource(Water)
        .WithItem(Ingot, "Iron Ingot")
        .WithItem(Plate, "Iron Plate")
        .WithItem(Rod, "Iron Rod")
        .WithItem(Screw, "Screw")
        .WithItem(Frame, "Modular Frame")
        .WithItem(Reinforced, "Reinforced Iron Plate")
        .WithItem(Rubber, "Rubber")
        .WithItem("Desc_Zeta_C", "Zeta Part")
        .WithItem("Desc_AlphaUpper_C", "Alpha Part")
        .WithItem("Desc_BetaUpper_C", "Beta Part")
        .WithItem("Desc_AlphaLower_C", "alpha Part")
        .WithItem("Desc_Twin_C", "Twin")
        .WithItem("Desc_Gamma_C", "Gamma")
        .WithItem("Desc_Zulu_C", "Zulu Part")
        .WithBuilding(Constructor, "Constructor", 4)
        .WithRecipe("Recipe_IngotIron_C", "Iron Ingot", Constructor, 2, [(Ore, 1)], [(Ingot, 1)])
        .WithRecipe("Recipe_IronPlate_C", "Iron Plate", Constructor, 6, [(Ingot, 3)], [(Plate, 2)])
        .WithRecipe("Recipe_IronRod_C", "Iron Rod", Constructor, 4, [(Ingot, 1)], [(Rod, 1)])
        .WithRecipe("Recipe_Screw_C", "Screw", Constructor, 6, [(Rod, 1)], [(Screw, 4)])
        .WithRecipe("Recipe_ModularFrame_C", "Modular Frame", Constructor, 60, [(Plate, 3), (Rod, 12)], [(Frame, 2)])
        .WithRecipe(
            "Recipe_Alternate_AdheredPlate_C", "Alternate: Adhered Iron Plate", Constructor, 16,
            [(Ingot, 3), (Screw, 1)], [(Reinforced, 1)], alternate: true)
        .WithRecipe("Recipe_Zeta_C", "Zeta Part", Constructor, 2, [(Ore, 1)], [("Desc_Zeta_C", 1)])
        .WithRecipe("Recipe_AlphaUpper_C", "Alpha Part", Constructor, 2, [(Ore, 1), (Water, 1)], [("Desc_AlphaUpper_C", 1)])
        .WithRecipe("Recipe_BetaUpper_C", "Beta Part", Constructor, 2, [(Ore, 1)], [("Desc_BetaUpper_C", 1)])
        .WithRecipe("Recipe_AlphaLower_C", "alpha Part", Constructor, 2, [(Ore, 1)], [("Desc_AlphaLower_C", 1)])
        .WithRecipe("Recipe_IngotLower_C", "iron ingot (lower case)", Constructor, 2, [(Ore, 1)], [(Ingot, 1)])
        .WithRecipe("Recipe_Alternate_PureIngot_C", "Pure Iron Ingot", Constructor, 2, [(Water, 1)], [(Ingot, 1)])
        .WithRecipe("Recipe_Twin1_C", "Twin", Constructor, 2, [(Ore, 1)], [("Desc_Twin_C", 1)])
        .WithRecipe("Recipe_Twin2_C", "Twin", Constructor, 2, [(Ore, 1)], [("Desc_Twin_C", 1)])
        .WithRecipe("Recipe_Gamma_C", "Gamma", Constructor, 2, [], [("Desc_Gamma_C", 1)])
        .WithRecipe("Recipe_AlphaFromGamma_C", "Alpha Part", Constructor, 2, [("Desc_Gamma_C", 1)], [("Desc_AlphaUpper_C", 1)])
        .WithRecipe("Recipe_ZuluFromRubber_C", "Zulu Part", Constructor, 2, [(Rubber, 1)], [("Desc_Zulu_C", 1)])
        .Build();

    private static readonly ExtractorCatalog Extractors = new(MinerTier.Mk1, NodePurity.Normal, new HashSet<ItemId>());

    private static DirectedAcyclicGraph<TierGraphNode> Graph(
        IEnumerable<string> newMachines, IEnumerable<(string Item, double Rate)>? sink = null) =>
        new TierGraphBuilder().Build(
            TierResultFactory.Create(Data, 2, newMachines.Select(rc => (rc, 1)), [], sink), Data, Extractors);

    private static string[] OrderOf(params string[] newMachines) =>
        new TierBuildOrder().Order(Graph(newMachines)).Select(n => n.Id).ToArray();

    [Test]
    public void Chain_IsBuiltFromTheRawEndOn()
    {
        Assert.That(
            OrderOf("Recipe_Screw_C", "Recipe_IronRod_C", "Recipe_IngotIron_C"),
            Is.EqualTo(ExpectedSequence.Of("Recipe_IngotIron_C", "Recipe_IronRod_C", "Recipe_Screw_C")));
    }

    [Test]
    public void Diamond_PutsTheJoinLastAndBreaksTheMiddleTieByProductName()
    {
        Assert.That(
            OrderOf("Recipe_ModularFrame_C", "Recipe_IronRod_C", "Recipe_IronPlate_C", "Recipe_IngotIron_C"),
            Is.EqualTo(ExpectedSequence.Of(
                "Recipe_IngotIron_C", "Recipe_IronPlate_C", "Recipe_IronRod_C", "Recipe_ModularFrame_C")));
    }

    [Test]
    public void Depth_IsTheLongestChainOfNewMachines()
    {
        // The adhered plate takes ingots directly (depth 2) and screws (depth 4 via rod and screw).
        Assert.That(
            OrderOf("Recipe_Alternate_AdheredPlate_C", "Recipe_IngotIron_C", "Recipe_Screw_C", "Recipe_IronRod_C"),
            Is.EqualTo(ExpectedSequence.Of(
                "Recipe_IngotIron_C", "Recipe_IronRod_C", "Recipe_Screw_C", "Recipe_Alternate_AdheredPlate_C")));
    }

    [Test]
    public void EqualDepth_FewerIngredientsFirst()
    {
        Assert.That(
            OrderOf("Recipe_AlphaUpper_C", "Recipe_Zeta_C"),
            Is.EqualTo(ExpectedSequence.Of("Recipe_Zeta_C", "Recipe_AlphaUpper_C")));
    }

    [Test]
    public void EqualDepthAndIngredientCount_ProductNameOrdinal_UppercaseBeforeLowercase()
    {
        Assert.That(
            OrderOf("Recipe_AlphaLower_C", "Recipe_BetaUpper_C", "Recipe_Zeta_C"),
            Is.EqualTo(ExpectedSequence.Of("Recipe_BetaUpper_C", "Recipe_Zeta_C", "Recipe_AlphaLower_C")));
    }

    [Test]
    public void SameProduct_RecipeNameOrdinal()
    {
        // Both make iron ingots with one ingredient and nothing new feeds them.
        Assert.That(
            OrderOf("Recipe_IngotLower_C", "Recipe_Alternate_PureIngot_C"),
            Is.EqualTo(ExpectedSequence.Of("Recipe_Alternate_PureIngot_C", "Recipe_IngotLower_C")));
    }

    [Test]
    public void EqualKeys_KeepTheNewMachinesOrder()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                OrderOf("Recipe_Twin2_C", "Recipe_Twin1_C"),
                Is.EqualTo(ExpectedSequence.Of("Recipe_Twin2_C", "Recipe_Twin1_C")));
            Assert.That(
                OrderOf("Recipe_Twin1_C", "Recipe_Twin2_C"),
                Is.EqualTo(ExpectedSequence.Of("Recipe_Twin1_C", "Recipe_Twin2_C")));
        });
    }

    [Test]
    public void Depth_CountsOnlyMachines_NotInputOrSinkNodes()
    {
        // Zulu is fed by a bus input only: depth 1. Counting the input node would give it depth 2 and put the
        // "Alpha Part" machine (depth 2, fed by Gamma) before it.
        DirectedAcyclicGraph<TierGraphNode> graph = Graph(
            ["Recipe_ZuluFromRubber_C", "Recipe_AlphaFromGamma_C", "Recipe_Gamma_C"],
            sink: [("Desc_Zulu_C", 1), ("Desc_Gamma_C", 2)]);

        IReadOnlyList<MachineGroupNode> order = new TierBuildOrder().Order(graph);

        Assert.That(
            order.Select(n => n.Id),
            Is.EqualTo(ExpectedSequence.Of("Recipe_Gamma_C", "Recipe_ZuluFromRubber_C", "Recipe_AlphaFromGamma_C")));
    }

    [Test]
    public void Steps_AreOneBased_OnTheGraphsOwnNodes_AndTheOrderIsTopological()
    {
        DirectedAcyclicGraph<TierGraphNode> graph = Graph(
            ["Recipe_ModularFrame_C", "Recipe_Screw_C", "Recipe_IronRod_C", "Recipe_IronPlate_C", "Recipe_IngotIron_C"]);

        IReadOnlyList<MachineGroupNode> order = new TierBuildOrder().Order(graph);

        Assert.Multiple(() =>
        {
            Assert.That(order.Select(n => n.Step), Is.EqualTo(ExpectedSequence.Of(1, 2, 3, 4, 5)));
            Assert.That(order.All(n => ReferenceEquals(n, graph.GetNode(n.Id))), Is.True);
            Assert.That(graph.Subgraph(n => n is MachineGroupNode).IsTopologicalOrder(order), Is.True);
            Assert.That(
                ((MachineGroupNode)graph.GetNode("Recipe_IngotIron_C")).Label,
                Is.EqualTo("#1 Iron Ingot\n1 x Constructor\nIron Ingot"));
        });
    }

    [Test]
    public void Order_ReturnsOnlyMachines()
    {
        DirectedAcyclicGraph<TierGraphNode> graph = Graph(["Recipe_IronPlate_C"], sink: [(Plate, 5), (Rubber, 1)]);

        IReadOnlyList<MachineGroupNode> order = new TierBuildOrder().Order(graph);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Nodes, Has.Count.EqualTo(4));
            Assert.That(order.Select(n => n.Id), Is.EqualTo(ExpectedSequence.Of("Recipe_IronPlate_C")));
        });
    }

    [Test]
    public void Order_IsRepeatable()
    {
        DirectedAcyclicGraph<TierGraphNode> graph = Graph(["Recipe_Screw_C", "Recipe_IronRod_C", "Recipe_IngotIron_C"]);
        var buildOrder = new TierBuildOrder();

        string[] first = buildOrder.Order(graph).Select(n => $"{n.Step} {n.Id}").ToArray();
        string[] second = buildOrder.Order(graph).Select(n => $"{n.Step} {n.Id}").ToArray();

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void EmptyGraph_GivesAnEmptyOrder()
    {
        Assert.That(new TierBuildOrder().Order(new DirectedAcyclicGraph<TierGraphNode>()), Is.Empty);
    }

    [Test]
    public void Order_RejectsANullGraph()
    {
        Assert.That(() => new TierBuildOrder().Order(null!), Throws.ArgumentNullException);
    }
}
