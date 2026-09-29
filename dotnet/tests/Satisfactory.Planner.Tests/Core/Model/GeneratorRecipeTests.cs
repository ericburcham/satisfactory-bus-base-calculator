using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Tests.Core.Model;

[TestFixture]
public sealed class GeneratorRecipeTests
{
    [Test]
    public void Properties_HoldConstructorValues()
    {
        var recipe = new GeneratorRecipe(
            new RecipeId("Gen_Nuclear_Uranium"),
            "Burn Uranium Fuel Rod",
            IsAlternate: false,
            300,
            new BuildingId("Desc_GeneratorNuclear_C"),
            [new ItemAmount(new ItemId("Desc_NuclearFuelRod_C"), 1), new ItemAmount(new ItemId("Desc_Water_C"), 1200)],
            [new ItemAmount(new ItemId("Desc_NuclearWaste_C"), 50)],
            "Nuclear Power Plant",
            2500);

        Assert.Multiple(() =>
        {
            Assert.That(recipe.BuildingName, Is.EqualTo("Nuclear Power Plant"));
            Assert.That(recipe.PowerOutputMw, Is.EqualTo(2500));
            Assert.That(recipe.MainProduct.Item, Is.EqualTo(new ItemId("Desc_NuclearWaste_C")));
            Assert.That(recipe.PerMinute(recipe.MainProduct), Is.EqualTo(10.0));
            Assert.That(recipe, Is.InstanceOf<Recipe>());
        });
    }
}
