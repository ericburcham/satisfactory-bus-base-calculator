using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Solving;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class ExtractorRowTests
{
    [Test]
    public void Record_HoldsItsValues()
    {
        var row = new ExtractorRow(new ItemId("Desc_Water_C"), PlannerConstants.WaterExtractor, 240);

        Assert.Multiple(() =>
        {
            Assert.That(row.Resource, Is.EqualTo(new ItemId("Desc_Water_C")));
            Assert.That(row.Spec, Is.SameAs(PlannerConstants.WaterExtractor));
            Assert.That(row.RatePerMinute, Is.EqualTo(240.0));
        });
    }
}
