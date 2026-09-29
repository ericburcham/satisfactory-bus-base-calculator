using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Core.Tiers.Graph;
using Satisfactory.Planner.Reporting.Writers;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Reporting.Writers;

/// <summary>
/// Hand-built tiers for the writer tests. <see cref="Rich"/> mirrors a synthetic tier that was fed to the Python
/// <c>write_tier</c> (tier 5: six machine groups incl. an alternate, a variable-power recipe without inputs and two
/// byproducts, a nuclear generator, a disposal recipe, names longer than their fields, reused capacity, sink and raw
/// input); the expected texts in the tests are the Python's output for it.
/// </summary>
public static class WriterScenario
{
    public const string OutputFolder = "out";

    public static readonly ItemId OreIron = new("Desc_OreIron_C");
    public static readonly ItemId Water = new("Desc_Water_C");
    public static readonly ItemId Coal = new("Desc_Coal_C");
    public static readonly ItemId IronIngot = new("Desc_IronIngot_C");
    public static readonly ItemId IronPlate = new("Desc_IronPlate_C");
    public static readonly ItemId Screw = new("Desc_Screw_C");
    public static readonly ItemId ReinforcedPlate = new("Desc_ReinforcedPlate_C");
    public static readonly ItemId NuclearFuelRod = new("Desc_NuclearFuelRod_C");
    public static readonly ItemId NuclearWaste = new("Desc_NuclearWaste_C");
    public static readonly ItemId FluidCanister = new("Desc_FluidCanister_C");
    public static readonly ItemId HeavyOil = new("Desc_HeavyOilResidue_C");
    public static readonly ItemId Plastic = new("Desc_Plastic_C");

