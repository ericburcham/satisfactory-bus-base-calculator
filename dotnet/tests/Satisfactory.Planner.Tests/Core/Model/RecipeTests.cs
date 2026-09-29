using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Tests.Core.Model;

[TestFixture]
public sealed class RecipeTests
{
    private static readonly ItemId Ore = new("Desc_OreIron_C");
    private static readonly ItemId Ingot = new("Desc_IronIngot_C");
    private static readonly ItemId Slag = new("Desc_Slag_C");

    private static ManufacturingRecipe Sample(double time = 2) => new(
        new RecipeId("Recipe_IngotIron_C"),
        "Iron Ingot",
        IsAlternate: false,
        time,
        new BuildingId("Desc_SmelterMk1_C"),
        [new ItemAmount(Ore, 1)],
        [new ItemAmount(Ingot, 1), new ItemAmount(Slag, 3)],
        IsVariablePower: false,
        MinPowerMw: 0,
        MaxPowerMw: 0);

    [Test]
    public void MainProduct_IsFirstProduct()
    {
        Assert.That(Sample().MainProduct, Is.EqualTo(new ItemAmount(Ingot, 1)));
    }

    [Test]
    public void PerMinute_IsAmountTimesSixtyThenDividedByTime()
    {
        // Python: 0.1 * 60 / 9 == 0.6666666666666666, while 0.1 * (60 / 9) == 0.6666666666666667.
        ManufacturingRecipe recipe = Sample(time: 9);
        var amount = new ItemAmount(Ore, 0.1);

        double rate = recipe.PerMinute(amount);

        Assert.That(rate.ToString("R", System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo("0.6666666666666666"));
    }

    [Test]
    public void PerMinute_OfWholeNumbers()
    {
        Assert.That(Sample(time: 2).PerMinute(new ItemAmount(Ingot, 1)), Is.EqualTo(30.0));
    }

    [Test]
    public void ProducesAndConsumes_CheckProductsAndIngredients()
    {
        ManufacturingRecipe recipe = Sample();
        Assert.Multiple(() =>
        {
            Assert.That(recipe.Produces(Ingot), Is.True);
            Assert.That(recipe.Produces(Slag), Is.True);
            Assert.That(recipe.Produces(Ore), Is.False);
            Assert.That(recipe.Consumes(Ore), Is.True);
            Assert.That(recipe.Consumes(Ingot), Is.False);
        });
    }
}
