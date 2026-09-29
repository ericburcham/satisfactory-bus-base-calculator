using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Solving;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class ProductionPlanTests
{
    private static ProductionPlan Empty() => new(
        [], [], new OrderedDictionary<ItemId, double>(), new OrderedDictionary<ItemId, double>(),
        new OrderedDictionary<ItemId, (double Rate, Recipe Recipe)>());

    [Test]
    public void ExcessRecipes_DefaultsToEmpty() =>
        Assert.That(Empty().ExcessRecipes, Is.Empty);

    [Test]
    public void ExcessRecipes_CanBeSetWithWith()
    {
        var recipe = new ManufacturingRecipe(
            new RecipeId("Recipe_IronRod_C"), "Iron Rod", false, 4, new BuildingId("B"), [], [new ItemAmount(new ItemId("Rod"), 1)], false, 0, 0);
        var recipes = new OrderedDictionary<ItemId, Recipe> { [new ItemId("Rod")] = recipe };

        ProductionPlan plan = Empty() with { ExcessRecipes = recipes };

        Assert.That(plan.ExcessRecipes[new ItemId("Rod")], Is.SameAs(recipe));
    }
}
