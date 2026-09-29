using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Solving;
using static Satisfactory.Planner.Tests.Core.Solving.PlanningTestKit;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class FluidDisposalFinderFactoryTests
{
    [Test]
    public void Create_ReturnsAFinderForTheTierAndCosts()
    {
        ITierContext context = Context(PlasticWithResidue()
            .WithItem(PetroleumCoke, "Petroleum Coke")
            .WithRecipe("Recipe_PetroleumCoke_C", "Petroleum Coke", Refinery, 6, [(HeavyOilResidue, 4)], [(PetroleumCoke, 12)])
            .Build());

        IFluidDisposalFinder finder = new FluidDisposalFinderFactory().Create(context, Costs(context));

        Assert.Multiple(() =>
        {
            Assert.That(finder, Is.TypeOf<FluidDisposalFinder>());
            Assert.That(finder.Find(Item(HeavyOilResidue)).Id.Value, Is.EqualTo("Recipe_PetroleumCoke_C"));
        });
    }
}
