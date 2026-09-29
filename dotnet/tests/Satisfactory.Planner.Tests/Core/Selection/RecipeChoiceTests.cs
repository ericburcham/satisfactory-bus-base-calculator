using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Selection;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Selection;

[TestFixture]
public sealed class RecipeChoiceTests
{
    private static readonly ItemId Rod = new("Desc_IronRod_C");
    private static readonly ItemId Plate = new("Desc_IronPlate_C");

    private static ManufacturingRecipe Recipe(string id, ItemId product) => new(
        new RecipeId(id), id, false, 4, new BuildingId("Desc_ConstructorMk1_C"), [], [new ItemAmount(product, 1)], false, 0, 0);

    [Test]
    public void For_ReturnsTheChosenRecipe_OrNull()
    {
        ManufacturingRecipe rod = Recipe("Recipe_IronRod_C", Rod);
        var source = new OrderedDictionary<ItemId, Recipe> { [Rod] = rod };
        var choice = new RecipeChoice(source);
        source[Plate] = Recipe("Recipe_IronPlate_C", Plate);

        Assert.Multiple(() =>
        {
            Assert.That(choice.For(Rod), Is.SameAs(rod));
            Assert.That(choice.For(Plate), Is.Null, "the source was copied");
            Assert.That(choice.Recipes.Keys, Is.EqualTo(ExpectedSequence.Of(Rod)));
        });
    }

    [Test]
    public void Constructor_Null_Throws() =>
        Assert.That(() => new RecipeChoice(null!), Throws.ArgumentNullException);
}
