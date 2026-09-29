using Satisfactory.Planner.Core;
using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Excess;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Selection;
using Satisfactory.Planner.Core.Solving;
using Satisfactory.Planner.Tests.TestSupport;
using static Satisfactory.Planner.Tests.Core.Solving.PlanningTestKit;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class ByproductCreditOptimizerTests
{
    private const string B = "Desc_B_C";
    private const string W = "Desc_W_C";

    /// <summary>
    /// B One makes B plus a W byproduct nobody needs; with W credited it is the cheapest B recipe (1 ore), without the
    /// credit B Two (1.5 ore) is.
    /// </summary>
    private static GameData WastedByproductData() => new GameDataBuilder()
        .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
        .WithItem(B, "B")
        .WithItem(W, "W")
        .WithBuilding(Constructor, "Constructor", 4)
        .WithRecipe("Recipe_W_C", "W", Constructor, 4, [(IronOre, 1)], [(W, 1)])
        .WithRecipe("Recipe_BOne_C", "B One", Constructor, 8, [(IronOre, 2)], [(B, 1), (W, 1)])
        .WithRecipe("Recipe_BTwo_C", "B Two", Constructor, 8, [(IronOre, 3)], [(B, 2)])
        .Build();

    private static ProductionPlan Optimize(
        GameData data,
        IReadOnlyList<ItemId> targets,
        IProductionSolver? solver = null,
        OrderedDictionary<ItemId, double>? fixedExcess = null,
        OrderedDictionary<ItemId, Recipe>? fixedRecipes = null,
        IExistingCapacity? existing = null) =>
        Optimizer(data, solver).Optimize(
            targets,
            fixedExcess ?? new OrderedDictionary<ItemId, double>(),
            fixedRecipes ?? new OrderedDictionary<ItemId, Recipe>(),
            Context(data),
            existing ?? NoExistingCapacity.Instance);

    [Test]
    public void Optimize_NothingWasted_ReturnsTheFirstRoundWithTheExcessRecipes()
    {
        GameData data = Steel().Build();
        var solver = new CountingSolver(Solver(data));

        ProductionPlan plan = Optimize(data, [Item(SteelIngot)], solver);

        Assert.Multiple(() =>
        {
            Assert.That(solver.Calls, Is.EqualTo(1));
            Assert.That(plan.Excess[Item(SteelIngot)], Is.EqualTo(45.0));
            Assert.That(plan.ExcessRecipes[Item(SteelIngot)].Id.Value, Is.EqualTo("Recipe_IngotSteel_C"));
        });
    }

    [Test]
    public void Optimize_FixedExcessComesFirstAndKeepsItsRateAndRecipe()
    {
        GameData data = ModularFrames().Build();
        Recipe oldRecipe = data.Recipes.Single(r => r.Id.Value == "Recipe_IronRod_C");
        var fixedExcess = ExcessOf((IronRod, 15), (IronPlate, 20));
        var fixedRecipes = new OrderedDictionary<ItemId, Recipe> { [Item(IronRod)] = oldRecipe, [Item(IronPlate)] = data.Recipes.Single(r => r.Id.Value == "Recipe_IronPlate_C") };

        ProductionPlan plan = Optimize(
            data, [Item(IronPlate), Item(IronRod), Item(Screw)], fixedExcess: fixedExcess, fixedRecipes: fixedRecipes);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Excess.Keys, Is.EqualTo(ExpectedSequence.Of(Item(IronRod), Item(IronPlate), Item(Screw))));
            Assert.That(plan.Excess[Item(Screw)], Is.EqualTo(40.0));
            Assert.That(plan.ExcessRecipes.Keys, Is.EqualTo(ExpectedSequence.Of(Item(IronRod), Item(IronPlate), Item(Screw))));
            Assert.That(plan.ExcessRecipes[Item(IronRod)], Is.SameAs(oldRecipe));
            Assert.That(fixedExcess.Keys, Has.Count.EqualTo(2), "the caller's maps are not modified");
        });
    }

    [Test]
    public void Optimize_WastedByproductLosesItsCredit()
    {
        GameData data = WastedByproductData();
        var solver = new CountingSolver(Solver(data));

        ProductionPlan plan = Optimize(data, [Item(B)], solver);

        Assert.Multiple(() =>
        {
            Assert.That(solver.Calls, Is.EqualTo(2));
            Assert.That(solver.Plans[0].Sink.Keys, Is.EqualTo(ExpectedSequence.Of(Item(W))), "round 1 sinks W");
            Assert.That(plan.ExcessRecipes[Item(B)].Id.Value, Is.EqualTo("Recipe_BTwo_C"));
            Assert.That(plan.Excess[Item(B)], Is.EqualTo(15.0));
            Assert.That(plan.Machines.Select(m => (m.Recipe.Id.Value, m.Count)), Is.EqualTo(ExpectedSequence.Of(("Recipe_BTwo_C", 1.0))));
            Assert.That(plan.Sink, Is.Empty);
        });
    }

    [Test]
    public void Optimize_SinkThatStaysAfterCreditIsDenied_IsReturned()
    {
        GameData data = PlasticWithResidue()
            .WithItem(PetroleumCoke, "Petroleum Coke")
            .WithRecipe("Recipe_PetroleumCoke_C", "Petroleum Coke", Refinery, 6, [(HeavyOilResidue, 4)], [(PetroleumCoke, 12)])
            .Build();
        var solver = new CountingSolver(Solver(data));

        ProductionPlan plan = Optimize(data, [Item(Plastic)], solver);

        Assert.Multiple(() =>
        {
            Assert.That(solver.Calls, Is.EqualTo(2), "round 2 wastes only items that already earn no credit");
            Assert.That(plan.Sink.Keys, Is.EqualTo(ExpectedSequence.Of(Item(PetroleumCoke))));
            Assert.That(plan.Disposal.Keys, Is.EqualTo(ExpectedSequence.Of(Item(HeavyOilResidue))));
        });
    }

    [Test]
    public void Optimize_UndisposableFluid_AvoidsRecipesThatLeaveItOver()
    {
        GameData data = PlasticWithResidue()
            .WithRecipe("Recipe_Alternate_CleanPlastic_C", "Alternate: Clean Plastic", Refinery, 6, [(CrudeOil, 4)], [(Plastic, 2)], alternate: true)
            .Build();
        var solver = new CountingSolver(Solver(data));

        ProductionPlan plan = Optimize(data, [Item(Plastic)], solver);

        Assert.Multiple(() =>
        {
            Assert.That(solver.Calls, Is.EqualTo(2));
            Assert.That(solver.Undisposable, Is.EqualTo(ExpectedSequence.Of(Item(HeavyOilResidue))));
            Assert.That(plan.ExcessRecipes[Item(Plastic)].Id.Value, Is.EqualTo("Recipe_Alternate_CleanPlastic_C"));
            Assert.That(plan.Machines.Select(m => m.Recipe.Id.Value), Is.EqualTo(ExpectedSequence.Of("Recipe_Alternate_CleanPlastic_C")));
        });
    }

    [Test]
    public void Optimize_TargetWithoutRecipe_Throws()
    {
        GameData data = PlasticWithResidue().Build();

        // Round 1 forbids the residue byproduct; round 2 has no plastic recipe left.
        Assert.That(
            () => Optimize(data, [Item(Plastic)]),
            Throws.TypeOf<PlannerException>().With.Message.EqualTo("Plastic is not machine-producible"));
    }

    [Test]
    public void Optimize_EveryRoundUndisposable_Throws()
    {
        GameData data = Steel().Build();
        var solver = new AlwaysUndisposableSolver();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Optimize(data, [Item(SteelIngot)], solver),
                Throws.TypeOf<PlannerException>().With.Message.Contains("Heavy"));
            Assert.That(solver.Calls, Is.EqualTo(20));
        });
    }

    [Test]
    public void Optimize_UsesExistingCapacity()
    {
        GameData data = IronRods().Build();
        var existing = new ExistingCapacity();
        existing.Add(data.Recipes.Single(r => r.Id.Value == "Recipe_IngotIron_C"), 1);

        ProductionPlan plan = Optimize(data, [Item(IronRod)], existing: existing);

        Assert.That(plan.Machines.Select(m => (m.Recipe.Id.Value, m.Count)), Is.EqualTo(ExpectedSequence.Of(("Recipe_IronRod_C", 1.0), ("Recipe_IngotIron_C", 0.5))));
    }

    [Test]
    public void Optimize_EachCallStartsWithAFreshCreditPolicy()
    {
        GameData data = WastedByproductData();
        ByproductCreditOptimizer optimizer = Optimizer(data);
        ITierContext context = Context(data);

        ProductionPlan first = optimizer.Optimize([Item(B)], ExcessOf(), new OrderedDictionary<ItemId, Recipe>(), context, NoExistingCapacity.Instance);
        var solver = new CountingSolver(Solver(data));
        ProductionPlan second = Optimizer(data, solver).Optimize([Item(B)], ExcessOf(), new OrderedDictionary<ItemId, Recipe>(), context, NoExistingCapacity.Instance);

        Assert.Multiple(() =>
        {
            Assert.That(second.ExcessRecipes[Item(B)].Id, Is.EqualTo(first.ExcessRecipes[Item(B)].Id));
            Assert.That(solver.Calls, Is.EqualTo(2));
        });
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        var costs = CostCalculator();
        var selector = new CheapestRecipeSelector();
        var excess = new OneMachineExcessPolicy();
        var solver = new AlwaysUndisposableSolver();
        var disposal = new FluidDisposalFinderFactory();

        Assert.Multiple(() =>
        {
            Assert.That(() => new ByproductCreditOptimizer(null!, selector, excess, solver, disposal), Throws.ArgumentNullException);
            Assert.That(() => new ByproductCreditOptimizer(costs, null!, excess, solver, disposal), Throws.ArgumentNullException);
            Assert.That(() => new ByproductCreditOptimizer(costs, selector, null!, solver, disposal), Throws.ArgumentNullException);
            Assert.That(() => new ByproductCreditOptimizer(costs, selector, excess, null!, disposal), Throws.ArgumentNullException);
            Assert.That(() => new ByproductCreditOptimizer(costs, selector, excess, solver, null!), Throws.ArgumentNullException);
        });
    }

    /// <summary>Counts the solves and remembers their plans and undisposable fluids.</summary>
    private sealed class CountingSolver(IProductionSolver inner) : IProductionSolver
    {
        public int Calls { get; private set; }

        public List<ProductionPlan> Plans { get; } = [];

        public List<ItemId> Undisposable { get; } = [];

        public ProductionPlan Solve(IReadOnlyDictionary<ItemId, double> excess, SolveContext context)
        {
            Calls++;
            try
            {
                ProductionPlan plan = inner.Solve(excess, context);
                Plans.Add(plan);
                return plan;
            }
            catch (UndisposableFluidException e)
            {
                Undisposable.Add(e.Fluid);
                throw;
            }
        }
    }

    /// <summary>A solver that always reports an undisposable fluid.</summary>
    private sealed class AlwaysUndisposableSolver : IProductionSolver
    {
        public int Calls { get; private set; }

        public ProductionPlan Solve(IReadOnlyDictionary<ItemId, double> excess, SolveContext context)
        {
            Calls++;
            throw new UndisposableFluidException(new ItemId("Desc_HeavyOilResidue_C"));
        }
    }
}
