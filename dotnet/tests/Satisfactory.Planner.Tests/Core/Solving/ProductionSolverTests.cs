using Satisfactory.Planner.Core;
using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Selection;
using Satisfactory.Planner.Core.Solving;
using Satisfactory.Planner.Tests.TestSupport;
using static Satisfactory.Planner.Tests.Core.Solving.PlanningTestKit;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class ProductionSolverTests
{
    private const double Tolerance = 1e-9;
    private const string Widget = "Desc_Widget_C";
    private const string Gadget = "Desc_Gadget_C";

    private static ProductionPlan Solve(GameData data, OrderedDictionary<ItemId, double> excess, IExistingCapacity? existing = null)
    {
        ITierContext context = Context(data);
        return Solver(data).Solve(excess, SolveContext(context, existing));
    }

    private static double CountOf(ProductionPlan plan, string recipe) =>
        plan.Machines.Single(m => m.Recipe.Id.Value == recipe).Count;

    private static double RateOf(ProductionPlan plan, string resource) =>
        plan.Extractors.Single(x => x.Resource.Value == resource).RatePerMinute;

    [Test]
    public void Solve_Steel_OneFoundryFromOreAndCoal()
    {
        GameData data = Steel().Build();

        ProductionPlan plan = Solve(data, ExcessOf((SteelIngot, 45)));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Machines.Select(m => (m.Recipe.Id.Value, m.Count, m.PowerMw)), Is.EqualTo(ExpectedSequence.Of(("Recipe_IngotSteel_C", 1.0, 16.0))));
            Assert.That(plan.Extractors.Select(x => (x.Resource.Value, x.RatePerMinute)), Is.EqualTo(ExpectedSequence.Of((IronOre, 45.0), (Coal, 45.0))));
            Assert.That(plan.Extractors[0].Spec, Is.EqualTo(PlannerConstants.Miners[MinerTier.Mk1]));
            Assert.That(plan.Excess, Is.EqualTo(ExcessOf((SteelIngot, 45))));
            Assert.That(plan.Sink, Is.Empty);
            Assert.That(plan.Disposal, Is.Empty);
            Assert.That(plan.ExcessRecipes, Is.Empty);
        });
    }

    [Test]
    public void Solve_ModularFrames_ExactMachineCounts()
    {
        GameData data = ModularFrames().Build();

        // One excess machine each of Modular Frame, Reinforced Iron Plate, Iron Plate, Iron Rod and Screws.
        ProductionPlan plan = Solve(
            data, ExcessOf((ModularFrame, 2), (ReinforcedPlate, 5), (IronPlate, 20), (IronRod, 15), (Screw, 40)));

        Assert.Multiple(() =>
        {
            Assert.That(CountOf(plan, "Recipe_ModularFrame_C"), Is.EqualTo(1.0).Within(Tolerance));
            Assert.That(CountOf(plan, "Recipe_IronPlateReinforced_C"), Is.EqualTo(1.6).Within(Tolerance));
            Assert.That(CountOf(plan, "Recipe_IronPlate_C"), Is.EqualTo(3.4).Within(Tolerance));
            Assert.That(CountOf(plan, "Recipe_IronRod_C"), Is.EqualTo(61.0 / 15).Within(Tolerance));
            Assert.That(CountOf(plan, "Recipe_Screw_C"), Is.EqualTo(3.4).Within(Tolerance));
            Assert.That(CountOf(plan, "Recipe_IngotIron_C"), Is.EqualTo(163.0 / 30).Within(Tolerance));
            Assert.That(
                plan.Machines.Select(m => (int)Math.Ceiling(m.Count - PlannerConstants.NewMachineTolerance)),
                Is.EqualTo(ExpectedSequence.Of(1, 2, 4, 5, 4, 6)));
            Assert.That(plan.Machines.Sum(m => (int)Math.Ceiling(m.Count - PlannerConstants.NewMachineTolerance)), Is.EqualTo(22));
            Assert.That(RateOf(plan, IronOre), Is.EqualTo(163.0).Within(Tolerance));
            Assert.That(plan.Sink, Is.Empty);
        });
    }

    [Test]
    public void Solve_UsesExistingCapacityFirst_ThenBuildsTheRest()
    {
        GameData data = IronRods()
            .WithItem(Screw, "Screws")
            .WithItem(Rebar, "Iron Rebar")
            .WithRecipe("Recipe_Screw_C", "Screws", Constructor, 6, [(IronRod, 1)], [(Screw, 4)])
            .WithRecipe("Recipe_SpikedRebar_C", "Iron Rebar", Constructor, 4, [(IronRod, 1)], [(Rebar, 1)])
            .Build();
        ITierContext context = Context(data);
        Recipe rod = context.AvailableRecipes.Single(r => r.Id.Value == "Recipe_IronRod_C");
        var existing = new ExistingCapacity();
        existing.Add(rod, 2);

        // 15 rods/min excess + 10 rods/min into screws were covered by the 2 rod machines; 15 more for rebar.
        ProductionPlan plan = Solver(data).Solve(
            ExcessOf((IronRod, 15), (Screw, 40), (Rebar, 15)), SolveContext(context, existing));
        double rods = CountOf(plan, "Recipe_IronRod_C");

        Assert.Multiple(() =>
        {
            Assert.That(rods, Is.EqualTo(40.0 / 15).Within(Tolerance));
            Assert.That((int)Math.Ceiling(rods - existing.MachinesOf(rod.Id) - PlannerConstants.NewMachineTolerance), Is.EqualTo(1));
            Assert.That(plan.Machines[0].Recipe.Id, Is.EqualTo(rod.Id), "the existing rod machines are used first");
        });
    }

    [Test]
    public void Solve_ExistingCapacityOfAnotherRecipeForTheSameItem_IsUsedBeforeTheChosenRecipe()
    {
        GameData data = IronRods()
            .WithRecipe("Recipe_Alternate_OldRod_C", "Alternate: Old Rod", Constructor, 4, [(IronIngot, 2)], [(IronRod, 1)], alternate: true)
            .Build();
        ITierContext context = Context(data);
        Recipe oldRod = context.AvailableRecipes.Single(r => r.Id.Value == "Recipe_Alternate_OldRod_C");
        var existing = new ExistingCapacity();
        existing.Add(oldRod, 1);

        ProductionPlan plan = Solver(data).Solve(ExcessOf((IronRod, 30)), SolveContext(context, existing));

        Assert.Multiple(() =>
        {
            Assert.That(CountOf(plan, "Recipe_Alternate_OldRod_C"), Is.EqualTo(1.0));
            Assert.That(CountOf(plan, "Recipe_IronRod_C"), Is.EqualTo(1.0));
            Assert.That(CountOf(plan, "Recipe_IngotIron_C"), Is.EqualTo(1.5));
        });
    }

    [Test]
    public void Solve_ByproductSupplyingPartOfTheDemand_RemovesDedicatedMachines()
    {
        // Gadget makes 7.5 widgets/min as a byproduct: half of the dedicated widget machine is removed.
        GameData data = new GameDataBuilder()
            .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
            .WithItem(Widget, "Widget")
            .WithItem(Gadget, "Gadget")
            .WithBuilding(Constructor, "Constructor", 4)
            .WithRecipe("Recipe_Widget_C", "Widget", Constructor, 4, [(IronOre, 1)], [(Widget, 1)])
            .WithRecipe("Recipe_Gadget_C", "Gadget", Constructor, 8, [(IronOre, 2)], [(Gadget, 2), (Widget, 1)])
            .Build();

        ProductionPlan plan = Solve(data, ExcessOf((Widget, 15), (Gadget, 15)));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Machines.Select(m => (m.Recipe.Id.Value, m.Count)), Is.EqualTo(ExpectedSequence.Of(("Recipe_Widget_C", 0.5), ("Recipe_Gadget_C", 1.0))));
            Assert.That(RateOf(plan, IronOre), Is.EqualTo(22.5), "over-extraction is trimmed too");
            Assert.That(plan.Sink, Is.Empty);
        });
    }

    [Test]
    public void Solve_ByproductCoveringAllDemand_KeepsAZeroRow()
    {
        GameData data = new GameDataBuilder()
            .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
            .WithItem(Widget, "Widget")
            .WithItem(Gadget, "Gadget")
            .WithBuilding(Constructor, "Constructor", 4)
            .WithRecipe("Recipe_Widget_C", "Widget", Constructor, 4, [(IronOre, 1)], [(Widget, 1)])
            .WithRecipe("Recipe_Gadget_C", "Gadget", Constructor, 4, [(IronOre, 2)], [(Gadget, 1), (Widget, 1)])
            .Build();

        ProductionPlan plan = Solve(data, ExcessOf((Widget, 15), (Gadget, 15)));

        Assert.Multiple(() =>
        {
            Assert.That(CountOf(plan, "Recipe_Widget_C"), Is.Zero);
            Assert.That(plan.Machines.Single(m => m.Recipe.Id.Value == "Recipe_Widget_C").PowerMw, Is.Zero);
            Assert.That(RateOf(plan, IronOre), Is.EqualTo(30.0));
        });
    }

    [Test]
    public void Solve_LeftoverFluid_IsTurnedIntoASinkableSolid()
    {
        GameData data = PlasticWithResidue()
            .WithItem(PetroleumCoke, "Petroleum Coke")
            .WithRecipe("Recipe_PetroleumCoke_C", "Petroleum Coke", Refinery, 6, [(HeavyOilResidue, 4)], [(PetroleumCoke, 12)])
            .Build();

        ProductionPlan plan = Solve(data, ExcessOf((Plastic, 20)));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Machines.Select(m => (m.Recipe.Id.Value, m.Count)), Is.EqualTo(ExpectedSequence.Of(("Recipe_Plastic_C", 1.0), ("Recipe_PetroleumCoke_C", 0.25))));
            Assert.That(plan.Sink.Select(s => (s.Key.Value, s.Value)), Is.EqualTo(ExpectedSequence.Of((PetroleumCoke, 30.0))));
            Assert.That(plan.Disposal.Keys, Is.EqualTo(ExpectedSequence.Of(Item(HeavyOilResidue))));
            Assert.That(plan.Disposal[Item(HeavyOilResidue)].Rate, Is.EqualTo(10.0));
            Assert.That(plan.Disposal[Item(HeavyOilResidue)].Recipe.Id.Value, Is.EqualTo("Recipe_PetroleumCoke_C"));
            Assert.That(plan.Extractors.Select(x => (x.Resource.Value, x.RatePerMinute)), Is.EqualTo(ExpectedSequence.Of((CrudeOil, 30.0))));
            Assert.That(plan.Extractors[0].Spec, Is.EqualTo(PlannerConstants.OilExtractor));
        });
    }

    [Test]
    public void Solve_UndisposableFluid_Throws()
    {
        GameData data = PlasticWithResidue().Build();

        Assert.That(
            () => Solve(data, ExcessOf((Plastic, 20))),
            Throws.TypeOf<UndisposableFluidException>()
                .With.Property(nameof(UndisposableFluidException.Fluid)).EqualTo(Item(HeavyOilResidue)));
    }

    [Test]
    public void Solve_UnsinkableLeftover_Throws()
    {
        GameData data = new GameDataBuilder()
            .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
            .WithItem(Widget, "Widget")
            .WithItem("Desc_NuclearWaste_C", "Uranium Waste")
            .WithItem("Desc_PlutoniumWaste_C", "Plutonium Waste")
            .WithBuilding(Constructor, "Constructor", 4)
            .WithRecipe(
                "Recipe_Widget_C", "Widget", Constructor, 4, [(IronOre, 1)],
                [(Widget, 1), ("Desc_NuclearWaste_C", 1), ("Desc_PlutoniumWaste_C", 1)])
            .Build();

        Assert.That(
            () => Solve(data, ExcessOf((Widget, 15))),
            Throws.TypeOf<PlannerException>().With.Message.EqualTo("Left over but cannot be sunk: Uranium Waste, Plutonium Waste"));
    }

    [Test]
    public void Solve_ShortfallWithoutRecipe_Throws()
    {
        GameData data = ModularFrames().Build();
        ITierContext context = Context(data);
        Recipe plate = context.AvailableRecipes.Single(r => r.Id.Value == "Recipe_IronPlate_C");
        var choice = new RecipeChoice(new OrderedDictionary<ItemId, Recipe> { [Item(IronPlate)] = plate });

        Assert.That(
            () => Solver(data).Solve(ExcessOf((IronPlate, 20)), SolveContext(context, choice: choice)),
            Throws.TypeOf<PlannerException>().With.Message.EqualTo("No automatable source for Iron Ingot"));
    }

    [Test]
    public void Solve_NeverSettling_ThrowsDidNotConverge()
    {
        GameData data = new GameDataBuilder()
            .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
            .WithItem(Widget, "Widget")
            .WithItem(Gadget, "Gadget")
            .WithBuilding(Constructor, "Constructor", 4)
            .WithRecipe("Recipe_Widget_C", "Widget", Constructor, 4, [(Gadget, 1)], [(Widget, 1)])
            .WithRecipe("Recipe_Gadget_C", "Gadget", Constructor, 4, [(Widget, 1)], [(Gadget, 1)])
            .Build();
        ITierContext context = Context(data);
        var choice = new RecipeChoice(new OrderedDictionary<ItemId, Recipe>
        {
            [Item(Widget)] = data.Recipes[0],
            [Item(Gadget)] = data.Recipes[1],
        });

        Assert.That(
            () => Solver(data).Solve(ExcessOf((Widget, 15)), SolveContext(context, choice: choice)),
            Throws.TypeOf<PlannerException>().With.Message.EqualTo("Did not converge"));
    }

    [Test]
    public void Solve_DisposalThatKeepsLeavingFluid_ThrowsDidNotConverge()
    {
        const string Solid = "Desc_Solid_C";
        GameData data = new GameDataBuilder()
            .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
            .WithFluid(Water, "Water").WithResource(Water)
            .WithItem(Widget, "Widget")
            .WithItem(Solid, "Solid")
            .WithBuilding(Constructor, "Constructor", 4)
            .WithRecipe("Recipe_Widget_C", "Widget", Constructor, 4, [(IronOre, 1)], [(Widget, 1), (Water, 1)])
            .WithRecipe("Recipe_Spring_C", "Spring", Constructor, 4, [(Water, 1)], [(Solid, 1), (Water, 2)])
            .Build();

        Assert.That(
            () => Solve(data, ExcessOf((Widget, 15))),
            Throws.TypeOf<PlannerException>().With.Message.EqualTo("Byproduct disposal did not converge"));
    }

    [Test]
    public void Solve_NullArguments_Throw()
    {
        GameData data = Steel().Build();
        SolveContext context = SolveContext(Context(data));

        Assert.Multiple(() =>
        {
            Assert.That(() => Solver(data).Solve(null!, context), Throws.ArgumentNullException);
            Assert.That(() => Solver(data).Solve(ExcessOf(), null!), Throws.ArgumentNullException);
            Assert.That(() => new ProductionSolver(null!), Throws.ArgumentNullException);
        });
    }
}
