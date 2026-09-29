using Satisfactory.Planner.Core;
using Satisfactory.Planner.Core.Data;

namespace Satisfactory.Planner.Tests.Core.Data;

[TestFixture]
public sealed class GameDataFormatExceptionTests
{
    [Test]
    public void IsAPlannerException()
    {
        Assert.That(new GameDataFormatException("bad"), Is.InstanceOf<PlannerException>());
    }

    [Test]
    public void Constructors_KeepMessageAndInner()
    {
        var inner = new FormatException("x");
        var e = new GameDataFormatException("Missing field", inner);

        Assert.Multiple(() =>
        {
            Assert.That(e.Message, Is.EqualTo("Missing field"));
            Assert.That(e.InnerException, Is.SameAs(inner));
            Assert.That(new GameDataFormatException().Message, Is.Not.Empty);
        });
    }
}
