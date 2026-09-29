using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Tests.Core.Tiers.Graph;

[TestFixture]
public sealed class RawResourceInputNodeTests
{
    [Test]
    public void Node_IsARawInputWithTheInPrefixedIdAndARawLabel()
    {
        var node = new RawResourceInputNode(new ItemId("Desc_OreIron_C"), "Iron Ore");

        Assert.Multiple(() =>
        {
            Assert.That(node.Id, Is.EqualTo("in_Desc_OreIron_C"));
            Assert.That(node.Item, Is.EqualTo(new ItemId("Desc_OreIron_C")));
            Assert.That(node.ItemName, Is.EqualTo("Iron Ore"));
            Assert.That(node.Kind, Is.EqualTo(TierGraphNodeKind.RawInput));
            Assert.That(node.Label, Is.EqualTo("Iron Ore\n(raw)"));
            Assert.That(node.ToString(), Is.EqualTo("in_Desc_OreIron_C"));
        });
    }

    [Test]
    public void Constructor_RejectsANullName()
    {
        Assert.That(() => new RawResourceInputNode(new ItemId("Desc_OreIron_C"), null!), Throws.ArgumentNullException);
    }
}
