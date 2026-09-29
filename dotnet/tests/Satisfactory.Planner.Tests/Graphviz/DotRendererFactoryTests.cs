using Satisfactory.Planner.Graphviz;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Graphviz;

[TestFixture]
public sealed class DotRendererFactoryTests
{
    private static readonly string[] OnlyDot = ["dot"];

    [Test]
    public void Create_DotNotFound_ReturnsNullDotRenderer()
    {
        var locator = new FakeExecutableLocator();
        var factory = new DotRendererFactory(locator, new FakeProcessRunner());

        IDotRenderer renderer = factory.Create();

        Assert.Multiple(() =>
        {
            Assert.That(renderer, Is.InstanceOf<NullDotRenderer>());
            Assert.That(renderer.IsAvailable, Is.False);
            Assert.That(locator.Requests, Is.EqualTo(OnlyDot));
        });
    }

    [Test]
    public void Create_DotFound_ReturnsGraphvizDotRendererThatRunsTheLocatedExecutable()
    {
        var locator = new FakeExecutableLocator().WithExecutable("dot", "/opt/graphviz/bin/dot");
        var runner = new FakeProcessRunner();
        var factory = new DotRendererFactory(locator, runner);

        IDotRenderer renderer = factory.Create();
        renderer.RenderSvg("tier_0.dot", "tier_0.svg");

        Assert.Multiple(() =>
        {
            Assert.That(renderer, Is.InstanceOf<GraphvizDotRenderer>());
            Assert.That(renderer.IsAvailable, Is.True);
            Assert.That(runner.Calls, Has.Count.EqualTo(1));
            Assert.That(runner.Calls[0].FileName, Is.EqualTo("/opt/graphviz/bin/dot"));
        });
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new DotRendererFactory(null!, new FakeProcessRunner()), Throws.ArgumentNullException);
            Assert.That(() => new DotRendererFactory(new FakeExecutableLocator(), null!), Throws.ArgumentNullException);
        });
    }
}
