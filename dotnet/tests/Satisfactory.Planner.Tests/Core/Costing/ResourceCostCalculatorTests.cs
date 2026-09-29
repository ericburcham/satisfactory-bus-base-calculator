using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Tests.TestSupport;
using static Satisfactory.Planner.Tests.Core.Solving.PlanningTestKit;

namespace Satisfactory.Planner.Tests.Core.Costing;

[TestFixture]
public sealed class ResourceCostCalculatorTests
{
    private const string Widget = "Desc_Widget_C";
    private const string Gadget = "Desc_Gadget_C";

    /// <summary>A makes A; B makes B plus half as much A as a byproduct (the byproduct credit case).</summary>
    private static GameData ByproductData() => new GameDataBuilder()
        .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
        .WithItem(Widget, "Widget")
        .WithItem(Gadget, "Gadget")
        .WithBuilding(Constructor, "Constructor", 4)
        .WithRecipe("Recipe_Widget_C", "Widget", Constructor, 4, [(IronOre, 1)], [(Widget, 1)])
        .WithRecipe("Recipe_Gadget_C", "Gadget", Constructor, 8, [(IronOre, 2)], [(Gadget, 2), (Widget, 1)])
        .Build();

    [Test]
    public void Calculate_ExtractableResourcesCostTheirWeight_WaterIsNearlyFree()
    {
        GameData data = new GameDataBuilder()
            .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
            .WithFluid(Water, "Water").WithResource(Water)
            .WithItem(Widget, "Widget")
            .WithBuilding(Constructor, "Constructor", 4)
            .WithRecipe("Recipe_Widget_C", "Widget", Constructor, 4, [(IronOre, 1), (Water, 10)], [(Widget, 1)])
            .Build();

        ResourceCostTable costs = Costs(Context(data));

        Assert.Multiple(() =>
        {
            Assert.That(costs.Costs[Item(IronOre)], Is.EqualTo(1.0));
            Assert.That(costs.Costs[Item(Water)], Is.EqualTo(0.001));
            Assert.That(costs.Costs[Item(Widget)], Is.EqualTo(1.0 + (0.001 * 10)));
        });
    }

    [Test]
    public void Calculate_RelaxesThroughTheChain()
    {
        GameData data = ModularFrames().Build();

        ResourceCostTable costs = Costs(Context(data));

        Assert.Multiple(() =>
        {
            Assert.That(costs.Costs[Item(IronIngot)], Is.EqualTo(1.0));
            Assert.That(costs.Costs[Item(IronPlate)], Is.EqualTo(1.5));
            Assert.That(costs.Costs[Item(IronRod)], Is.EqualTo(1.0));
            Assert.That(costs.Costs[Item(Screw)], Is.EqualTo(0.25));
            Assert.That(costs.Costs[Item(ReinforcedPlate)], Is.EqualTo(12.0));
            Assert.That(costs.Costs[Item(ModularFrame)], Is.EqualTo(24.0));
        });
    }

    [Test]
    public void Calculate_CreditsByproductsAtTheirOwnCost()
    {
        ITierContext context = Context(ByproductData());

        ResourceCostTable costs = Costs(context);

        // Gadget: (2 ore - 1 widget * 1.0) / 2 gadgets.
        Assert.That(costs.Costs[Item(Gadget)], Is.EqualTo(0.5));
    }

    [Test]
    public void Calculate_NoCredit_ByproductsEarnNothing()
    {
        ITierContext context = Context(ByproductData());
        var credit = new CreditPolicy();
        credit.DenyCredit([Item(Widget)]);

        ResourceCostTable costs = Costs(context, credit);

        Assert.Multiple(() =>
        {
            Assert.That(costs.Costs[Item(Gadget)], Is.EqualTo(1.0));

            // The gadget recipe still credits gadgets when it makes widgets: (2 - 1.0 * 2) / 1.
            Assert.That(costs.Costs[Item(Widget)], Is.Zero);
        });
    }

    [Test]
    public void Calculate_ClampsAtZeroWhenCreditExceedsInput()
    {
        ResourceCostTable costs = Costs(Context(PlasticWithResidue().Build()));

        Assert.Multiple(() =>
        {
            Assert.That(costs.Costs[Item(Plastic)], Is.EqualTo(1.5));
            Assert.That(costs.Costs[Item(HeavyOilResidue)], Is.Zero);
        });
    }

    [Test]
    public void Calculate_ExtractableProductKeepsItsWeight()
    {
        GameData data = new GameDataBuilder()
            .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
            .WithFluid(Water, "Water").WithResource(Water)
            .WithItem(Widget, "Widget")
            .WithBuilding(Constructor, "Constructor", 4)
            .WithRecipe("Recipe_Wet_C", "Wet Widget", Constructor, 4, [(IronOre, 1)], [(Widget, 1), (Water, 2000)])
            .Build();

        ResourceCostTable costs = Costs(Context(data));

        Assert.Multiple(() =>
        {
            Assert.That(costs.Costs[Item(Water)], Is.EqualTo(0.001));
            Assert.That(costs.Costs[Item(Widget)], Is.Zero, "credit 2000 * 0.001 = 2 exceeds the 1 ore input");
        });
    }

    [Test]
    public void Calculate_ItemsThatCannotBeMadeHaveNoCost()
    {
        GameData data = new GameDataBuilder()
            .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
            .WithItem(Widget, "Widget")
            .WithItem(Gadget, "Gadget")
            .WithBuilding(Constructor, "Constructor", 4)
            .WithRecipe("Recipe_Gadget_C", "Gadget", Constructor, 4, [(Widget, 1)], [(Gadget, 1)])
            .Build();

        ResourceCostTable costs = Costs(Context(data));

        Assert.Multiple(() =>
        {
            Assert.That(costs.TryGetCost(Item(Gadget), out _), Is.False);
            Assert.That(costs.TryGetCost(Item(Widget), out _), Is.False);
            Assert.That(costs.Costs.Keys, Is.EqualTo(ExpectedSequence.Of(Item(IronOre))));
        });
    }

    [Test]
    public void Calculate_LockedResourcesAreNotCosted()
    {
        GameData data = new GameDataBuilder()
            .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
            .WithFluid(Water, "Water").WithResource(Water)
            .WithMilestone("Schematic_2_C", 2, ["Recipe_WaterPump_C"])
            .Build();

        ResourceCostTable tier1 = Costs(Context(data, 1));
        ResourceCostTable tier2 = Costs(Context(data, 2));

        Assert.Multiple(() =>
        {
            Assert.That(tier1.TryGetCost(Item(Water), out _), Is.False);
            Assert.That(tier2.TryGetCost(Item(Water), out double water), Is.True);
            Assert.That(water, Is.EqualTo(0.001));
        });
    }

    [Test]
    public void Calculate_KeepsTheCreditPolicyForCostOf()
    {
        ITierContext context = Context(ByproductData());
        var credit = new CreditPolicy();
        ResourceCostTable costs = Costs(context, credit);
        Recipe gadget = context.AvailableRecipes.Single(r => r.Id.Value == "Recipe_Gadget_C");

        double before = costs.CostOf(gadget, Item(Gadget));
        credit.DenyCredit([Item(Widget)]);
        double after = costs.CostOf(gadget, Item(Gadget));

        Assert.Multiple(() =>
        {
            Assert.That(before, Is.EqualTo(0.5));
            Assert.That(after, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void Constructor_NullWeights_Throws() =>
        Assert.That(() => new ResourceCostCalculator(null!), Throws.ArgumentNullException);
}
