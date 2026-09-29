using Satisfactory.Planner.Reporting.Writers;

namespace Satisfactory.Planner.Tests.Reporting.Writers;

[TestFixture]
public sealed class TierTotalsTests
{
    [Test]
    public void Record_HasValueEquality()
    {
        var totals = new TierTotals(402, 13162.0, 30000.0);

        Assert.Multiple(() =>
        {
            Assert.That(totals.NewMachines, Is.EqualTo(402));
            Assert.That(totals.PowerUsedMw, Is.EqualTo(13162.0));
            Assert.That(totals.PowerGeneratedMw, Is.EqualTo(30000.0));
            Assert.That(totals, Is.EqualTo(new TierTotals(402, 13162.0, 30000.0)));
            Assert.That(totals, Is.Not.EqualTo(new TierTotals(402, 13162.0, 0.0)));
        });
    }
}