    public static GameData Data { get; } = new GameDataBuilder()
        .WithItem("Desc_OreIron_C", "Iron Ore").WithResource("Desc_OreIron_C")
        .WithFluid("Desc_Water_C", "Water").WithResource("Desc_Water_C")
        .WithItem("Desc_Coal_C", "Coal").WithResource("Desc_Coal_C")
        .WithItem("Desc_IronIngot_C", "Iron Ingot")
        .WithItem("Desc_IronPlate_C", "Iron Plate")
        .WithItem("Desc_Screw_C", "Screws")
        .WithItem("Desc_ReinforcedPlate_C", "Reinforced Iron Plate With An Extremely Long Name")
        .WithItem("Desc_DarkMatter_C", "Dark Matter")
        .WithItem("Desc_NuclearFuelRod_C", "Uranium Fuel Rod")
        .WithItem("Desc_NuclearWaste_C", "Uranium Waste")
        .WithItem("Desc_FluidCanister_C", "Empty Canister")
        .WithItem("Desc_PackagedWater_C", "Packaged Water")
        .WithFluid("Desc_HeavyOilResidue_C", "Heavy Oil Residue")
        .WithItem("Desc_Plastic_C", "Plastic")
        .WithBuilding("Desc_SmelterMk1_C", "Smelter", 4)
        .WithBuilding("Desc_ConstructorMk1_C", "Constructor", 4)
        .WithBuilding("Desc_AssemblerMk1_C", "Assembler Of Very Long Name Mk.1", 15)
        .WithBuilding("Desc_HadronCollider_C", "Particle Accelerator", 0)
        .WithBuilding("Desc_Packager_C", "Packager", 10)
        .WithBuilding("Desc_GeneratorNuclear_C", "Nuclear Power Plant", 0)
        .WithRecipe("Recipe_IngotIron_C", "Iron Ingot", "Desc_SmelterMk1_C", 2, [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
        .WithRecipe("Recipe_IronPlate_C", "Iron Plate", "Desc_ConstructorMk1_C", 2, [("Desc_IronIngot_C", 3)], [("Desc_IronPlate_C", 2)])
        .WithRecipe(
            "Recipe_Alternate_BoltedPlate_C", "Alternate: Bolted Iron Plate", "Desc_AssemblerMk1_C", 2,
            [("Desc_IronPlate_C", 3), ("Desc_Screw_C", 12)], [("Desc_ReinforcedPlate_C", 1)], alternate: true)
        .WithVariablePowerRecipe(
            "Recipe_DarkMatter_C", "Dark Matter From Nothing", "Desc_HadronCollider_C", 2,
            [], [("Desc_DarkMatter_C", 1), ("Desc_Water_C", 1), ("Desc_Coal_C", 2)], 250, 750)
        .WithRecipe(
            "Recipe_PackagedWater_C", "Packaged Water", "Desc_Packager_C", 2,
            [("Desc_Water_C", 2), ("Desc_FluidCanister_C", 2)], [("Desc_PackagedWater_C", 2)])
        .WithGeneratorRecipe(
            "Gen_Nuclear_Uranium", "Burn Uranium Fuel Rod", "Desc_GeneratorNuclear_C", "Nuclear Power Plant", 2,
            [("Desc_NuclearFuelRod_C", 1), ("Desc_Water_C", 1200)], [("Desc_NuclearWaste_C", 50)], 2500)
        .Build();

    public static Recipe RecipeOf(string id) => Data.Recipes.Single(r => r.Id.Value == id);

    /// <summary>The rich tier 5 (see the class summary).</summary>
    public static TierResult Rich()
    {
        Recipe ingot = RecipeOf("Recipe_IngotIron_C");
        Recipe plate = RecipeOf("Recipe_IronPlate_C");
        Recipe bolted = RecipeOf("Recipe_Alternate_BoltedPlate_C");
        Recipe darkMatter = RecipeOf("Recipe_DarkMatter_C");
        Recipe generator = RecipeOf("Gen_Nuclear_Uranium");
        Recipe packager = RecipeOf("Recipe_PackagedWater_C");
        Recipe[] recipes = [ingot, plate, bolted, darkMatter, generator, packager];
        return new TierResult(
            5,
            [IronPlate, ReinforcedPlate],
            Ordered<RecipeId, int>((ingot.Id, 3), (plate.Id, 2), (bolted.Id, 1), (darkMatter.Id, 2), (generator.Id, 2), (packager.Id, 1)),
            Ordered(recipes.Select(r => (r.Id, r)).ToArray()),
            Ordered<RecipeId, double>(),
            Ordered<RecipeId, int>(),
            Ordered<ItemId, double>((IronPlate, 20.0), (ReinforcedPlate, 1234.5)),
            Ordered<ItemId, Recipe>((IronPlate, plate), (ReinforcedPlate, bolted)),
            Ordered<ItemId, double>((IronIngot, 0.25), (IronPlate, 0.9), (Screw, 0.25)),
            Ordered<ItemId, double>((Water, 15328.07), (OreIron, 1273.905), (Coal, 0.004)),
            Ordered<ItemId, double>((IronPlate, 7.5), (NuclearWaste, 100.0), (FluidCanister, 7.5), (Plastic, 1500.25)),
            Ordered<ItemId, (double Rate, Recipe Recipe)>((Water, (12.5, packager)), (HeavyOil, (1234.56, packager))));
    }

    /// <summary>A minimal tier 0: one smelter, nothing reused, sunk or generated, no raw input listed.</summary>
    public static TierResult Minimal()
    {
        Recipe ingot = RecipeOf("Recipe_IngotIron_C");
        return new TierResult(
            0,
            [IronIngot],
            Ordered<RecipeId, int>((ingot.Id, 1)),
            Ordered<RecipeId, Recipe>((ingot.Id, ingot)),
            Ordered<RecipeId, double>(),
            Ordered<RecipeId, int>(),
            Ordered<ItemId, double>((IronIngot, 30.0)),
            Ordered<ItemId, Recipe>((IronIngot, ingot)),
            Ordered<ItemId, double>(),
            Ordered<ItemId, double>(),
            Ordered<ItemId, double>(),
            Ordered<ItemId, (double Rate, Recipe Recipe)>());
    }

    /// <summary>
    /// The graph the tier graph builder produces for <paramref name="result"/> (machine groups in NewMachines order,
    /// then per machine one edge per ingredient, then the sink), with <see cref="MachineGroupNode.Step"/> set from the
    /// Python build order of the scenario.
    /// </summary>
    public static DirectedAcyclicGraph<TierGraphNode> GraphOf(TierResult result, params string[] buildOrder)
    {
        var graph = new DirectedAcyclicGraph<TierGraphNode>();
        var makers = new Dictionary<ItemId, MachineGroupNode>();
        foreach ((RecipeId id, int machines) in result.NewMachines)
        {
            Recipe recipe = result.Recipes[id];
            ItemId product = recipe.MainProduct.Item;
            string building = recipe is GeneratorRecipe g ? g.BuildingName : Data.GetBuilding(recipe.BuildingId).Name;
            var node = new MachineGroupNode(recipe, machines, Data.ItemName(product), building, result.NewItems.Contains(product));
            graph.AddNode(node);
            makers.TryAdd(product, node);
        }

        foreach (MachineGroupNode machine in graph.Nodes.OfType<MachineGroupNode>().ToList())
        {
            foreach (ItemAmount ingredient in machine.Recipe.Ingredients)
            {
                TierGraphNode source = makers.TryGetValue(ingredient.Item, out MachineGroupNode? maker)
                    ? maker
                    : InputNode(graph, ingredient.Item, Data.IsResource(ingredient.Item));
                graph.AddEdge(source, machine);
            }
        }

        if (result.Sink.Count > 0)
        {
            var sink = new SinkNode();
            graph.AddNode(sink);
            foreach ((ItemId item, double rate) in result.Sink)
            {
                TierGraphNode source = makers.TryGetValue(item, out MachineGroupNode? maker)
                    ? maker
                    : InputNode(graph, item, raw: false);
                graph.AddEdge(source, sink, $"{Data.ItemName(item)} {Satisfactory.Planner.Core.Numerics.PythonNumerics.FormatFixed(rate, 0, thousands: true)}/min");
            }
        }

        for (int index = 0; index < buildOrder.Length; index++)
        {
            ((MachineGroupNode)graph.GetNode(buildOrder[index])).Step = index + 1;
        }

        return graph;
    }

    /// <summary>The build order of the graph: machine groups sorted by their assigned step.</summary>
    public static IReadOnlyList<MachineGroupNode> BuildOrderOf(DirectedAcyclicGraph<TierGraphNode> graph) =>
        graph.Nodes.OfType<MachineGroupNode>().OrderBy(m => m.Step).ToList();

    public static TierReportContext ContextOf(TierResult result, TierTotals totals, params string[] buildOrder)
    {
        DirectedAcyclicGraph<TierGraphNode> graph = GraphOf(result, buildOrder);
        return new TierReportContext(OutputFolder, Data, graph, BuildOrderOf(graph), totals);
    }

    /// <summary>The Python build order of <see cref="Rich"/>.</summary>
    public static string[] RichBuildOrder() =>
    [
        "Recipe_DarkMatter_C", "Recipe_IngotIron_C", "Recipe_PackagedWater_C", "Gen_Nuclear_Uranium",
        "Recipe_IronPlate_C", "Recipe_Alternate_BoltedPlate_C",
    ];

    public static TierReportContext RichContext() => ContextOf(Rich(), new TierTotals(11, 1045.0, 5000.0), RichBuildOrder());

    public static TierReportContext MinimalContext() =>
        ContextOf(Minimal(), new TierTotals(1, 4.0, 0.0), "Recipe_IngotIron_C");

    /// <summary>Joins lines with <paramref name="newLine"/> after every line.</summary>
    public static string Text(string newLine, params string[] lines) => string.Concat(lines.Select(l => l + newLine));

    public static OrderedDictionary<TKey, TValue> Ordered<TKey, TValue>(params (TKey Key, TValue Value)[] entries)
        where TKey : notnull
    {
        var dictionary = new OrderedDictionary<TKey, TValue>();
        foreach ((TKey key, TValue value) in entries)
        {
            dictionary.Add(key, value);
        }

        return dictionary;
    }

    private static ExternalInputNode InputNode(DirectedAcyclicGraph<TierGraphNode> graph, ItemId item, bool raw)
    {
        if (graph.TryGetNode(ExternalInputNode.IdFor(item), out TierGraphNode? existing))
        {
            return (ExternalInputNode)existing;
        }

        ExternalInputNode node = raw
            ? new RawResourceInputNode(item, Data.ItemName(item))
            : new BusInputNode(item, Data.ItemName(item));
        graph.AddNode(node);
        return node;
    }
}
