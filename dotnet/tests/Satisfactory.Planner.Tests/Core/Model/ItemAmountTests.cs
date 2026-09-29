using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Tests.Core.Model;

[TestFixture]
public sealed class ItemAmountTests
{
    [Test]
    public void Equality_IsByValue()
    {
        var left = new ItemAmount(new ItemId("Desc_Wire_C"), 2);
        var right = new ItemAmount(new ItemId("Desc_Wire_C"), 2.0);

        Assert.That(left, Is.EqualTo(right));
    }
}
