using Satisfactory.Planner.Cli;
using Satisfactory.Planner.Graphviz;

namespace Satisfactory.Planner.Tests.Cli;

[TestFixture]
public sealed class GraphvizRendererAdapterTests
{
    [Test]
    public void Render_Available_RendersSvgNextToTheDotFile()
    {
        var renderer = new RecordingDotRenderer(isAvailable: true);
        var adapter = new GraphvizRendererAdapter(renderer);
        string dotPath = Path.Combine("tiers", "tier_3.dot");

        adapter.Render(dotPath);

        Assert.That(renderer.Calls, Is.EqualTo(new[] { (dotPath, Path.Combine("tiers", "tier_3.svg")) }));
    }

    [Test]
    public void Render_Available_UsesPathChangeExtension()
    {
        var renderer = new RecordingDotRenderer(isAvailable: true);
        var adapter = new GraphvizRendererAdapter(renderer);

        adapter.Render("out.dir/graph");
        adapter.Render("a.b.dot");

        Assert.That(renderer.Calls, Is.EqualTo(new[] { ("out.dir/graph", "out.dir/graph.svg"), ("a.b.dot", "a.b.svg") }));
    }

    [Test]
    public void Render_Unavailable_DoesNothing()
    {
        var renderer = new RecordingDotRenderer(isAvailable: false);
        var adapter = new GraphvizRendererAdapter(renderer);

        adapter.Render(Path.Combine("tiers", "tier_0.dot"));

        Assert.That(renderer.Calls, Is.Empty);
    }

    [Test]
    public void Render_WithNullDotRenderer_DoesNothing()
    {
        var adapter = new GraphvizRendererAdapter(new NullDotRenderer());

        Assert.That(() => adapter.Render("does-not-exist.dot"), Throws.Nothing);
    }

    [Test]
    public void Render_RendererFails_PropagatesTheException()
    {
        var renderer = new RecordingDotRenderer(isAvailable: true) { Failure = new GraphvizRenderException("boom") };
        var adapter = new GraphvizRendererAdapter(renderer);

        Assert.That(() => adapter.Render("tier_1.dot"), Throws.TypeOf<GraphvizRenderException>().With.Message.EqualTo("boom"));
    }

    [Test]
    public void Arguments_Validated()
    {
        Assert.That(() => new GraphvizRendererAdapter(null!), Throws.ArgumentNullException);

        var adapter = new GraphvizRendererAdapter(new RecordingDotRenderer(isAvailable: true));
        Assert.That(() => adapter.Render(null!), Throws.ArgumentNullException);
        Assert.That(() => adapter.Render(string.Empty), Throws.ArgumentException);
    }

    private sealed class RecordingDotRenderer(bool isAvailable) : IDotRenderer
    {
        public bool IsAvailable { get; } = isAvailable;

        public List<(string DotPath, string SvgPath)> Calls { get; } = [];

        public Exception? Failure { get; init; }

        public void RenderSvg(string dotPath, string svgPath)
        {
            Calls.Add((dotPath, svgPath));
            if (Failure is not null)
            {
                throw Failure;
            }
        }
    }
}
