using Satisfactory.Planner.Graphviz;

namespace Satisfactory.Planner.Tests.Graphviz;

[TestFixture]
public sealed class NullDotRendererTests
{
    [Test]
    public void IsAvailable_IsFalse()
    {
        Assert.That(new NullDotRenderer().IsAvailable, Is.False);
    }

    [Test]
    public void RenderSvg_DoesNothing()
    {
        string svgPath = Path.Combine(Path.GetTempPath(), "null-renderer-" + Guid.NewGuid().ToString("N") + ".svg");

        Assert.That(() => new NullDotRenderer().RenderSvg("does-not-exist.dot", svgPath), Throws.Nothing);
        Assert.That(File.Exists(svgPath), Is.False);
    }
}
