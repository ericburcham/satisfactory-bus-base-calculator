using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Tests.Core.Model;

[TestFixture]
public sealed class BuildingTests
{
    [Test]
    public void Properties_HoldConstructorValues()
    {
        var building = new Building(new BuildingId("Desc_SmelterMk1_C"), "Smelter", 4, PowerConsumptionIsInteger: true);
        var none = new Building(new BuildingId("Desc_Wall_C"), "Wall", null, PowerConsumptionIsInteger: false);

        Assert.Multiple(() =>
        {
            Assert.That(building.PowerConsumptionMw, Is.EqualTo(4));
            Assert.That(building.PowerConsumptionIsInteger, Is.True);
            Assert.That(none.PowerConsumptionMw, Is.Null);
        });
    }
}
