using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Solving;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class ProductionBalanceTests
{
    private static readonly ItemId Ore = new("Desc_OreIron_C");
    private static readonly ItemId Ingot = new("Desc_IronIngot_C");
    private static readonly ItemId Plate = new("Desc_IronPlate_C");
    private static readonly ItemId Rod = new("Desc_IronRod_C");

    private static ManufacturingRecipe Recipe(string id, double time, IReadOnlyList<ItemAmount> ingredients, IReadOnlyList<ItemAmount> products) =>
        new(new RecipeId(id), id, false, time, new BuildingId("B"), ingredients, products, false, 0, 0);

    private static void AddRun(ProductionState state, Recipe recipe, double machines)
    {
        state.Recipes[recipe.Id] = recipe;
        state.Runs[recipe.Id] = machines;
    }

    [Test]
    public void Of_EmptyState_IsEmpty() =>
        Assert.That(ProductionBalance.Of(new ProductionState()), Is.Empty);

    [Test]
    public void Of_RunsThenExtractionThenDemand_InFirstTouchOrder()
    {
        var state = new ProductionState();
        AddRun(state, Recipe("Ingot", 2, [new(Ore, 1)], [new(Ingot, 1)]), 2);
        AddRun(state, Recipe("Plate", 6, [new(Ingot, 3)], [new(Plate, 2)]), 1);
        state.Extract[Ore] = 5;
        state.Demand[Plate] = 3;
        state.Demand[Rod] = 7;

        OrderedDictionary<ItemId, double> balance = ProductionBalance.Of(state);

        Assert.Multiple(() =>
        {
            Assert.That(balance.Keys, Is.EqualTo(ExpectedSequence.Of(Ingot, Ore, Plate, Rod)));
            Assert.That(balance[Ingot], Is.EqualTo(60.0 - 30.0));
            Assert.That(balance[Ore], Is.EqualTo(-60.0 + 5.0));
            Assert.That(balance[Plate], Is.EqualTo(20.0 - 3.0));
            Assert.That(balance[Rod], Is.EqualTo(-7.0));
        });
    }

    [Test]
    public void Of_ComputesTheMachineFactorFirst()
    {
        // Python: f = n * 60 / time; bal += amount * f. 3 * (0.1 * 60 / 3) is 6.0, 3 * 0.1 * 60 / 3 is not.
        var state = new ProductionState();
        AddRun(state, Recipe("Rod", 3, [], [new(Rod, 3)]), 0.1);

        Assert.That(ProductionBalance.Of(state)[Rod], Is.EqualTo(6.0));
    }

    [Test]
    public void Of_ZeroRunsStillListTheirItems()
    {
        var state = new ProductionState();
        AddRun(state, Recipe("Rod", 4, [new(Ingot, 1)], [new(Rod, 1)]), 0);

        OrderedDictionary<ItemId, double> balance = ProductionBalance.Of(state);

        Assert.That(balance.Keys, Is.EqualTo(ExpectedSequence.Of(Rod, Ingot)));
    }

    [Test]
    public void Of_Null_Throws() =>
        Assert.That(() => ProductionBalance.Of(null!), Throws.ArgumentNullException);
}
