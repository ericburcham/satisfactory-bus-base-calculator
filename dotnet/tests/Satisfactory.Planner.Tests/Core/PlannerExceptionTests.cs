using Satisfactory.Planner.Core;

namespace Satisfactory.Planner.Tests.Core;

[TestFixture]
public sealed class PlannerExceptionTests
{
    [Test]
    public void MessageConstructor_KeepsMessageExactly()
    {
        var e = new PlannerException("Iron Plate is not machine-producible");

        Assert.That(e.Message, Is.EqualTo("Iron Plate is not machine-producible"));
    }

    [Test]
    public void InnerExceptionConstructor_KeepsBoth()
    {
        var inner = new InvalidOperationException("inner");

        var e = new PlannerException("outer", inner);

        Assert.Multiple(() =>
        {
            Assert.That(e.Message, Is.EqualTo("outer"));
            Assert.That(e.InnerException, Is.SameAs(inner));
        });
    }

    [Test]
    public void DefaultConstructor_HasAMessage()
    {
        Assert.That(new PlannerException().Message, Is.Not.Empty);
    }
}
