using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Tests.Core.Model;

[TestFixture]
public sealed class RecipeDefinitionTests
{
    [Test]
    public void Properties_HoldConstructorValues()
    {
        var definition = new RecipeDefinition(
            new RecipeId("Recipe_SmelterMk1_C"),
            "Smelter",
            [new ItemAmount(new ItemId("Desc_IronRod_C"), 5)],
            [new ItemAmount(new ItemId("Desc_SmelterMk1_C"), 1)]);

        Assert.Multiple(() =>
        {
            Assert.That(definition.Id.Value, Is.EqualTo("Recipe_SmelterMk1_C"));
            Assert.That(definition.Name, Is.EqualTo("Smelter"));
            Assert.That(definition.Ingredients, Has.Count.EqualTo(1));
            Assert.That(definition.Products[0].Item.Value, Is.EqualTo("Desc_SmelterMk1_C"));
        });
    }
}
