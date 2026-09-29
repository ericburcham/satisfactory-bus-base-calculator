using Satisfactory.Graphs;

namespace Satisfactory.Planner.Tests.Graphs;

[TestFixture]
public sealed class GraphEdgeTests
{
    [Test]
    public void Constructor_SetsProperties()
    {
        var a = new TestNode("a");
        var b = new TestNode("b");

        var edge = new GraphEdge<TestNode>(a, b, "label");

        Assert.Multiple(() =>
        {
            Assert.That(edge.From, Is.SameAs(a));
            Assert.That(edge.To, Is.SameAs(b));
            Assert.That(edge.Label, Is.EqualTo("label"));
        });
    }

    [Test]
    public void Label_DefaultsToNull()
    {
        var edge = new GraphEdge<TestNode>(new TestNode("a"), new TestNode("b"));

        Assert.That(edge.Label, Is.Null);
    }

    [Test]
    public void Equality_IsByValue()
    {
        var a = new TestNode("a");
        var b = new TestNode("b");
        var first = new GraphEdge<TestNode>(a, b, "x");
        var second = new GraphEdge<TestNode>(a, b, "x");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(new GraphEdge<TestNode>(a, b, "x"), Is.Not.EqualTo(new GraphEdge<TestNode>(a, b, "y")));
            Assert.That(new GraphEdge<TestNode>(a, b), Is.Not.EqualTo(new GraphEdge<TestNode>(b, a)));
        });
    }

    [Test]
    public void Constructor_NullEndpoint_Throws()
    {
        var a = new TestNode("a");

        Assert.Multiple(() =>
        {
            Assert.That(() => new GraphEdge<TestNode>(null!, a), Throws.ArgumentNullException);
            Assert.That(() => new GraphEdge<TestNode>(a, null!), Throws.ArgumentNullException);
        });
    }
}
