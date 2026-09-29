using Satisfactory.Graphs;
using Satisfactory.Planner.Core;
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
public sealed class TierGraphBuilderTests
{
    private const string Ore = "Desc_OreIron_C";
    private const string CopperOre = "Desc_OreCopper_C";
    private const string Water = "Desc_Water_C";
    private const string Ingot = "Desc_IronIngot_C";
    private const string Plate = "Desc_IronPlate_C";
    private const string Rod = "Desc_IronRod_C";
    private const string Screw = "Desc_IronScrew_C";
    private const string Reinforced = "Desc_IronPlateReinforced_C";
    private const string Rubber = "Desc_Rubber_C";
    private const string Plastic = "Desc_Plastic_C";
    private const string FuelRod = "Desc_NuclearFuelRod_C";
    private const string Waste = "Desc_NuclearWaste_C";

    private static readonly GameData Data = new GameDataBuilder()
        .WithItem(Ore, "Iron Ore").WithResource(Ore)
        .WithItem(CopperOre, "Copper Ore").WithResource(CopperOre)
        .WithFluid(Water, "Water").WithResource(Water)
        .WithItem(Ingot, "Iron Ingot")
        .WithItem(Plate, "Iron Plate")
        .WithItem(Rod, "Iron Rod")
        .WithItem(Screw, "Screw")
        .WithItem(Reinforced, "Reinforced Iron Plate")
        .WithItem("Desc_CopperIngot_C", "Copper Ingot")
        .WithItem(Rubber, "Rubber")
        .WithItem(Plastic, "Plastic")
        .WithItem(FuelRod, "Uranium Fuel Rod")
        .WithItem(Waste, "Uranium Waste")
        .WithBuilding("Desc_SmelterMk1_C", "Smelter", 4)
        .WithBuilding("Desc_ConstructorMk1_C", "Constructor", 4)
        .WithBuilding("Desc_AssemblerMk1_C", "Assembler", 15)
        .WithBuilding("Desc_OilRefinery_C", "Refinery", 30)
        .WithBuilding("Desc_GeneratorNuclear_C", "Generator Descriptor", 0)
        .WithRecipe("Recipe_IngotIron_C", "Iron Ingot", "Desc_SmelterMk1_C", 2, [(Ore, 1)], [(Ingot, 1)])
        .WithRecipe(
            "Recipe_Alternate_PureIron_C", "Alternate: Pure Iron Ingot", "Desc_OilRefinery_C", 12,
            [(Ore, 7), (Water, 4)], [(Ingot, 13)], alternate: true)
        .WithRecipe("Recipe_IronPlate_C", "Iron Plate", "Desc_ConstructorMk1_C", 6, [(Ingot, 3)], [(Plate, 2)])
        .WithRecipe("Recipe_IronRod_C", "Iron Rod", "Desc_ConstructorMk1_C", 4, [(Ingot, 1)], [(Rod, 1)])
        .WithRecipe("Recipe_Screw_C", "Screw", "Desc_ConstructorMk1_C", 6, [(Rod, 1)], [(Screw, 4)])
        .WithRecipe(
            "Recipe_ReinforcedPlate_C", "Reinforced Iron Plate", "Desc_AssemblerMk1_C", 12,
            [(Plate, 6), (Screw, 12)], [(Reinforced, 1)])
        .WithRecipe(
            "Recipe_Alternate_DoubledPlate_C", "Alternate: Doubled Plate", "Desc_AssemblerMk1_C", 12,
            [(Plate, 3), (Plate, 3), (Rubber, 1)], [(Reinforced, 1)], alternate: true)
        .WithRecipe("Recipe_IngotCopper_C", "Copper Ingot", "Desc_SmelterMk1_C", 2, [(CopperOre, 1)], [("Desc_CopperIngot_C", 1)])
        .WithRecipe("Recipe_Plastic_C", "Plastic", "Desc_OilRefinery_C", 6, [(Rubber, 2)], [(Plastic, 2)])
        .WithRecipe("Recipe_SelfLoop_C", "Ingot Loop", "Desc_SmelterMk1_C", 2, [(Ingot, 1)], [(Ingot, 2)])
        .WithRecipe("Recipe_CycleA_C", "Cycle A", "Desc_ConstructorMk1_C", 2, [(Rod, 1)], [(Plate, 1)])
        .WithRecipe("Recipe_CycleB_C", "Cycle B", "Desc_ConstructorMk1_C", 2, [(Plate, 1)], [(Rod, 1)])
        .WithGeneratorRecipe(
            "Gen_Test_Uranium", "Burn Uranium Fuel Rod", "Desc_GeneratorNuclear_C", "Nuclear Power Plant", 300,
            [(FuelRod, 1), (Water, 1200)], [(Waste, 50)], 2500)
        .WithGeneratorRecipe(
            "Gen_Test_Unnamed", "Burn Without Name", "Desc_GeneratorNuclear_C", string.Empty, 300,
            [(FuelRod, 1)], [(Waste, 50)], 2500)
        .Build();

