using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Tests.Core.Tiers.Graph;

[TestFixture]
public sealed class ExternalInputNodeTests
{
    [Test]
    public void IdFor_PrefixesTheItemIdWithIn()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ExternalInputNode.IdPrefix, Is.EqualTo("in_"));
            Assert.That(ExternalInputNode.IdFor(new ItemId("Desc_Water_C")), Is.EqualTo("in_Desc_Water_C"));
        });
    }

    [Test]
    public void RawAndBusNodesOfTheSameItem_ShareTheIdButNotTheLabel()
    {
        var item = new ItemId("Desc_Coal_C");
        ExternalInputNode raw = new RawResourceInputNode(item, "Coal");
        ExternalInputNode bus = new BusInputNode(item, "Coal");

        Assert.Multiple(() =>
        {
            Assert.That(raw.Id, Is.EqualTo(bus.Id));
            Assert.That(raw.Id, Is.EqualTo(ExternalInputNode.IdFor(item)));
            Assert.That(raw.Label, Is.EqualTo("Coal\n(raw)"));
            Assert.That(bus.Label, Is.EqualTo("Coal\n(bus)"));
        });
    }
}
