using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Solving;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class NoExistingCapacityTests
{
    [Test]
    public void Instance_HasNoMachines()
    {
        NoExistingCapacity none = NoExistingCapacity.Instance;

        Assert.Multiple(() =>
        {
            Assert.That(none, Is.SameAs(NoExistingCapacity.Instance));
            Assert.That(none.Machines, Is.Empty);
            Assert.That(none.MachinesOf(new RecipeId("Recipe_IronRod_C")), Is.Zero);
        });
    }
}
