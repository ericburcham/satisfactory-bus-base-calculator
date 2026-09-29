using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Reporting.Rendering;
using Satisfactory.Planner.Reporting.Writers;
using Satisfactory.Planner.Tests.TestSupport;
using static Satisfactory.Planner.Tests.Reporting.Writers.WriterScenario;

namespace Satisfactory.Planner.Tests.Reporting.Rendering;

[TestFixture]
public sealed class ReportGeneratorTests
{
    private List<string> _log = null!;
    private FakeReportTierContextFactory _contexts = null!;
    private FakeTierGraphBuilder _graphs = null!;
    private FakeTierBuildOrder _order = null!;
    private FakeTierTotalsCalculator _totals = null!;
    private FakeTierReportWriter _text = null!;
    private FakeTierReportWriter _csv = null!;
    private FakeTierDotWriter _dot = null!;
    private FakeGraphRenderer _renderer = null!;
    private FakeSummaryWriter _summary = null!;

    [SetUp]
    public void SetUp()
    {
        _log = [];
        _contexts = new FakeReportTierContextFactory(Data, _log);
        _graphs = new FakeTierGraphBuilder(_log);
        _order = new FakeTierBuildOrder(_log);
        _totals = new FakeTierTotalsCalculator(_log);
        _text = new FakeTierReportWriter("text", _log);
        _csv = new FakeTierReportWriter("csv", _log);
        _dot = new FakeTierDotWriter(_log);
        _renderer = new FakeGraphRenderer(_log);
        _summary = new FakeSummaryWriter(_log);
    }

    [Test]
    public void Generate_RunsEveryStepPerTierInOrder_ThenTheSummary()
    {
        TierResult[] results = [Minimal(), Rich() with { Tier = 1 }, Rich() with { Tier = 2 }];

        string summary = Generator().Generate(results, "out");

        Assert.Multiple(() =>
        {
            Assert.That(summary, Is.EqualTo("summary text\n"));
            Assert.That(_log, Is.EqualTo(ExpectedSequence.Of(
                "context:2",
                "graph:0", "order", "totals", "text:0", "csv:0", "dot:0", "render:out/fake_tier_0.dot",
                "graph:1", "order", "totals", "text:1", "csv:1", "dot:1", "render:out/fake_tier_1.dot",
                "graph:2", "order", "totals", "text:2", "csv:2", "dot:2", "render:out/fake_tier_2.dot",
                "summary:out")));
        });
    }

    [Test]
    public void Generate_BuildsEveryGraphWithTheFinalTiersExtractors()
    {
        TierResult[] results = [Minimal(), Rich()];

        Generator().Generate(results, "out");

        Assert.Multiple(() =>
        {
            Assert.That(_contexts.ExtractorsByTier.Keys, Is.EqualTo(ExpectedSequence.Of(5)));
            Assert.That(_graphs.Calls.Select(c => c.Extractors), Is.All.SameAs(_contexts.ExtractorsByTier[5]));
            Assert.That(_graphs.Calls.Select(c => c.Data), Is.All.SameAs(Data));
            Assert.That(_graphs.Calls.Select(c => c.Result), Is.EqualTo(results));
        });
    }

    [Test]
    public void Generate_PassesTheSameContextToEveryWriter()
    {
        TierResult[] results = [Minimal(), Rich()];

        Generator().Generate(results, "reports");

        Assert.Multiple(() =>
        {
            for (int tier = 0; tier < results.Length; tier++)
            {
                TierReportContext context = _text.Calls[tier].Context;
                Assert.That(_text.Calls[tier].Result, Is.SameAs(results[tier]));
                Assert.That(context.OutputFolder, Is.EqualTo("reports"));
                Assert.That(context.Data, Is.SameAs(Data));
                Assert.That(context.Graph, Is.SameAs(_graphs.Calls[tier].Graph));
                Assert.That(_order.Calls[tier].Graph, Is.SameAs(_graphs.Calls[tier].Graph));
                Assert.That(context.BuildOrder, Is.SameAs(_order.Calls[tier].Order));
                Assert.That(_totals.Calls[tier].BuildOrder, Is.SameAs(_order.Calls[tier].Order));
                Assert.That(context.Totals, Is.SameAs(_totals.Calls[tier].Totals));
                Assert.That(_csv.Calls[tier].Context, Is.SameAs(context));
                Assert.That(_dot.Calls[tier].Context, Is.SameAs(context));
                Assert.That(_dot.Calls[tier].Result, Is.SameAs(results[tier]));
            }
        });
    }

    [Test]
    public void Generate_PassesEachTierWithItsTotalsToTheSummary()
    {
        TierResult[] results = [Minimal(), Rich()];

        Generator().Generate(results, "out");

        Assert.Multiple(() =>
        {
            Assert.That(_summary.Calls, Has.Count.EqualTo(1));
            Assert.That(_summary.Calls[0].OutputFolder, Is.EqualTo("out"));
            Assert.That(
                _summary.Calls[0].Tiers,
                Is.EqualTo(new[] { (results[0], new TierTotals(1, 10, 100)), (results[1], new TierTotals(2, 20, 200)) }));
            Assert.That(_renderer.Paths, Is.EqualTo(ExpectedSequence.Of("out/fake_tier_0.dot", "out/fake_tier_5.dot")));
        });
    }

