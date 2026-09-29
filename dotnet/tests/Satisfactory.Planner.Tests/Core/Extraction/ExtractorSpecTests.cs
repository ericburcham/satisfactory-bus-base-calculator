using Satisfactory.Planner.Core.Extraction;

namespace Satisfactory.Planner.Tests.Core.Extraction;

[TestFixture]
public sealed class ExtractorSpecTests
{
    [Test]
    public void Properties_HoldConstructorValues()
    {
        var spec = new ExtractorSpec("Water Extractor", 120, 20);

        Assert.Multiple(() =>
        {
            Assert.That(spec.Name, Is.EqualTo("Water Extractor"));
            Assert.That(spec.RatePerMinute, Is.EqualTo(120));
            Assert.That(spec.PowerMw, Is.EqualTo(20));
        });
    }
}
