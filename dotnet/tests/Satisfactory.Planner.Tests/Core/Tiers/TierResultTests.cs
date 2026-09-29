using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Tiers;

namespace Satisfactory.Planner.Tests.Core.Tiers;

[TestFixture]
public sealed class TierResultTests
{
    private static readonly RecipeId RecipeZ = new("Recipe_Z");
    private static readonly RecipeId RecipeA = new("Recipe_A");
    private static readonly ItemId ItemB = new("B");
    private static readonly ItemId ItemA = new("A");
    private static readonly ItemId Water = new("Desc_Water_C");

    private static readonly Recipe Rod = new ManufacturingRecipe(
        new RecipeId("Recipe_IronRod_C"), "Iron Rod", false, 4, new BuildingId("B"), [],
        [new ItemAmount(new ItemId("Rod"), 1)], false, 0, 0);

    private static TierResult Sample() => new(
        Tier: 2,
        NewItems: [ItemB, ItemA],
        NewMachines: new OrderedDictionary<RecipeId, int> { [RecipeZ] = 2, [RecipeA] = 1 },
        Recipes: new OrderedDictionary<RecipeId, Recipe> { [RecipeZ] = Rod, [RecipeA] = Rod },
        Runs: new OrderedDictionary<RecipeId, double> { [RecipeZ] = 1.5, [RecipeA] = 0.25 },
        ExistingBefore: new OrderedDictionary<RecipeId, int> { [RecipeA] = 3 },
        Excess: new OrderedDictionary<ItemId, double> { [ItemB] = 15, [ItemA] = 7.5 },
        ExcessRecipe: new OrderedDictionary<ItemId, Recipe> { [ItemB] = Rod },
        Reused: new OrderedDictionary<ItemId, double> { [ItemA] = 0.5 },
        RawInput: new OrderedDictionary<ItemId, double> { [ItemB] = 45, [ItemA] = 30 },
        Sink: new OrderedDictionary<ItemId, double> { [ItemA] = 2 },
        Disposal: new OrderedDictionary<ItemId, (double Rate, Recipe Recipe)> { [Water] = (12.5, Rod) });

    [Test]
    public void Constructor_KeepsEveryValue()
    {
        TierResult result = Sample();

        Assert.Multiple(() =>
        {
            Assert.That(result.Tier, Is.EqualTo(2));
            Assert.That(result.NewItems, Is.EqualTo(new[] { ItemB, ItemA }));
            Assert.That(result.NewMachines[RecipeZ], Is.EqualTo(2));
            Assert.That(result.Recipes[RecipeA], Is.SameAs(Rod));
            Assert.That(result.Runs[RecipeA], Is.EqualTo(0.25));
            Assert.That(result.ExistingBefore[RecipeA], Is.EqualTo(3));
            Assert.That(result.Excess[ItemA], Is.EqualTo(7.5));
            Assert.That(result.ExcessRecipe[ItemB], Is.SameAs(Rod));
            Assert.That(result.Reused[ItemA], Is.EqualTo(0.5));
            Assert.That(result.RawInput[ItemB], Is.EqualTo(45));
            Assert.That(result.Sink[ItemA], Is.EqualTo(2));
            Assert.That(result.Disposal[Water].Rate, Is.EqualTo(12.5));
            Assert.That(result.Disposal[Water].Recipe, Is.SameAs(Rod));
        });
    }

    [Test]
    public void Collections_EnumerateInTheOrderGiven()
    {
        TierResult result = Sample();

        Assert.Multiple(() =>
        {
            Assert.That(result.NewItems, Is.EqualTo(new[] { ItemB, ItemA }));
            Assert.That(result.NewMachines.Keys, Is.EqualTo(new[] { RecipeZ, RecipeA }));
            Assert.That(result.Recipes.Keys, Is.EqualTo(new[] { RecipeZ, RecipeA }));
            Assert.That(result.Runs.Keys, Is.EqualTo(new[] { RecipeZ, RecipeA }));
            Assert.That(result.Excess.Keys, Is.EqualTo(new[] { ItemB, ItemA }));
            Assert.That(result.RawInput.Keys, Is.EqualTo(new[] { ItemB, ItemA }));
        });
    }

    [Test]
    public void With_ChangesOnlyTheGivenMemberAndLeavesTheOriginal()
    {
        TierResult original = Sample();

        TierResult copy = original with { Tier = 5 };

        Assert.Multiple(() =>
        {
            Assert.That(copy.Tier, Is.EqualTo(5));
            Assert.That(original.Tier, Is.EqualTo(2));
            Assert.That(copy.NewItems, Is.SameAs(original.NewItems));
            Assert.That(copy.NewMachines, Is.SameAs(original.NewMachines));
            Assert.That(copy.Runs, Is.SameAs(original.Runs));
            Assert.That(copy.Disposal, Is.SameAs(original.Disposal));
            Assert.That(copy, Is.Not.EqualTo(original));
            Assert.That(copy with { Tier = 2 }, Is.EqualTo(original), "same member references compare equal");
        });
    }
}
