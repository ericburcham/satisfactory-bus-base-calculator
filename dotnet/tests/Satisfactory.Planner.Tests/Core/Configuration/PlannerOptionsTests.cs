using Satisfactory.Planner.Core.Configuration;

namespace Satisfactory.Planner.Tests.Core.Configuration;

[TestFixture]
public sealed class PlannerOptionsTests
{
    [Test]
    public void Properties_HoldConstructorValues()
    {
        var options = new PlannerOptions("data/x.json", "out", MinerTier.Mk3, NodePurity.Pure, IncludeIngots: true);

        Assert.Multiple(() =>
        {
            Assert.That(options.DataPath, Is.EqualTo("data/x.json"));
            Assert.That(options.OutputFolder, Is.EqualTo("out"));
            Assert.That(options.MinerTier, Is.EqualTo(MinerTier.Mk3));
            Assert.That(options.NodePurity, Is.EqualTo(NodePurity.Pure));
            Assert.That(options.IncludeIngots, Is.True);
            Assert.That(options, Is.EqualTo(options with { }));
        });
    }
}
