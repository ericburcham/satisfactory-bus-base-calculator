using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Progression;

[TestFixture]
public sealed class TierContextTests
{
    private static GameData Sample() => new GameDataBuilder()
        .WithItem("Desc_OreIron_C", "Iron Ore").WithResource("Desc_OreIron_C")
        .WithFluid("Desc_Water_C", "Water").WithResource("Desc_Water_C")
        .WithItem("Desc_Mystery_C", "Mystery").WithResource("Desc_Mystery_C")
        .WithItem("Desc_IronIngot_C", "Iron Ingot")
        .WithItem("Desc_IronPlate_C", "Iron Plate")
        .WithItem("Desc_Scrap_C", "Scrap")
        .WithItem("Desc_Wet_C", "Wet Plate")
        .WithItem("Desc_Odd_C", "Odd")
        .WithBuilding("Desc_ConstructorMk1_C", "Constructor", 4)
        // Listed before the ingot recipe, so the fixed point needs a second pass.
        .WithRecipe("Recipe_IronPlate_C", "Iron Plate", "Desc_ConstructorMk1_C", 6, [("Desc_IronIngot_C", 3)], [("Desc_IronPlate_C", 2), ("Desc_Scrap_C", 1)])
        .WithRecipe("Recipe_IngotIron_C", "Iron Ingot", "Desc_ConstructorMk1_C", 2, [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
        .WithRecipe("Recipe_Wet_C", "Wet Plate", "Desc_ConstructorMk1_C", 2, [("Desc_IronPlate_C", 1), ("Desc_Water_C", 1)], [("Desc_Wet_C", 1)])
        .WithRecipe("Recipe_Odd_C", "Odd", "Desc_ConstructorMk1_C", 2, [("Desc_Mystery_C", 1)], [("Desc_Odd_C", 1)])
        .Build();

    private static TierContext ContextWith(GameData data, IEnumerable<Recipe> recipes, params string[] locked)
    {
        var lockedSet = new HashSet<ItemId>(locked.Select(c => new ItemId(c)));
        return new TierContext(3, data, recipes, lockedSet, new ExtractorCatalog(MinerTier.Mk1, NodePurity.Normal, lockedSet));
    }

    [Test]
    public void Automatable_IsTheFixedPointFromExtractableResources()
    {
        GameData data = Sample();

        TierContext context = ContextWith(data, data.Recipes, "Desc_Water_C");

        Assert.That(
            context.Automatable.Select(i => i.Value).Order(StringComparer.Ordinal),
            Is.EqualTo(ExpectedSequence.Of("Desc_IronIngot_C", "Desc_IronPlate_C", "Desc_OreIron_C", "Desc_Scrap_C")));
    }

    [Test]
    public void Automatable_IncludesUnlockedFluidsAndTheirProducts()
    {
        GameData data = Sample();

        TierContext context = ContextWith(data, data.Recipes);

        Assert.Multiple(() =>
        {
            Assert.That(context.Automatable, Does.Contain(new ItemId("Desc_Water_C")));
            Assert.That(context.Automatable, Does.Contain(new ItemId("Desc_Wet_C")));
            Assert.That(context.Automatable, Does.Not.Contain(new ItemId("Desc_Mystery_C")), "no extractor for it");
            Assert.That(context.Automatable, Does.Not.Contain(new ItemId("Desc_Odd_C")));
        });
    }

    [Test]
    public void Automatable_OnlyUsesTheAvailableRecipes()
    {
        GameData data = Sample();

        TierContext context = ContextWith(data, data.Recipes.Where(r => r.Id.Value != "Recipe_IngotIron_C"));

        Assert.That(
            context.Automatable.Select(i => i.Value).Order(StringComparer.Ordinal),
            Is.EqualTo(ExpectedSequence.Of("Desc_OreIron_C", "Desc_Water_C")));
    }

    [Test]
    public void Properties_ExposeTheInputs()
    {
        GameData data = Sample();
        var locked = new HashSet<ItemId> { new("Desc_Water_C") };
        var extractors = new ExtractorCatalog(MinerTier.Mk2, NodePurity.Pure, locked);
        var recipes = data.Recipes.Take(2).ToList();

        var context = new TierContext(4, data, recipes, locked, extractors);
        recipes.Clear();
        locked.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(context.Tier, Is.EqualTo(4));
            Assert.That(context.Data, Is.SameAs(data));
            Assert.That(context.AvailableRecipes.Select(r => r.Id.Value), Is.EqualTo(ExpectedSequence.Of("Recipe_IronPlate_C", "Recipe_IngotIron_C")));
            Assert.That(context.LockedRawResources, Is.EquivalentTo(ExpectedSequence.Of(new ItemId("Desc_Water_C"))));
            Assert.That(context.Extractors, Is.SameAs(extractors));
        });
    }
}
