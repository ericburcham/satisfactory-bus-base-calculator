using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Tests.Core.Tiers.Graph;

[TestFixture]
public sealed class BusInputNodeTests
{
    [Test]
    public void Node_IsABusInputWithTheInPrefixedIdAndABusLabel()
    {
        var node = new BusInputNode(new ItemId("Desc_IronPlate_C"), "Iron Plate");

        Assert.Multiple(() =>
        {
            Assert.That(node.Id, Is.EqualTo("in_Desc_IronPlate_C"));
            Assert.That(node.Item, Is.EqualTo(new ItemId("Desc_IronPlate_C")));
            Assert.That(node.ItemName, Is.EqualTo("Iron Plate"));
            Assert.That(node.Kind, Is.EqualTo(TierGraphNodeKind.BusInput));
            Assert.That(node.Label, Is.EqualTo("Iron Plate\n(bus)"));
        });
    }

    [Test]
    public void Label_UsesTheNameVerbatim()
    {
        var node = new BusInputNode(new ItemId("Desc_Unknown_C"), "Desc_Unknown_C");

        Assert.That(node.Label, Is.EqualTo("Desc_Unknown_C\n(bus)"));
    }

    [Test]
    public void Constructor_RejectsANullName()
    {
        Assert.That(() => new BusInputNode(new ItemId("Desc_IronPlate_C"), null!), Throws.ArgumentNullException);
    }
}
