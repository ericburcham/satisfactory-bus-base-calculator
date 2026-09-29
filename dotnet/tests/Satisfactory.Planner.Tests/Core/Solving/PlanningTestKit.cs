using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Excess;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Power;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Selection;
using Satisfactory.Planner.Core.Solving;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Tests.TestSupport;
using GameData = Satisfactory.Planner.Core.Model.GameData;

namespace Satisfactory.Planner.Tests.Core.Solving;

/// <summary>
/// Shared helpers for the planning engine tests: synthetic game data with the real vanilla recipe numbers of the data
/// file, and the real planning components wired together.
/// </summary>
internal static class PlanningTestKit
{
    public const string IronOre = "Desc_OreIron_C";
    public const string Coal = "Desc_Coal_C";
    public const string Water = "Desc_Water_C";
    public const string CrudeOil = "Desc_LiquidOil_C";
    public const string IronIngot = "Desc_IronIngot_C";
    public const string SteelIngot = "Desc_SteelIngot_C";
    public const string IronPlate = "Desc_IronPlate_C";
    public const string IronRod = "Desc_IronRod_C";
    public const string Screw = "Desc_IronScrew_C";
    public const string ReinforcedPlate = "Desc_IronPlateReinforced_C";
    public const string ModularFrame = "Desc_ModularFrame_C";
    public const string Rebar = "Desc_SpikedRebar_C";
    public const string Plastic = "Desc_Plastic_C";
    public const string HeavyOilResidue = "Desc_HeavyOilResidue_C";
    public const string PetroleumCoke = "Desc_PetroleumCoke_C";
    public const string Smelter = "Desc_SmelterMk1_C";
    public const string Constructor = "Desc_ConstructorMk1_C";
    public const string Assembler = "Desc_AssemblerMk1_C";
    public const string Foundry = "Desc_FoundryMk1_C";
    public const string Refinery = "Desc_OilRefinery_C";

    public static ItemId Item(string id) => new(id);

    public static RecipeId Recipe(string id) => new(id);

    /// <summary>Iron ore, iron ingots and rods (vanilla recipes).</summary>
    public static GameDataBuilder IronRods() => new GameDataBuilder()
        .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
        .WithItem(IronIngot, "Iron Ingot")
        .WithItem(IronRod, "Iron Rod")
        .WithBuilding(Smelter, "Smelter", 4)
        .WithBuilding(Constructor, "Constructor", 4)
        .WithRecipe("Recipe_IngotIron_C", "Iron Ingot", Smelter, 2, [(IronOre, 1)], [(IronIngot, 1)])
        .WithRecipe("Recipe_IronRod_C", "Iron Rod", Constructor, 4, [(IronIngot, 1)], [(IronRod, 1)]);

    /// <summary>The vanilla modular frame chain: ingots, plates, rods, screws, reinforced plates, modular frames.</summary>
    public static GameDataBuilder ModularFrames() => IronRods()
        .WithItem(IronPlate, "Iron Plate")
        .WithItem(Screw, "Screws")
        .WithItem(ReinforcedPlate, "Reinforced Iron Plate")
        .WithItem(ModularFrame, "Modular Frame")
        .WithBuilding(Assembler, "Assembler", 15)
        .WithRecipe("Recipe_IronPlate_C", "Iron Plate", Constructor, 6, [(IronIngot, 3)], [(IronPlate, 2)])
        .WithRecipe("Recipe_Screw_C", "Screws", Constructor, 6, [(IronRod, 1)], [(Screw, 4)])
        .WithRecipe(
            "Recipe_IronPlateReinforced_C", "Reinforced Iron Plate", Assembler, 12,
            [(IronPlate, 6), (Screw, 12)], [(ReinforcedPlate, 1)])
        .WithRecipe(
            "Recipe_ModularFrame_C", "Modular Frame", Assembler, 60,
            [(ReinforcedPlate, 3), (IronRod, 12)], [(ModularFrame, 2)]);

