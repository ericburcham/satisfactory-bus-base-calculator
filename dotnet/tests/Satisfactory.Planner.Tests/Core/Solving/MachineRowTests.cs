using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Solving;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class MachineRowTests
{
    [Test]
    public void Record_HoldsItsValues()
    {
        var recipe = new ManufacturingRecipe(
            new RecipeId("Recipe_IronRod_C"), "Iron Rod", false, 4, new BuildingId("B"), [], [new ItemAmount(new ItemId("Rod"), 1)], false, 0, 0);

        var row = new MachineRow(recipe, 1.5, 6.0);

        Assert.Multiple(() =>
        {
            Assert.That(row.Recipe, Is.SameAs(recipe));
            Assert.That(row.Count, Is.EqualTo(1.5));
            Assert.That(row.PowerMw, Is.EqualTo(6.0));
            Assert.That(row, Is.EqualTo(new MachineRow(recipe, 1.5, 6.0)));
        });
    }
}
