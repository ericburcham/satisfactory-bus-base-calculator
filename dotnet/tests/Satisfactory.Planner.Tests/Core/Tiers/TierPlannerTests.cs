using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Selection;
using Satisfactory.Planner.Core.Solving;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Tests.TestSupport;
using static Satisfactory.Planner.Tests.Core.Solving.PlanningTestKit;

namespace Satisfactory.Planner.Tests.Core.Tiers;

[TestFixture]
public sealed class TierPlannerTests
{
    private const double Tolerance = 1e-9;
    private const string RodRecipe = "Recipe_IronRod_C";
    private const string ScrewRecipe = "Recipe_Screw_C";
    private const string IngotRecipe = "Recipe_IngotIron_C";
    private const string RebarRecipe = "Recipe_SpikedRebar_C";

    /// <summary>Rods and screws from tier 0; Iron Rebar (15 rods/min per machine) unlocked by a milestone.</summary>
    private static GameData RebarData(int rebarTier) => IronRods()
        .WithItem(Screw, "Screws")
        .WithItem(Rebar, "Iron Rebar")
        .WithRecipe(ScrewRecipe, "Screws", Constructor, 6, [(IronRod, 1)], [(Screw, 4)])
        .WithRecipe(RebarRecipe, "Iron Rebar", Constructor, 4, [(IronRod, 1)], [(Rebar, 1)])
        .WithMilestone("Schematic_Rebar_C", rebarTier, [RebarRecipe])
        .Build();

    private static IReadOnlyList<TierResult> PlanAll(GameData data, bool includeIngots = false) =>
        TierPlanner(data).PlanAllTiers(Options(includeIngots));

    private static RecipeId[] Recipes(params string[] ids) => ids.Select(Recipe).ToArray();

    [Test]
    public void PlanAllTiers_PlansEveryTierInOrder()
    {
        IReadOnlyList<TierResult> results = PlanAll(RebarData(1));

        Assert.That(
            results.Select(r => r.Tier),
            Is.EqualTo(Enumerable.Range(PlannerConstants.FirstTier, PlannerConstants.LastTier - PlannerConstants.FirstTier + 1)));
    }

    [Test]
    public void PlanAllTiers_FirstTier_OneExcessMachinePerNewItem()
    {
        TierResult tier0 = PlanAll(RebarData(1))[0];

        Assert.Multiple(() =>
        {
            Assert.That(tier0.NewItems, Is.EqualTo(ExpectedSequence.Of(Item(IronRod), Item(Screw))), "ingots and resources are not targets");
            Assert.That(tier0.NewMachines.Keys, Is.EqualTo(Recipes(RodRecipe, ScrewRecipe, IngotRecipe)));
            Assert.That(tier0.NewMachines.Values, Is.EqualTo(ExpectedSequence.Of(2, 1, 1)));
            Assert.That(tier0.Recipes.Keys, Is.EqualTo(Recipes(RodRecipe, ScrewRecipe, IngotRecipe)));
            Assert.That(tier0.Runs[Recipe(RodRecipe)], Is.EqualTo(25.0 / 15).Within(Tolerance));
            Assert.That(tier0.Runs[Recipe(IngotRecipe)], Is.EqualTo(25.0 / 30).Within(Tolerance));
            Assert.That(tier0.ExistingBefore, Is.Empty);
            Assert.That(tier0.Excess.Keys, Is.EqualTo(ExpectedSequence.Of(Item(IronRod), Item(Screw))));
            Assert.That(tier0.Excess.Values, Is.EqualTo(ExpectedSequence.Of(15.0, 40.0)));
            Assert.That(tier0.ExcessRecipe.Values.Select(r => r.Id), Is.EqualTo(Recipes(RodRecipe, ScrewRecipe)));
            Assert.That(tier0.Reused, Is.Empty);
            Assert.That(tier0.RawInput.Keys, Is.EqualTo(ExpectedSequence.Of(Item(IronOre))));
            Assert.That(tier0.RawInput[Item(IronOre)], Is.EqualTo(25.0).Within(Tolerance));
            Assert.That(tier0.Sink, Is.Empty);
            Assert.That(tier0.Disposal, Is.Empty);
        });
    }

