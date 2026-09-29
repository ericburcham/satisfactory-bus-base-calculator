using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Solving;
using static Satisfactory.Planner.Tests.Core.Solving.PlanningTestKit;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class SolveContextTests
{
    [Test]
    public void Record_HoldsItsParts()
    {
        ITierContext tier = Context(Steel().Build());

        SolveContext context = SolveContext(tier);

        Assert.Multiple(() =>
        {
            Assert.That(context.Tier, Is.SameAs(tier));
            Assert.That(context.Extractors, Is.SameAs(tier.Extractors));
            Assert.That(context.Existing, Is.SameAs(NoExistingCapacity.Instance));
            Assert.That(context.DisposalFinder, Is.TypeOf<FluidDisposalFinder>());
            Assert.That(context.Choice.For(Item(SteelIngot))!.Id.Value, Is.EqualTo("Recipe_IngotSteel_C"));
            Assert.That(context.Credit.NoCredit, Is.Empty);
        });
    }
}
