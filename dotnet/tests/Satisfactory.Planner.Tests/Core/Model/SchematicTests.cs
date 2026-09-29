using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Model;

[TestFixture]
public sealed class SchematicTests
{
    [Test]
    public void Properties_HoldConstructorValues()
    {
        var schematic = new Schematic(
            new SchematicId("Schematic_1-1_C"),
            "Base Building",
            SchematicType.Milestone,
            1,
            [new SchematicId("Schematic_0-1_C")],
            [new ItemAmount(new ItemId("Desc_IronPlate_C"), 50)],
            [new RecipeId("Recipe_ConstructorMk1_C")]);

        Assert.Multiple(() =>
        {
            Assert.That(schematic.Type, Is.EqualTo(SchematicType.Milestone));
            Assert.That(schematic.Tier, Is.EqualTo(1));
            Assert.That(schematic.Required, Is.EqualTo(ExpectedSequence.Of(new SchematicId("Schematic_0-1_C"))));
            Assert.That(schematic.Cost, Has.Count.EqualTo(1));
            Assert.That(schematic.UnlocksRecipes, Is.EqualTo(ExpectedSequence.Of(new RecipeId("Recipe_ConstructorMk1_C"))));
        });
    }
}