    [Test]
    public void Generate_NoResults_WritesOnlyTheSummary()
    {
        string summary = Generator().Generate([], "out");

        Assert.Multiple(() =>
        {
            Assert.That(summary, Is.EqualTo("summary text\n"));
            Assert.That(_log, Is.EqualTo(ExpectedSequence.Of("summary:out")));
            Assert.That(_summary.Calls[0].Tiers, Is.Empty);
        });
    }

    [Test]
    public void Generate_WithRealWriters_WritesEveryFileOfEachTier()
    {
        // The real writers with a fake graph builder that returns the scenario graph.
        var fileSystem = new InMemoryFileSystem();
        var power = new Satisfactory.Planner.Core.Power.PowerCalculator(Data);
        var generator = new ReportGenerator(
            Data,
            _contexts,
            new ScenarioGraphBuilder(),
            new StepBuildOrder(),
            new TierTotalsCalculator(power),
            [new TierOrderTextWriter(fileSystem, "\n"), new TierOrderCsvWriter(fileSystem, new Satisfactory.Planner.Reporting.Formatting.CsvFormatter(), power)],
            new TierDotWriter(fileSystem, "\n"),
            _renderer,
            new SummaryWriter(fileSystem, "\n"));

        string summary = generator.Generate([Minimal()], "out");

        string[] names = ["tier_0_order.txt", "tier_0_order.csv", "tier_0.dot", "summary.txt"];
        Assert.Multiple(() =>
        {
            Assert.That(fileSystem.WriteOrder, Is.EqualTo(names.Select(n => InMemoryFileSystem.Normalize(Path.Combine("out", n)))));
            Assert.That(_renderer.Paths, Is.EqualTo(ExpectedSequence.Of(Path.Combine("out", "tier_0.dot"))));
            Assert.That(summary, Is.EqualTo(fileSystem.ReadAllText(Path.Combine("out", "summary.txt"))));
            Assert.That(
                summary,
                Does.EndWith("Tier 0 - Onboarding            1            1     0     0.0          4          0 |              1          4          0             0\n"));
            Assert.That(fileSystem.ReadAllText(Path.Combine("out", "tier_0.dot")), Does.Contain("label=\"Tier 0 - Onboarding: 1 new machines\""));
        });
    }

    [Test]
    public void Constructor_RejectsNulls()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new ReportGenerator(null!, _contexts, _graphs, _order, _totals, [_text], _dot, _renderer, _summary), Throws.ArgumentNullException);
            Assert.That(() => new ReportGenerator(Data, null!, _graphs, _order, _totals, [_text], _dot, _renderer, _summary), Throws.ArgumentNullException);
            Assert.That(() => new ReportGenerator(Data, _contexts, null!, _order, _totals, [_text], _dot, _renderer, _summary), Throws.ArgumentNullException);
            Assert.That(() => new ReportGenerator(Data, _contexts, _graphs, null!, _totals, [_text], _dot, _renderer, _summary), Throws.ArgumentNullException);
            Assert.That(() => new ReportGenerator(Data, _contexts, _graphs, _order, null!, [_text], _dot, _renderer, _summary), Throws.ArgumentNullException);
            Assert.That(() => new ReportGenerator(Data, _contexts, _graphs, _order, _totals, null!, _dot, _renderer, _summary), Throws.ArgumentNullException);
            Assert.That(() => new ReportGenerator(Data, _contexts, _graphs, _order, _totals, [null!], _dot, _renderer, _summary), Throws.ArgumentException);
            Assert.That(() => new ReportGenerator(Data, _contexts, _graphs, _order, _totals, [_text], null!, _renderer, _summary), Throws.ArgumentNullException);
            Assert.That(() => new ReportGenerator(Data, _contexts, _graphs, _order, _totals, [_text], _dot, null!, _summary), Throws.ArgumentNullException);
            Assert.That(() => new ReportGenerator(Data, _contexts, _graphs, _order, _totals, [_text], _dot, _renderer, null!), Throws.ArgumentNullException);
            Assert.That(() => Generator().Generate(null!, "out"), Throws.ArgumentNullException);
            Assert.That(() => Generator().Generate([], null!), Throws.ArgumentNullException);
        });
    }

    private ReportGenerator Generator() =>
        new(Data, _contexts, _graphs, _order, _totals, [_text, _csv], _dot, _renderer, _summary);

    private sealed class ScenarioGraphBuilder : Satisfactory.Planner.Core.Tiers.ITierGraphBuilder
    {
        public Satisfactory.Graphs.DirectedAcyclicGraph<Satisfactory.Planner.Core.Tiers.Graph.TierGraphNode> Build(
            TierResult result, Satisfactory.Planner.Core.Model.GameData data, Satisfactory.Planner.Core.Extraction.IExtractorCatalog extractors) =>
            GraphOf(result, [.. result.NewMachines.Keys.Select(k => k.Value)]);
    }

    private sealed class StepBuildOrder : ITierBuildOrder
    {
        public IReadOnlyList<Satisfactory.Planner.Core.Tiers.Graph.MachineGroupNode> Order(
            Satisfactory.Graphs.DirectedAcyclicGraph<Satisfactory.Planner.Core.Tiers.Graph.TierGraphNode> graph) => BuildOrderOf(graph);
    }
}
