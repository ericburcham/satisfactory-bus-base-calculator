using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Core.Tiers.Graph;
using Satisfactory.Planner.Reporting.Writers;

namespace Satisfactory.Planner.Reporting.Rendering;

/// <summary>
/// Writes the reports of a planning run (the Python <c>main</c> loop over <c>write_tier</c>, then the summary).
/// </summary>
/// <remarks>
/// For each tier, in order: build the tier graph with the <b>final</b> tier's extractor catalog (the Python writes
/// every tier after the last one was planned, so its <c>extractor_for</c> sees the last tier's locks), compute the build
/// order and totals, run every <see cref="ITierReportWriter"/> in the given order, write the <c>.dot</c> file and render
/// it. Then write the summary of all tiers.
/// </remarks>
public sealed class ReportGenerator : IReportGenerator
{
    private readonly GameData _data;
    private readonly ITierContextFactory _contexts;
    private readonly ITierGraphBuilder _graphs;
    private readonly ITierBuildOrder _buildOrder;
    private readonly ITierTotalsCalculator _totals;
    private readonly IReadOnlyList<ITierReportWriter> _writers;
    private readonly ITierDotWriter _dotWriter;
    private readonly IGraphRenderer _renderer;
    private readonly ISummaryWriter _summary;

    /// <summary>Creates the generator.</summary>
    /// <param name="data">The game data.</param>
    /// <param name="contexts">Creates the final tier's context, for its extractor catalog.</param>
    /// <param name="graphs">Builds each tier's graph.</param>
    /// <param name="buildOrder">Orders each tier's machine groups.</param>
    /// <param name="totals">Sums each tier's machines and power.</param>
    /// <param name="writers">The per-tier report writers, run in this order (the CLI passes text, then CSV).</param>
    /// <param name="dotWriter">Writes each tier's <c>.dot</c> file.</param>
    /// <param name="renderer">Renders each written <c>.dot</c> file.</param>
    /// <param name="summary">Writes the summary.</param>
    public ReportGenerator(
        GameData data,
        ITierContextFactory contexts,
        ITierGraphBuilder graphs,
        ITierBuildOrder buildOrder,
        ITierTotalsCalculator totals,
        IEnumerable<ITierReportWriter> writers,
        ITierDotWriter dotWriter,
        IGraphRenderer renderer,
        ISummaryWriter summary)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(contexts);
        ArgumentNullException.ThrowIfNull(graphs);
        ArgumentNullException.ThrowIfNull(buildOrder);
        ArgumentNullException.ThrowIfNull(totals);
        ArgumentNullException.ThrowIfNull(writers);
        ArgumentNullException.ThrowIfNull(dotWriter);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(summary);
        _data = data;
        _contexts = contexts;
        _graphs = graphs;
        _buildOrder = buildOrder;
        _totals = totals;
        _writers = writers.ToList().AsReadOnly();
        if (_writers.Any(w => w is null))
        {
            throw new ArgumentException("The writers contain null.", nameof(writers));
        }

        _dotWriter = dotWriter;
        _renderer = renderer;
        _summary = summary;
    }

    /// <inheritdoc />
    public string Generate(IReadOnlyList<TierResult> results, string outputFolder)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(outputFolder);
        var tiers = new List<(TierResult Result, TierTotals Totals)>(results.Count);
        if (results.Count > 0)
        {
            IExtractorCatalog extractors = _contexts.Create(results[^1].Tier).Extractors;
            foreach (TierResult result in results)
            {
                ArgumentNullException.ThrowIfNull(result, nameof(results));
                tiers.Add((result, WriteTier(result, outputFolder, extractors)));
            }
        }

        return _summary.Write(tiers, outputFolder);
    }

    private TierTotals WriteTier(TierResult result, string outputFolder, IExtractorCatalog extractors)
    {
        DirectedAcyclicGraph<TierGraphNode> graph = _graphs.Build(result, _data, extractors);
        IReadOnlyList<MachineGroupNode> order = _buildOrder.Order(graph);
        TierTotals totals = _totals.Calculate(order);
        var context = new TierReportContext(outputFolder, _data, graph, order, totals);
        foreach (ITierReportWriter writer in _writers)
        {
            writer.Write(result, context);
        }

        string dotPath = _dotWriter.Write(result, context);
        _renderer.Render(dotPath);
        return totals;
    }
}
