using Satisfactory.Graphs;
using Satisfactory.Planner.Reporting.Writers;
using Satisfactory.Planner.Tests.Graphs;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Reporting.Writers;

[TestFixture]
public sealed class GraphvizDotWriterTests
{
    private static DirectedAcyclicGraph<TestNode> Sample()
    {
        var graph = new DirectedAcyclicGraph<TestNode>();
        var a = new TestNode("a");
        var b = new TestNode("b \"quoted\"");
        var c = new TestNode("c");
        graph.AddEdge(a, b);
        graph.AddEdge(b, c, "two\nlines");
        graph.AddEdge(a, c);
        return graph;
    }

    [TestCase("\n")]
    [TestCase("\r\n")]
    public void Render_Default_WritesNodesThenEdges_WithLabelsOnly(string newLine)
    {
        string text = new GraphvizDotWriter<TestNode>(newLine).Render(Sample(), "g");

        Assert.That(text, Is.EqualTo(string.Concat(
            ExpectedSequence.Of(
                "digraph g {",
                "  \"a\";",
                "  \"b \\\"quoted\\\"\";",
                "  \"c\";",
                "  \"a\" -> \"b \\\"quoted\\\"\";",
                "  \"b \\\"quoted\\\"\" -> \"c\" [label=\"two\\nlines\"];",
                "  \"a\" -> \"c\";",
                "}").Select(l => l + newLine))));
    }

    [Test]
    public void Render_EmptyGraph_WritesOnlyTheFrame()
    {
        string text = new GraphvizDotWriter<TestNode>("\n").Render(new DirectedAcyclicGraph<TestNode>(), "empty");

        Assert.That(text, Is.EqualTo("digraph empty {\n}\n"));
    }

    [Test]
    public void Render_UsesTheOverriddenHooks()
    {
        string text = new CustomWriter().Render(Sample(), "custom");

        Assert.That(text, Is.EqualTo(string.Concat(
            ExpectedSequence.Of(
                "digraph custom {",
                "  rankdir=LR;",
                "  \"c\" [shape=box, label=\"C!\"];",
                "  \"a\" -> \"c\" [weight=3];",
                "}").Select(l => l + "\n"))));
    }

    [Test]
    public void Quote_EscapesQuotesAndNewlinesOnly()
    {
        Assert.That(CustomWriter.QuoteText("a\"b\nc\\d"), Is.EqualTo("\"a\\\"b\\nc\\d\""));
    }

    [Test]
    public void Render_RejectsBadArguments()
    {
        var writer = new GraphvizDotWriter<TestNode>("\n");

        Assert.Multiple(() =>
        {
            Assert.That(() => new GraphvizDotWriter<TestNode>(null!), Throws.ArgumentNullException);
            Assert.That(() => writer.Render(null!, "g"), Throws.ArgumentNullException);
            Assert.That(() => writer.Render(Sample(), string.Empty), Throws.ArgumentException);
        });
    }

    /// <summary>A subclass that only writes node c and the edge a -> c, with custom attributes.</summary>
    private sealed class CustomWriter : GraphvizDotWriter<TestNode>
    {
        public CustomWriter()
            : base("\n")
        {
        }

        public static string QuoteText(string text) => Quote(text);

        protected override IEnumerable<string> GraphAttributeStatements(DirectedAcyclicGraph<TestNode> graph) => ["  rankdir=LR;"];

        protected override IEnumerable<KeyValuePair<string, string>> NodeAttributes(TestNode node) =>
            [new("shape", "box"), new("label", Quote("C!"))];

        protected override IEnumerable<KeyValuePair<string, string>> EdgeAttributes(GraphEdge<TestNode> edge) =>
            [new("weight", "3")];

        protected override IEnumerable<string> BodyStatements(DirectedAcyclicGraph<TestNode> graph)
        {
            yield return NodeStatement(graph.GetNode("c"));
            yield return EdgeStatement(graph.Edges[2]);
        }
    }
}
