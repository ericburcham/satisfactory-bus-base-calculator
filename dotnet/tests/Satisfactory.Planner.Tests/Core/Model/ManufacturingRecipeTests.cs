using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Tests.Core.Model;

[TestFixture]
public sealed class ManufacturingRecipeTests
{
    [Test]
    public void Properties_HoldConstructorValues()
    {
        var recipe = new ManufacturingRecipe(
            new RecipeId("Recipe_DarkMatter_C"),
            "Dark Matter Crystal",
            IsAlternate: true,
            2,
            new BuildingId("Desc_HadronCollider_C"),
            [],
            [new ItemAmount(new ItemId("Desc_DarkMatter_C"), 1)],
            IsVariablePower: true,
            MinPowerMw: 500,
            MaxPowerMw: 1500);

        Assert.Multiple(() =>
        {
            Assert.That(recipe.Id, Is.EqualTo(new RecipeId("Recipe_DarkMatter_C")));
            Assert.That(recipe.Name, Is.EqualTo("Dark Matter Crystal"));
            Assert.That(recipe.IsAlternate, Is.True);
            Assert.That(recipe.TimeSeconds, Is.EqualTo(2));
            Assert.That(recipe.BuildingId, Is.EqualTo(new BuildingId("Desc_HadronCollider_C")));
            Assert.That(recipe.IsVariablePower, Is.True);
            Assert.That(recipe.MinPowerMw, Is.EqualTo(500));
            Assert.That(recipe.MaxPowerMw, Is.EqualTo(1500));
            Assert.That(recipe, Is.InstanceOf<Recipe>());
        });
    }
}