    /// <summary>Vanilla steel ingots: Foundry, 3 iron ore + 3 coal -> 3 steel ingots in 4 s.</summary>
    public static GameDataBuilder Steel() => new GameDataBuilder()
        .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
        .WithItem(Coal, "Coal").WithResource(Coal)
        .WithItem(SteelIngot, "Steel Ingot")
        .WithBuilding(Foundry, "Foundry", 16)
        .WithRecipe("Recipe_IngotSteel_C", "Steel Ingot", Foundry, 4, [(IronOre, 3), (Coal, 3)], [(SteelIngot, 3)]);

    /// <summary>Vanilla plastic (with heavy oil residue as byproduct) from crude oil.</summary>
    public static GameDataBuilder PlasticWithResidue() => new GameDataBuilder()
        .WithFluid(CrudeOil, "Crude Oil").WithResource(CrudeOil)
        .WithItem(Plastic, "Plastic")
        .WithFluid(HeavyOilResidue, "Heavy Oil Residue")
        .WithBuilding(Refinery, "Refinery", 30)
        .WithRecipe("Recipe_Plastic_C", "Plastic", Refinery, 6, [(CrudeOil, 3)], [(Plastic, 2), (HeavyOilResidue, 1)]);

    public static ITierContext Context(GameData data, int tier = 0) =>
        new TierContextFactory(data, new UnlockTierResolver(data), MinerTier.Mk1, NodePurity.Normal).Create(tier);

    public static ResourceCostCalculator CostCalculator() => new(new DefaultResourceWeights());

    public static ResourceCostTable Costs(ITierContext context, CreditPolicy? credit = null) =>
        CostCalculator().Calculate(context, credit ?? new CreditPolicy());

    public static RecipeChoice Choice(ITierContext context, CreditPolicy? credit = null)
    {
        credit ??= new CreditPolicy();
        return new CheapestRecipeSelector().Choose(context, Costs(context, credit), credit);
    }

    /// <summary>A solve context with the tier's cheapest recipes, as one optimisation round builds it.</summary>
    public static SolveContext SolveContext(
        ITierContext context, IExistingCapacity? existing = null, CreditPolicy? credit = null, RecipeChoice? choice = null)
    {
        credit ??= new CreditPolicy();
        ResourceCostTable costs = Costs(context, credit);
        choice ??= new CheapestRecipeSelector().Choose(context, costs, credit);
        return new SolveContext(
            context, choice, credit, existing ?? NoExistingCapacity.Instance, context.Extractors,
            new FluidDisposalFinder(context, costs));
    }

    public static ProductionSolver Solver(GameData data) => new(new PowerCalculator(data));

    public static ByproductCreditOptimizer Optimizer(GameData data, IProductionSolver? solver = null) =>
        new(CostCalculator(), new CheapestRecipeSelector(), new OneMachineExcessPolicy(), solver ?? Solver(data),
            new FluidDisposalFinderFactory());

    public static TierPlanner TierPlanner(GameData data, IProductionSolver? solver = null)
    {
        var contexts = new TierContextFactory(data, new UnlockTierResolver(data), MinerTier.Mk1, NodePurity.Normal);
        return new TierPlanner(contexts, CostCalculator(), new CheapestRecipeSelector(), Optimizer(data, solver));
    }

    public static PlannerOptions Options(bool includeIngots = false) =>
        new(PlannerConstants.DefaultDataPath, PlannerConstants.DefaultOutputFolder, MinerTier.Mk1, NodePurity.Normal, includeIngots);

    /// <summary>An ordered excess map.</summary>
    public static OrderedDictionary<ItemId, double> ExcessOf(params (string Item, double Rate)[] entries)
    {
        var map = new OrderedDictionary<ItemId, double>();
        foreach ((string item, double rate) in entries)
        {
            map[Item(item)] = rate;
        }

        return map;
    }
}
