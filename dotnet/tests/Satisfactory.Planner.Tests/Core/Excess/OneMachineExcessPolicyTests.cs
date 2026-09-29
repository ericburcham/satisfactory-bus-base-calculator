using Satisfactory.Planner.Core.Excess;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Tests.Core.Excess;

[TestFixture]
public sealed class OneMachineExcessPolicyTests
{
    private static readonly ItemId Ore = new("Desc_OreIron_C");
    private static readonly ItemId Coal = new("Desc_Coal_C");
    private static readonly ItemId Steel = new("Desc_SteelIngot_C");
    private static readonly ItemId Plastic = new("Desc_Plastic_C");
    private static readonly ItemId Residue = new("Desc_HeavyOilResidue_C");

    private static ManufacturingRecipe Recipe(double time, IReadOnlyList<ItemAmount> ingredients, IReadOnlyList<ItemAmount> products) =>
        new(new RecipeId("R"), "R", false, time, new BuildingId("B"), ingredients, products, false, 0, 0);

    [Test]
    public void ExcessRate_IsOneMachineAtFullClock()
    {
        ManufacturingRecipe steel = Recipe(4, [new(Ore, 3), new(Coal, 3)], [new(Steel, 3)]);

        Assert.That(new OneMachineExcessPolicy().ExcessRate(Steel, steel), Is.EqualTo(45.0));
    }

    [Test]
    public void ExcessRate_UsesTheProductEntryOfTheItem_FirstOneWhenRepeated()
    {
        ManufacturingRecipe recipe = Recipe(6, [], [new(Plastic, 2), new(Residue, 1), new(Residue, 5)]);
        var policy = new OneMachineExcessPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(policy.ExcessRate(Plastic, recipe), Is.EqualTo(20.0));
            Assert.That(policy.ExcessRate(Residue, recipe), Is.EqualTo(10.0));
        });
    }

    [Test]
    public void ExcessRate_NotAProduct_Throws()
    {
        ManufacturingRecipe steel = Recipe(4, [new(Ore, 3)], [new(Steel, 3)]);

        Assert.Multiple(() =>
        {
            Assert.That(() => new OneMachineExcessPolicy().ExcessRate(Ore, steel), Throws.ArgumentException);
            Assert.That(() => new OneMachineExcessPolicy().ExcessRate(Ore, null!), Throws.ArgumentNullException);
        });
    }
}