    /// <summary>Iron ore and water can be extracted; copper ore is still locked.</summary>
    private static readonly ExtractorCatalog Extractors =
        new(MinerTier.Mk1, NodePurity.Normal, new HashSet<ItemId> { new(CopperOre) });

    private static DirectedAcyclicGraph<TierGraphNode> Build(
        IEnumerable<(string Recipe, int Machines)> newMachines,
        IEnumerable<string>? newItems = null,
        IEnumerable<(string Item, double Rate)>? sink = null) =>
        new TierGraphBuilder().Build(TierResultFactory.Create(Data, 3, newMachines, newItems ?? [], sink), Data, Extractors);

    private static string[] EdgeIds(DirectedAcyclicGraph<TierGraphNode> graph) =>
        graph.Edges.Select(e => $"{e.From.Id} -> {e.To.Id}").ToArray();

    [Test]
    public void MachineNodes_ComeFirst_InNewMachinesOrder_WithTheirNamesCountsAndHighlight()
    {
        DirectedAcyclicGraph<TierGraphNode> graph =
            Build([("Recipe_IronPlate_C", 2), ("Recipe_IngotIron_C", 3), ("Gen_Test_Uranium", 1)], newItems: [Plate]);

        MachineGroupNode[] machines = graph.Nodes.Take(3).Cast<MachineGroupNode>().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(graph.Nodes.Select(n => n.Id), Is.EqualTo(ExpectedSequence.Of(
                "Recipe_IronPlate_C", "Recipe_IngotIron_C", "Gen_Test_Uranium", "in_Desc_OreIron_C",
                "in_Desc_NuclearFuelRod_C", "in_Desc_Water_C")));
            Assert.That(machines.Select(m => m.ProductName), Is.EqualTo(ExpectedSequence.Of("Iron Plate", "Iron Ingot", "Uranium Waste")));
            Assert.That(machines.Select(m => m.BuildingName), Is.EqualTo(ExpectedSequence.Of("Constructor", "Smelter", "Nuclear Power Plant")));
            Assert.That(machines.Select(m => m.NewMachines), Is.EqualTo(ExpectedSequence.Of(2, 3, 1)));
            Assert.That(machines.Select(m => m.MakesNewItem), Is.EqualTo(ExpectedSequence.Of(true, false, false)));
            Assert.That(machines.Select(m => m.Recipe.Id.Value), Is.EqualTo(machines.Select(m => m.Id)));
            Assert.That(machines.Select(m => m.Step), Is.All.Zero);
            Assert.That(machines[0].Label, Is.EqualTo("#0 Iron Plate\n2 x Constructor\nIron Plate"));
        });
    }

    [Test]
    public void BuildingName_ComesFromTheBuilding_OrFromANonEmptyGeneratorBuildingName()
    {
        DirectedAcyclicGraph<TierGraphNode> graph =
            Build([("Gen_Test_Uranium", 1), ("Gen_Test_Unnamed", 1), ("Recipe_Alternate_PureIron_C", 1)]);

        Assert.That(
            graph.Nodes.OfType<MachineGroupNode>().Select(m => m.BuildingName),
            Is.EqualTo(ExpectedSequence.Of("Nuclear Power Plant", "Generator Descriptor", "Refinery")));
    }

    [Test]
    public void Maker_IsTheFirstNewMachinePerMainProduct()
    {
        DirectedAcyclicGraph<TierGraphNode> graph =
            Build([("Recipe_Alternate_PureIron_C", 1), ("Recipe_IngotIron_C", 1), ("Recipe_IronPlate_C", 1)]);

        Assert.That(EdgeIds(graph), Is.EqualTo(ExpectedSequence.Of(
            "in_Desc_OreIron_C -> Recipe_Alternate_PureIron_C",
            "in_Desc_Water_C -> Recipe_Alternate_PureIron_C",
            "in_Desc_OreIron_C -> Recipe_IngotIron_C",
            "Recipe_Alternate_PureIron_C -> Recipe_IronPlate_C")));
    }

    [Test]
    public void ExternalInputs_AreRawWhenExtractable_AndBusOtherwise_CreatedOnce()
    {
        DirectedAcyclicGraph<TierGraphNode> graph = Build(
            [("Recipe_IngotCopper_C", 1), ("Recipe_IngotIron_C", 1), ("Recipe_Plastic_C", 1), ("Recipe_IronPlate_C", 1),
             ("Recipe_Alternate_PureIron_C", 1)]);

        ExternalInputNode[] inputs = graph.Nodes.OfType<ExternalInputNode>().ToArray();
        Assert.Multiple(() =>
        {
            // The iron plate machine is fed by the first ingot maker, so iron ingot is not an external input.
            Assert.That(inputs.Select(n => n.Id), Is.EqualTo(ExpectedSequence.Of(
                "in_Desc_OreCopper_C", "in_Desc_OreIron_C", "in_Desc_Rubber_C", "in_Desc_Water_C")));
            Assert.That(inputs.Select(n => n.Kind), Is.EqualTo(ExpectedSequence.Of(
                TierGraphNodeKind.BusInput, TierGraphNodeKind.RawInput, TierGraphNodeKind.BusInput, TierGraphNodeKind.RawInput)));
            Assert.That(inputs.Select(n => n.Label), Is.EqualTo(ExpectedSequence.Of(
                "Copper Ore\n(bus)", "Iron Ore\n(raw)", "Rubber\n(bus)", "Water\n(raw)")));
            Assert.That(inputs[0], Is.InstanceOf<BusInputNode>());
            Assert.That(inputs[1], Is.InstanceOf<RawResourceInputNode>());
            Assert.That(graph.OutgoingEdges(inputs[1]).Select(e => e.To.Id), Is.EqualTo(ExpectedSequence.Of(
                "Recipe_IngotIron_C", "Recipe_Alternate_PureIron_C")));
        });
    }

    [Test]
    public void AnIngredientNoNewMachineMakes_IsABusInput()
    {
        DirectedAcyclicGraph<TierGraphNode> graph = Build([("Recipe_IronPlate_C", 1)]);

        Assert.Multiple(() =>
        {
            Assert.That(graph.GetNode("in_Desc_IronIngot_C"), Is.InstanceOf<BusInputNode>());
            Assert.That(graph.GetNode("in_Desc_IronIngot_C").Label, Is.EqualTo("Iron Ingot\n(bus)"));
            Assert.That(EdgeIds(graph), Is.EqualTo(ExpectedSequence.Of("in_Desc_IronIngot_C -> Recipe_IronPlate_C")));
        });
    }

    [Test]
    public void DuplicateIngredientEntries_GiveParallelEdges()
    {
        DirectedAcyclicGraph<TierGraphNode> external = Build([("Recipe_Alternate_DoubledPlate_C", 1)]);
        DirectedAcyclicGraph<TierGraphNode> made = Build([("Recipe_Alternate_DoubledPlate_C", 1), ("Recipe_IronPlate_C", 1)]);

        Assert.Multiple(() =>
        {
            Assert.That(EdgeIds(external), Is.EqualTo(ExpectedSequence.Of(
                "in_Desc_IronPlate_C -> Recipe_Alternate_DoubledPlate_C",
                "in_Desc_IronPlate_C -> Recipe_Alternate_DoubledPlate_C",
                "in_Desc_Rubber_C -> Recipe_Alternate_DoubledPlate_C")));
            Assert.That(external.Nodes.Count(n => n.Id == "in_Desc_IronPlate_C"), Is.EqualTo(1));
            Assert.That(EdgeIds(made), Is.EqualTo(ExpectedSequence.Of(
                "Recipe_IronPlate_C -> Recipe_Alternate_DoubledPlate_C",
                "Recipe_IronPlate_C -> Recipe_Alternate_DoubledPlate_C",
                "in_Desc_Rubber_C -> Recipe_Alternate_DoubledPlate_C",
                "in_Desc_IronIngot_C -> Recipe_IronPlate_C")));
        });
    }

    [Test]
    public void Edges_FollowNewMachinesOrderThenIngredientOrder_WithoutLabels_OnTheStoredNodes()
    {
        DirectedAcyclicGraph<TierGraphNode> graph = Build(
            [("Recipe_ReinforcedPlate_C", 1), ("Recipe_IronPlate_C", 2), ("Recipe_IronRod_C", 1), ("Recipe_Screw_C", 1),
             ("Recipe_IngotIron_C", 4)]);

        Assert.Multiple(() =>
        {
            Assert.That(EdgeIds(graph), Is.EqualTo(ExpectedSequence.Of(
                "Recipe_IronPlate_C -> Recipe_ReinforcedPlate_C",
                "Recipe_Screw_C -> Recipe_ReinforcedPlate_C",
                "Recipe_IngotIron_C -> Recipe_IronPlate_C",
                "Recipe_IngotIron_C -> Recipe_IronRod_C",
                "Recipe_IronRod_C -> Recipe_Screw_C",
                "in_Desc_OreIron_C -> Recipe_IngotIron_C")));
            Assert.That(graph.Edges.Select(e => e.Label), Is.All.Null);
            Assert.That(graph.Edges.All(e => ReferenceEquals(e.From, graph.GetNode(e.From.Id))), Is.True);
            Assert.That(graph.Edges.All(e => ReferenceEquals(e.To, graph.GetNode(e.To.Id))), Is.True);
        });
    }

    [Test]
    public void Sink_GetsOneLabelledEdgePerItem_FromTheMakerTheExistingInputOrANewBusNode()
    {
        DirectedAcyclicGraph<TierGraphNode> graph = Build(
            [("Recipe_IronPlate_C", 1), ("Recipe_IngotIron_C", 1)],
            sink: [(Plate, 12345.5), (Ore, 2.5), (Rubber, 3.5), (Water, 1234567.891)]);

        TierGraphNode sink = graph.GetNode("SINK");
        GraphEdge<TierGraphNode>[] sinkEdges = graph.IncomingEdges(sink).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(sink, Is.InstanceOf<SinkNode>());
            Assert.That(graph.Nodes.Select(n => n.Id), Is.EqualTo(ExpectedSequence.Of(
                "Recipe_IronPlate_C", "Recipe_IngotIron_C", "in_Desc_OreIron_C", "SINK", "in_Desc_Rubber_C", "in_Desc_Water_C")));
            Assert.That(sinkEdges.Select(e => e.From.Id), Is.EqualTo(ExpectedSequence.Of(
                "Recipe_IronPlate_C", "in_Desc_OreIron_C", "in_Desc_Rubber_C", "in_Desc_Water_C")));
            Assert.That(sinkEdges.Select(e => e.Label), Is.EqualTo(ExpectedSequence.Of(
                "Iron Plate 12,346/min", "Iron Ore 2/min", "Rubber 4/min", "Water 1,234,568/min")));

            // The ore node used as an ingredient is reused (still raw); a sunk item nobody uses is always a bus node,
            // even when it could be extracted (as in the Python).
            Assert.That(sinkEdges[1].From, Is.SameAs(graph.GetNode("in_Desc_OreIron_C")));
            Assert.That(sinkEdges[1].From, Is.InstanceOf<RawResourceInputNode>());
            Assert.That(sinkEdges[2].From, Is.InstanceOf<BusInputNode>());
            Assert.That(sinkEdges[3].From, Is.InstanceOf<BusInputNode>());
            Assert.That(graph.Edges.Take(2).Select(e => e.To.Id), Is.All.Not.EqualTo("SINK"));
            Assert.That(graph.Edges.Skip(2).Select(e => e.To.Id), Is.All.EqualTo("SINK"));
        });
    }

    [TestCase(0.5, "0")]
    [TestCase(1.5, "2")]
    [TestCase(2.5, "2")]
    [TestCase(999.5, "1,000")]
    [TestCase(0.0001, "0")]
    [TestCase(1000.0, "1,000")]
    public void SinkLabel_RoundsHalfToEvenWithThousandsSeparators(double rate, string expected)
    {
        DirectedAcyclicGraph<TierGraphNode> graph = Build([], sink: [(Screw, rate)]);

        Assert.That(graph.Edges.Single().Label, Is.EqualTo($"Screw {expected}/min"));
    }

    [Test]
    public void SinkLabel_FallsBackToTheItemIdForAnUnknownItem()
    {
        DirectedAcyclicGraph<TierGraphNode> graph = Build([], sink: [("Desc_Mystery_C", 7)]);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Edges.Single().Label, Is.EqualTo("Desc_Mystery_C 7/min"));
            Assert.That(graph.GetNode("in_Desc_Mystery_C").Label, Is.EqualTo("Desc_Mystery_C\n(bus)"));
        });
    }

    [Test]
    public void NoSink_MeansNoSinkNode()
    {
        DirectedAcyclicGraph<TierGraphNode> graph = Build([("Recipe_IngotIron_C", 1)]);

        Assert.That(graph.Contains("SINK"), Is.False);
    }

    [Test]
    public void EmptyTier_GivesAnEmptyGraph()
    {
        DirectedAcyclicGraph<TierGraphNode> graph = Build([]);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Nodes, Is.Empty);
            Assert.That(graph.Edges, Is.Empty);
        });
    }

    [Test]
    public void ARecipeConsumingItsOwnMainProduct_IsReportedAsAPlannerException()
    {
        var e = Assert.Throws<PlannerException>(() => Build([("Recipe_SelfLoop_C", 1)]));

        Assert.Multiple(() =>
        {
            Assert.That(e!.Message, Does.Contain("Recipe_SelfLoop_C"));
            Assert.That(e.Message, Does.Contain("Tier 3"));
            Assert.That(e.InnerException, Is.InstanceOf<CycleDetectedException>());
        });
    }

    [Test]
    public void RecipesFeedingEachOther_AreReportedAsAPlannerExceptionNamingBoth()
    {
        var e = Assert.Throws<PlannerException>(() => Build([("Recipe_CycleA_C", 1), ("Recipe_CycleB_C", 1)]));

        Assert.Multiple(() =>
        {
            Assert.That(e!.Message, Does.Contain("Recipe_CycleA_C"));
            Assert.That(e.Message, Does.Contain("Recipe_CycleB_C"));
            Assert.That(e.InnerException, Is.InstanceOf<CycleDetectedException>());
        });
    }

    [Test]
    public void ANewMachineWithoutARecipe_IsRejected()
    {
        TierResult result = TierResultFactory.Create(Data, 1, [("Recipe_IngotIron_C", 1)], [], recipes: []);

        Assert.That(
            () => new TierGraphBuilder().Build(result, Data, Extractors),
            Throws.ArgumentException.With.Message.Contains("Recipe_IngotIron_C"));
    }

    [Test]
    public void Build_RejectsNullArguments()
    {
        TierResult result = TierResultFactory.Create(Data, 1, [], []);
        var builder = new TierGraphBuilder();

        Assert.Multiple(() =>
        {
            Assert.That(() => builder.Build(null!, Data, Extractors), Throws.ArgumentNullException);
            Assert.That(() => builder.Build(result, null!, Extractors), Throws.ArgumentNullException);
            Assert.That(() => builder.Build(result, Data, null!), Throws.ArgumentNullException);
        });
    }
}
