using Satisfactory.Planner.Core.Solving;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class ProductionStateTests
{
    [Test]
    public void NewState_IsEmpty()
    {
        var state = new ProductionState();

        Assert.Multiple(() =>
        {
            Assert.That(state.Runs, Is.Empty);
            Assert.That(state.Recipes, Is.Empty);
            Assert.That(state.Extract, Is.Empty);
            Assert.That(state.Demand, Is.Empty);
        });
    }
}
