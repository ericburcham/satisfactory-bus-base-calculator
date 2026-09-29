using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Tests.Core.Costing;

[TestFixture]
public sealed class DefaultResourceWeightsTests
{
    [Test]
    public void WeightOf_WaterIsNearlyFree_EverythingElseCountsOne()
    {
        var weights = new DefaultResourceWeights();

        Assert.Multiple(() =>
        {
            Assert.That(weights.WeightOf(new ItemId("Desc_Water_C")), Is.EqualTo(0.001));
            Assert.That(weights.WeightOf(new ItemId("Desc_OreIron_C")), Is.EqualTo(1.0));
            Assert.That(weights.WeightOf(new ItemId("Desc_LiquidOil_C")), Is.EqualTo(1.0));
            Assert.That(weights.WeightOf(new ItemId("Anything")), Is.EqualTo(1.0));
        });
    }
}
