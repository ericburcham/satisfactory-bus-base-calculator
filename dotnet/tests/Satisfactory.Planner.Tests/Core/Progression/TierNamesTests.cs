using Satisfactory.Planner.Core.Progression;

namespace Satisfactory.Planner.Tests.Core.Progression;

[TestFixture]
public sealed class TierNamesTests
{
    [TestCase(0, "Tier 0 - Onboarding")]
    [TestCase(1, "Tier 1")]
    [TestCase(5, "Tier 5")]
    [TestCase(9, "Tier 9")]
    [TestCase(10, "Tier 10")]
    [TestCase(-1, "Tier -1")]
    public void For_UsesTheTableOrFallsBack(int tier, string expected)
    {
        Assert.That(TierNames.For(tier), Is.EqualTo(expected));
    }
}