    [Test]
    public void PlanAllTiers_ExistingCapacityIsUsedFirst_OnlyTheShortfallIsBuilt()
    {
        // Tier 0 built 2 rod machines for 25 rods/min; a 15 rods/min rebar machine in tier 1 needs exactly 1 more.
        TierResult tier1 = PlanAll(RebarData(1))[1];

        Assert.Multiple(() =>
        {
            Assert.That(tier1.NewItems, Is.EqualTo(ExpectedSequence.Of(Item(Rebar))));
            Assert.That(tier1.ExistingBefore.Keys, Is.EqualTo(Recipes(RodRecipe, ScrewRecipe, IngotRecipe)));
            Assert.That(tier1.ExistingBefore.Values, Is.EqualTo(ExpectedSequence.Of(2, 1, 1)));
            Assert.That(tier1.Runs.Keys, Is.EqualTo(Recipes(RodRecipe, ScrewRecipe, RebarRecipe, IngotRecipe)));
            Assert.That(tier1.Runs[Recipe(RodRecipe)], Is.EqualTo(40.0 / 15).Within(Tolerance));
            Assert.That(tier1.Runs[Recipe(IngotRecipe)], Is.EqualTo(40.0 / 30).Within(Tolerance));
            Assert.That(tier1.NewMachines.Keys, Is.EqualTo(Recipes(RodRecipe, RebarRecipe, IngotRecipe)));
            Assert.That(tier1.NewMachines.Values, Is.EqualTo(ExpectedSequence.Of(1, 1, 1)));
            Assert.That(tier1.Excess.Keys, Is.EqualTo(ExpectedSequence.Of(Item(Rebar))));
            Assert.That(tier1.Excess[Item(Rebar)], Is.EqualTo(15.0));
            Assert.That(tier1.ExcessRecipe[Item(Rebar)].Id, Is.EqualTo(Recipe(RebarRecipe)));
        });
    }

    [Test]
    public void PlanAllTiers_ReusedIsTheSpareCapacityPutToWork()
    {
        TierResult tier1 = PlanAll(RebarData(1))[1];

        Assert.Multiple(() =>
        {
            Assert.That(tier1.Reused.Keys, Is.EqualTo(ExpectedSequence.Of(Item(IronRod), Item(IronIngot))));
            Assert.That(tier1.Reused[Item(IronRod)], Is.EqualTo(2 - (25.0 / 15)).Within(Tolerance));
            Assert.That(tier1.Reused[Item(IronIngot)], Is.EqualTo(1 - (25.0 / 30)).Within(Tolerance));
        });
    }

    [Test]
    public void PlanAllTiers_ExcessStaysFixedAndMachinesStayBuilt()
    {
        IReadOnlyList<TierResult> results = PlanAll(RebarData(1));
        TierResult tier1 = results[1];
        TierResult last = results[^1];

        Assert.Multiple(() =>
        {
            Assert.That(last.NewItems, Is.Empty);
            Assert.That(last.NewMachines, Is.Empty);
            Assert.That(last.Excess, Is.Empty);
            Assert.That(last.Reused, Is.Empty);
            Assert.That(last.ExistingBefore.Keys, Is.EqualTo(Recipes(RodRecipe, ScrewRecipe, IngotRecipe, RebarRecipe)));
            Assert.That(last.ExistingBefore.Values, Is.EqualTo(ExpectedSequence.Of(3, 1, 2, 1)));
            Assert.That(last.Runs.Keys, Is.EquivalentTo(tier1.Runs.Keys));
            foreach ((RecipeId id, double count) in tier1.Runs)
            {
                Assert.That(last.Runs[id], Is.EqualTo(count).Within(Tolerance), id.Value);
            }
        });
    }

    [Test]
    public void PlanAllTiers_RecipeOfALaterTierIsNeverUsedEarlier()
    {
        IReadOnlyList<TierResult> results = PlanAll(RebarData(2));

        Assert.Multiple(() =>
        {
            for (int tier = 0; tier < 2; tier++)
            {
                Assert.That(results[tier].NewItems, Does.Not.Contain(Item(Rebar)), $"tier {tier}");
                Assert.That(results[tier].Recipes.Keys, Does.Not.Contain(Recipe(RebarRecipe)), $"tier {tier}");
            }

            Assert.That(results[1].NewItems, Is.Empty);
            Assert.That(results[2].NewItems, Is.EqualTo(ExpectedSequence.Of(Item(Rebar))));
            Assert.That(results[2].Recipes.Keys, Does.Contain(Recipe(RebarRecipe)));
        });
    }

