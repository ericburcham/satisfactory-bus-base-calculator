using System.Globalization;
using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Core.Tiers.Graph;
using Satisfactory.Planner.Reporting.IO;

namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>
/// Writes <c>tier_N.dot</c>, the Graphviz picture of a tier's new machines, their external inputs and the AWESOME Sink
/// (Python <c>write_tier</c> lines 877-908).
/// </summary>
/// <remarks>
/// <para>
/// Lines, in order: <c>digraph tier {</c>; the default node and edge style lines and the title line
/// (<c>{tier name}: {new machines} new machines</c>); each machine group in build order, followed by one edge per
/// ingredient in ingredient order; the external input nodes (graph insertion order); when there is a sink, the sink node
/// and one dashed red edge per sunk item (sink order), where an input node that only feeds the sink is written just
/// before its edge; <c>}</c>.
/// </para>
/// <para>
/// The Python writes the external input nodes by iterating a <c>set</c>, so its order of those lines is arbitrary; this
/// writer uses their first-use order. Every other line is in the Python's order.
/// </para>
/// <para>
/// When <see cref="GraphvizDotWriter{TNode}.Render"/> is called directly instead of through <see cref="Write"/>, there is
/// no tier to name: the title line is left out and the machine groups are written in <see cref="MachineGroupNode.Step"/>
/// order.
/// </para>
/// </remarks>
public sealed class TierDotWriter : GraphvizDotWriter<TierGraphNode>, ITierDotWriter
{
    private const string GraphName = "tier";
    private const string SinkColor = "\"#cc3333\"";
    private readonly IFileSystem _fileSystem;
    private readonly Lock _gate = new();
    private (TierResult Result, TierReportContext Context)? _current;

    /// <summary>Creates the writer.</summary>
    /// <param name="fileSystem">The file system the <c>.dot</c> file is written to.</param>
    /// <param name="newLine">The line terminator written after every line (the CLI passes <see cref="Environment.NewLine"/>).</param>
    public TierDotWriter(IFileSystem fileSystem, string newLine)
        : base(newLine)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public string Write(TierResult result, TierReportContext context)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(context);
        string content;
        lock (_gate)
        {
            _current = (result, context);
            try
            {
                content = Render(context.Graph, GraphName);
            }
            finally
            {
                _current = null;
            }
        }

        string path = Path.Combine(context.OutputFolder, string.Create(CultureInfo.InvariantCulture, $"tier_{result.Tier}.dot"));
        _fileSystem.WriteAllText(path, content);
        return path;
    }

    /// <inheritdoc />
    protected override IEnumerable<string> GraphAttributeStatements(DirectedAcyclicGraph<TierGraphNode> graph)
    {
        yield return "  rankdir=LR; node [shape=box, style=\"rounded,filled\", fontname=\"Helvetica\", fontsize=10, fillcolor=\"#ffffff\"];";
        yield return "  edge [color=\"#888888\", arrowsize=0.6];";
        if (CurrentFor(graph) is { } current)
        {
            string title = string.Create(
                CultureInfo.InvariantCulture,
                $"{TierNames.For(current.Result.Tier)}: {current.Context.Totals.NewMachines} new machines");
            yield return $"  labelloc=t; label={Quote(title)}; fontsize=18;";
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<string> BodyStatements(DirectedAcyclicGraph<TierGraphNode> graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        IEnumerable<MachineGroupNode> buildOrder = CurrentFor(graph)?.Context.BuildOrder
            ?? [.. graph.Nodes.OfType<MachineGroupNode>().OrderBy(m => m.Step)];
        foreach (MachineGroupNode machine in buildOrder)
        {
            yield return NodeStatement(machine);
            foreach (GraphEdge<TierGraphNode> edge in graph.IncomingEdges(machine))
            {
                yield return EdgeStatement(edge);
            }
        }

        // Python `ext`: the external inputs that feed a machine; an input that only feeds the sink is written later.
        var written = new HashSet<string>(StringComparer.Ordinal);
        foreach (ExternalInputNode input in graph.Nodes.OfType<ExternalInputNode>())
        {
            if (!FeedsOnlyTheSink(graph, input))
            {
                written.Add(input.Id);
                yield return NodeStatement(input);
            }
        }

        if (!graph.TryGetNode(SinkNode.SinkId, out TierGraphNode? sink))
        {
            yield break;
        }

        yield return NodeStatement(sink);
        foreach (GraphEdge<TierGraphNode> edge in graph.IncomingEdges(sink))
        {
            if (edge.From is ExternalInputNode input && written.Add(input.Id))
            {
                yield return NodeStatement(input);
            }

            yield return EdgeStatement(edge);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<KeyValuePair<string, string>> NodeAttributes(TierGraphNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        yield return new("label", Quote(node.Label));
        switch (node)
        {
            case MachineGroupNode { MakesNewItem: true }:
                yield return new("penwidth", "2.5");
                yield return new("color", "\"#2a7a2a\"");
                break;
            case ExternalInputNode:
                yield return new("shape", "ellipse");
                yield return new("fillcolor", "\"#eeeeee\"");
                break;
            case SinkNode:
                yield return new("shape", "doubleoctagon");
                yield return new("fillcolor", "\"#ffd6d6\"");
                break;
            default:
                break;
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<KeyValuePair<string, string>> EdgeAttributes(GraphEdge<TierGraphNode> edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (edge.To is not SinkNode)
        {
            yield break;
        }

        yield return new("color", SinkColor);
        yield return new("style", "dashed");
        yield return new("label", Quote(edge.Label ?? string.Empty));
        yield return new("fontcolor", SinkColor);
    }

    /// <summary>The tier being written by <see cref="Write"/>, when <paramref name="graph"/> is its graph.</summary>
    private (TierResult Result, TierReportContext Context)? CurrentFor(DirectedAcyclicGraph<TierGraphNode> graph) =>
        _current is { } current && ReferenceEquals(current.Context.Graph, graph) ? current : null;

    private static bool FeedsOnlyTheSink(DirectedAcyclicGraph<TierGraphNode> graph, ExternalInputNode input)
    {
        IReadOnlyList<GraphEdge<TierGraphNode>> outgoing = graph.OutgoingEdges(input);
        return outgoing.Count > 0 && outgoing.All(e => e.To is SinkNode);
    }
}
