using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Tests.Core.Tiers.Graph;

[TestFixture]
public sealed class SinkNodeTests
{
    [Test]
    public void Node_IsTheAwesomeSink()
    {
        var node = new SinkNode();

        Assert.Multiple(() =>
        {
            Assert.That(node.Id, Is.EqualTo("SINK"));
            Assert.That(SinkNode.SinkId, Is.EqualTo("SINK"));
            Assert.That(node.Kind, Is.EqualTo(TierGraphNodeKind.Sink));
            Assert.That(node.Label, Is.EqualTo("AWESOME Sink"));
        });
    }
}
