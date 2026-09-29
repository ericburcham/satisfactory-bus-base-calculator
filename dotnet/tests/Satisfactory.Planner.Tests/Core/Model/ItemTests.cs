using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Tests.Core.Model;

[TestFixture]
public sealed class ItemTests
{
    [Test]
    public void Properties_HoldConstructorValues()
    {
        var item = new Item(new ItemId("Desc_Water_C"), "Water", IsFluid: true, 0, 5000, 0.5);

        Assert.Multiple(() =>
        {
            Assert.That(item.Id.Value, Is.EqualTo("Desc_Water_C"));
            Assert.That(item.Name, Is.EqualTo("Water"));
            Assert.That(item.IsFluid, Is.True);
            Assert.That(item.StackSize, Is.Zero);
            Assert.That(item.SinkPoints, Is.EqualTo(5000));
            Assert.That(item.EnergyValue, Is.EqualTo(0.5));
        });
    }
}
