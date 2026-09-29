using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Model;

[TestFixture]
public sealed class GameDataTests
{
    private static GameData Sample() => new GameDataBuilder()
        .WithItem("Desc_OreIron_C", "Iron Ore")
        .WithFluid("Desc_Water_C", "Water")
        .WithItem("Desc_IronIngot_C", "Iron Ingot")
        .WithResource("Desc_OreIron_C")
        .WithResource("Desc_Water_C")
        .WithBuilding("Desc_SmelterMk1_C", "Smelter", 4)
        .WithRecipe("Recipe_IngotIron_C", "Iron Ingot", "Desc_SmelterMk1_C", 2, [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
        .Build();

    [Test]
    public void GetItem_AndTryGetItem_FindDeclaredItems()
    {
        GameData data = Sample();

        Assert.Multiple(() =>
        {
            Assert.That(data.GetItem(new ItemId("Desc_IronIngot_C")).Name, Is.EqualTo("Iron Ingot"));
            Assert.That(data.TryGetItem(new ItemId("Desc_Water_C"), out Item? water), Is.True);
            Assert.That(water!.IsFluid, Is.True);
            Assert.That(data.TryGetItem(new ItemId("Desc_Nope_C"), out _), Is.False);
            Assert.That(() => data.GetItem(new ItemId("Desc_Nope_C")), Throws.TypeOf<KeyNotFoundException>());
        });
    }

    [Test]
    public void GetBuilding_AndTryGetBuilding_FindDeclaredBuildings()
    {
        GameData data = Sample();

        Assert.Multiple(() =>
        {
            Assert.That(data.GetBuilding(new BuildingId("Desc_SmelterMk1_C")).Name, Is.EqualTo("Smelter"));
            Assert.That(data.TryGetBuilding(new BuildingId("Desc_Nope_C"), out _), Is.False);
            Assert.That(() => data.GetBuilding(new BuildingId("Desc_Nope_C")), Throws.TypeOf<KeyNotFoundException>());
        });
    }

    [Test]
    public void ItemName_FallsBackToTheId()
    {
        GameData data = Sample();

        Assert.Multiple(() =>
        {
            Assert.That(data.ItemName(new ItemId("Desc_OreIron_C")), Is.EqualTo("Iron Ore"));
            Assert.That(data.ItemName(new ItemId("Desc_Unknown_C")), Is.EqualTo("Desc_Unknown_C"));
        });
    }

    [Test]
    public void IsFluid_IsFalseForUnknownItems()
    {
        GameData data = Sample();

        Assert.Multiple(() =>
        {
            Assert.That(data.IsFluid(new ItemId("Desc_Water_C")), Is.True);
            Assert.That(data.IsFluid(new ItemId("Desc_OreIron_C")), Is.False);
            Assert.That(data.IsFluid(new ItemId("Desc_Unknown_C")), Is.False);
        });
    }

    [Test]
    public void IsResource_ChecksTheResourceList()
    {
        GameData data = Sample();

        Assert.Multiple(() =>
        {
            Assert.That(data.IsResource(new ItemId("Desc_OreIron_C")), Is.True);
            Assert.That(data.IsResource(new ItemId("Desc_IronIngot_C")), Is.False);
            Assert.That(data.Resources, Is.EqualTo(ExpectedSequence.Of(new ItemId("Desc_OreIron_C"), new ItemId("Desc_Water_C"))));
        });
    }

    [Test]
    public void Lists_KeepTheGivenOrder()
    {
        GameData data = Sample();

        Assert.Multiple(() =>
        {
            Assert.That(
                data.Items.Select(i => i.Id.Value),
                Is.EqualTo(ExpectedSequence.Of("Desc_OreIron_C", "Desc_Water_C", "Desc_IronIngot_C")));
            Assert.That(data.Recipes.Select(r => r.Id.Value), Is.EqualTo(ExpectedSequence.Of("Recipe_IngotIron_C")));
            Assert.That(data.RecipeDefinitions.Select(r => r.Id.Value), Is.EqualTo(ExpectedSequence.Of("Recipe_IngotIron_C")));
        });
    }

    [Test]
    public void Constructor_RejectsDuplicateItemIds()
    {
        var item = new Item(new ItemId("Desc_A_C"), "A", false, 1, 0, 0);

        Assert.That(
            () => new GameData([item, item with { Name = "B" }], [], [], [], [], []),
            Throws.ArgumentException.With.Message.Contains("Desc_A_C"));
    }

    [Test]
    public void Constructor_RejectsDuplicateResources()
    {
        Assert.That(
            () => new GameData([], [], [], [new ItemId("Desc_A_C"), new ItemId("Desc_A_C")], [], []),
            Throws.ArgumentException);
    }

    [Test]
    public void Constructor_CopiesTheSequences()
    {
        var items = new List<Item> { new(new ItemId("Desc_A_C"), "A", false, 1, 0, 0) };
        var data = new GameData(items, [], [], [], [], []);

        items.Add(new Item(new ItemId("Desc_B_C"), "B", false, 1, 0, 0));

        Assert.That(data.Items, Has.Count.EqualTo(1));
    }
}