    [Test]
    public void PlanAllTiers_NewItemsExcludeResourcesUnsinkablesIngotsAndItemsWithoutRecipe()
    {
        const string Waste = "Desc_NuclearWaste_C";
        const string Widget = "Desc_Widget_C";
        GameData data = IronRods()
            .WithItem(Waste, "Uranium Waste")
            .WithItem(Widget, "Widget")
            .WithRecipe("Recipe_Waste_C", "Waste", Constructor, 4, [(IronOre, 1)], [(Waste, 1)])
            .WithRecipe("Recipe_UnpackageWidget_C", "Unpackage Widget", Constructor, 4, [(IronRod, 1)], [(Widget, 1)])
            .Build();

        TierResult withoutIngots = PlanAll(data)[0];
        TierResult withIngots = PlanAll(data, includeIngots: true)[0];

        Assert.Multiple(() =>
        {
            Assert.That(withoutIngots.NewItems, Is.EqualTo(ExpectedSequence.Of(Item(IronRod))));
            Assert.That(withIngots.NewItems, Is.EqualTo(ExpectedSequence.Of(Item(IronIngot), Item(IronRod))));
            Assert.That(withIngots.Excess[Item(IronIngot)], Is.EqualTo(30.0));
            Assert.That(withIngots.NewMachines[Recipe(IngotRecipe)], Is.EqualTo(2), "30 excess + 15 for the rod machine");
        });
    }

    [Test]
    public void PlanAllTiers_PassesTheSortedTargetsAndTheFixedExcessToTheOptimizer()
    {
        GameData data = RebarData(1);
        var contexts = new TierContextFactory(data, new UnlockTierResolver(data), MinerTier.Mk1, NodePurity.Normal);
        var optimizer = new RecordingOptimizer(Optimizer(data));
        var planner = new TierPlanner(contexts, CostCalculator(), new CheapestRecipeSelector(), optimizer);

        planner.PlanAllTiers(Options());

        Assert.Multiple(() =>
        {
            Assert.That(optimizer.Calls, Has.Count.EqualTo(10));
            Assert.That(optimizer.Calls[0].Targets, Is.EqualTo(ExpectedSequence.Of(Item(IronRod), Item(Screw))));
            Assert.That(optimizer.Calls[0].FixedExcess, Is.Empty);
            Assert.That(optimizer.Calls[1].Targets, Is.EqualTo(ExpectedSequence.Of(Item(IronRod), Item(Screw), Item(Rebar))));
            Assert.That(optimizer.Calls[1].FixedExcess, Is.EqualTo(ExpectedSequence.Of(Item(IronRod), Item(Screw))));
            Assert.That(optimizer.Calls[2].FixedExcess, Is.EqualTo(ExpectedSequence.Of(Item(IronRod), Item(Screw), Item(Rebar))));
            Assert.That(optimizer.Calls[2].ExistingMachines, Is.EqualTo(ExpectedSequence.Of(3, 1, 2, 1)));
        });
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        GameData data = RebarData(1);
        var contexts = new TierContextFactory(data, new UnlockTierResolver(data), MinerTier.Mk1, NodePurity.Normal);
        ResourceCostCalculator costs = CostCalculator();
        var selector = new CheapestRecipeSelector();
        ByproductCreditOptimizer optimizer = Optimizer(data);

        Assert.Multiple(() =>
        {
            Assert.That(() => new TierPlanner(null!, costs, selector, optimizer), Throws.ArgumentNullException);
            Assert.That(() => new TierPlanner(contexts, null!, selector, optimizer), Throws.ArgumentNullException);
            Assert.That(() => new TierPlanner(contexts, costs, null!, optimizer), Throws.ArgumentNullException);
            Assert.That(() => new TierPlanner(contexts, costs, selector, null!), Throws.ArgumentNullException);
            Assert.That(() => new TierPlanner(contexts, costs, selector, optimizer).PlanAllTiers(null!), Throws.ArgumentNullException);
        });
    }

    /// <summary>Records what the tier planner passes to the optimizer (copied at call time).</summary>
    private sealed class RecordingOptimizer(IPlanOptimizer inner) : IPlanOptimizer
    {
        public List<(IReadOnlyList<ItemId> Targets, IReadOnlyList<ItemId> FixedExcess, IReadOnlyList<int> ExistingMachines)> Calls { get; } = [];

        public ProductionPlan Optimize(
            IReadOnlyList<ItemId> targets,
            IReadOnlyDictionary<ItemId, double> fixedExcess,
            IReadOnlyDictionary<ItemId, Recipe> fixedExcessRecipes,
            ITierContext context,
            IExistingCapacity existing)
        {
            Calls.Add((targets.ToList(), fixedExcess.Keys.ToList(), existing.Machines.Select(m => m.Machines).ToList()));
            return inner.Optimize(targets, fixedExcess, fixedExcessRecipes, context, existing);
        }
    }
}
