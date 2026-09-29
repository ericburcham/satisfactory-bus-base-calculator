using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Tiers.Graph;
using Satisfactory.Planner.Reporting.Writers;
using static Satisfactory.Planner.Tests.Reporting.Writers.WriterScenario;

namespace Satisfactory.Planner.Tests.Reporting.Writers;

[TestFixture]
public sealed class TierReportContextTests
{
    [Test]
    public void Constructor_KeepsItsValues()
    {
        var graph = new DirectedAcyclicGraph<TierGraphNode>();
        MachineGroupNode[] order = [];
        var totals = new TierTotals(1, 2, 3);

        var context = new TierReportContext("folder", Data, graph, order, totals);

        Assert.Multiple(() =>
        {
            Assert.That(context.OutputFolder, Is.EqualTo("folder"));
            Assert.That(context.Data, Is.SameAs(Data));
            Assert.That(context.Graph, Is.SameAs(graph));
            Assert.That(context.BuildOrder, Is.SameAs(order));
            Assert.That(context.Totals, Is.SameAs(totals));
        });
    }
}
