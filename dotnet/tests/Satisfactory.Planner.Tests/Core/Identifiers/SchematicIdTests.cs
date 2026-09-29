using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Identifiers;

[TestFixture]
public sealed class SchematicIdTests
{
    [Test]
    public void Equality_IsOrdinalOnValue()
    {
        var left = new SchematicId("Desc_Wire_C");
        var right = new SchematicId(string.Concat("Desc_", "Wire_C"));   // equal text, different string instance
        var other = new SchematicId("desc_wire_c");

        Assert.Multiple(() =>
        {
            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
            Assert.That(left != other, Is.True);
            Assert.That(left, Is.Not.EqualTo(other));
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        });
    }

    [Test]
    public void CompareTo_IsOrdinal()
    {
        // Ordinal: upper case sorts before lower case, '_' (0x5F) sorts after upper-case letters.
        var ids = new[] { new SchematicId("b"), new SchematicId("B"), new SchematicId("A_"), new SchematicId("AB"), new SchematicId("a") };

        SchematicId[] sorted = [.. ids.Order()];

        Assert.That(sorted.Select(i => i.Value), Is.EqualTo(ExpectedSequence.Of("AB", "A_", "B", "a", "b")));
    }

    [Test]
    public void ComparisonOperators_FollowCompareTo()
    {
        var a = new SchematicId("Desc_A_C");
        var b = new SchematicId("Desc_B_C");
        Assert.Multiple(() =>
        {
            Assert.That(a < b, Is.True);
            Assert.That(a <= b, Is.True);
            Assert.That(b > a, Is.True);
            Assert.That(b >= a, Is.True);
            Assert.That(a >= new SchematicId("Desc_A_C"), Is.True);
            Assert.That(a.CompareTo(new SchematicId("Desc_A_C")), Is.Zero);
        });
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        Assert.That(new SchematicId("Desc_IronPlate_C").ToString(), Is.EqualTo("Desc_IronPlate_C"));
    }

    [Test]
    public void Constructor_RejectsNull()
    {
        Assert.That(() => new SchematicId(null!), Throws.ArgumentNullException);
    }
}
