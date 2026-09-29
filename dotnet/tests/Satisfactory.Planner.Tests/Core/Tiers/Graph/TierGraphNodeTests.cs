using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Tests.Core.Tiers.Graph;

[TestFixture]
public sealed class TierGraphNodeTests
{
    [Test]
    public void Constructor_KeepsTheIdAndToStringReturnsIt()
    {
        var node = new TestTierNode("node_1");

        Assert.Multiple(() =>
        {
            Assert.That(node.Id, Is.EqualTo("node_1"));
            Assert.That(node.ToString(), Is.EqualTo("node_1"));
        });
    }

    [Test]
    public void Constructor_RejectsANullOrEmptyId()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new TestTierNode(null!), Throws.ArgumentNullException);
            Assert.That(() => new TestTierNode(string.Empty), Throws.ArgumentException);
        });
    }

    [Test]
    public void EveryNodeType_HasItsOwnKind()
    {
        var recipe = new ManufacturingRecipe(
            new RecipeId("Recipe_IngotIron_C"), "Iron Ingot", false, 2, new BuildingId("Desc_SmelterMk1_C"),
            [new ItemAmount(new ItemId("Desc_OreIron_C"), 1)], [new ItemAmount(new ItemId("Desc_IronIngot_C"), 1)],
            false, 0, 0);
        TierGraphNode[] nodes =
        [
            new MachineGroupNode(recipe, 1, "Iron Ingot", "Smelter", false),
            new RawResourceInputNode(new ItemId("Desc_OreIron_C"), "Iron Ore"),
            new BusInputNode(new ItemId("Desc_IronPlate_C"), "Iron Plate"),
            new SinkNode(),
        ];

        Assert.That(
            nodes.Select(n => n.Kind),
            Is.EqualTo(ExpectedKinds()));
    }

    private static TierGraphNodeKind[] ExpectedKinds() =>
        [TierGraphNodeKind.Machine, TierGraphNodeKind.RawInput, TierGraphNodeKind.BusInput, TierGraphNodeKind.Sink];

    private sealed class TestTierNode(string id) : TierGraphNode(id)
    {
        public override TierGraphNodeKind Kind => TierGraphNodeKind.Sink;

        public override string Label => "test";
    }
}
