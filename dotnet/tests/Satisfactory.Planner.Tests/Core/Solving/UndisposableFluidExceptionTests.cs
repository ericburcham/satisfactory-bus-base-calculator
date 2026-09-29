using Satisfactory.Planner.Core;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Solving;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class UndisposableFluidExceptionTests
{
    [Test]
    public void FluidConstructor_NamesTheFluid()
    {
        var fluid = new ItemId("Desc_HeavyOilResidue_C");

        var exception = new UndisposableFluidException(fluid);

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.InstanceOf<PlannerException>());
            Assert.That(exception.Fluid, Is.EqualTo(fluid));
            Assert.That(exception.Message, Does.Contain("Desc_HeavyOilResidue_C"));
        });
    }

    [Test]
    public void StandardConstructors_HaveAnEmptyFluid()
    {
        var inner = new InvalidOperationException("inner");

        Assert.Multiple(() =>
        {
            Assert.That(new UndisposableFluidException().Fluid.Value, Is.Empty);
            Assert.That(new UndisposableFluidException().Message, Is.Not.Empty);
            Assert.That(new UndisposableFluidException("m").Message, Is.EqualTo("m"));
            Assert.That(new UndisposableFluidException("m").Fluid.Value, Is.Empty);
            Assert.That(new UndisposableFluidException("m", inner).InnerException, Is.SameAs(inner));
            Assert.That(new UndisposableFluidException("m", inner).Fluid.Value, Is.Empty);
        });
    }
}
