using System.Text;
using Satisfactory.Graphs;

namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>
/// Renders a <see cref="DirectedAcyclicGraph{TNode}"/> as Graphviz DOT text. Subclasses customise the output through
/// the virtual hooks (Template Method): graph-level statements, node and edge attributes, and the body statements.
/// </summary>
/// <typeparam name="TNode">The node type.</typeparam>
/// <remarks>
/// The output is <c>digraph {name} {</c>, the <see cref="GraphAttributeStatements"/>, the
/// <see cref="BodyStatements"/> and <c>}</c>, each on its own line, joined by the configured line terminator, with a
/// trailing terminator. Every statement is one complete line including its indentation. Node statements look like
/// <c>  "id" [k=v, k=v];</c> and edge statements like <c>  "a" -> "b";</c> (the brackets are left out when there are
/// no attributes).
/// </remarks>
public class GraphvizDotWriter<TNode>
    where TNode : IGraphNode
{
    private const string Indent = "  ";
    private readonly string _newLine;

    /// <summary>Creates the writer.</summary>
    /// <param name="newLine">The line terminator written after every line.</param>
    public GraphvizDotWriter(string newLine)
    {
        ArgumentNullException.ThrowIfNull(newLine);
        _newLine = newLine;
    }

    /// <summary>Renders the graph.</summary>
    /// <param name="graph">The graph to render.</param>
    /// <param name="graphName">The graph name written after <c>digraph</c>, verbatim (it must be a valid DOT ID).</param>
    /// <returns>The DOT text, ending with the line terminator.</returns>
    public string Render(DirectedAcyclicGraph<TNode> graph, string graphName)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentException.ThrowIfNullOrEmpty(graphName);
        var text = new StringBuilder();
        text.Append("digraph ").Append(graphName).Append(" {").Append(_newLine);
        foreach (string statement in GraphAttributeStatements(graph))
        {
            text.Append(statement).Append(_newLine);
        }

        foreach (string statement in BodyStatements(graph))
        {
            text.Append(statement).Append(_newLine);
        }

        text.Append('}').Append(_newLine);
        return text.ToString();
    }

    /// <summary>
    /// The graph-level statement lines written right after <c>digraph {name} {</c> (for example default node and edge
    /// attributes). The default writes none.
    /// </summary>
    /// <param name="graph">The graph being rendered.</param>
    /// <returns>Complete lines, including their indentation.</returns>
    protected virtual IEnumerable<string> GraphAttributeStatements(DirectedAcyclicGraph<TNode> graph) => [];

    /// <summary>The attributes of a node, in output order. The default has none.</summary>
    /// <param name="node">The node.</param>
    /// <returns>Name/value pairs; values are raw DOT (already quoted with <see cref="Quote"/> where needed).</returns>
    protected virtual IEnumerable<KeyValuePair<string, string>> NodeAttributes(TNode node) => [];

    /// <summary>The attributes of an edge, in output order. The default is the edge's label, when it has one.</summary>
    /// <param name="edge">The edge.</param>
    /// <returns>Name/value pairs; values are raw DOT (already quoted with <see cref="Quote"/> where needed).</returns>
    protected virtual IEnumerable<KeyValuePair<string, string>> EdgeAttributes(GraphEdge<TNode> edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        return edge.Label is null ? [] : [new KeyValuePair<string, string>("label", Quote(edge.Label))];
    }

    /// <summary>
    /// The statement lines of the graph body. The default writes every node (insertion order), then every edge
    /// (insertion order).
    /// </summary>
    /// <param name="graph">The graph being rendered.</param>
    /// <returns>Complete lines, including their indentation.</returns>
    protected virtual IEnumerable<string> BodyStatements(DirectedAcyclicGraph<TNode> graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        return graph.Nodes.Select(NodeStatement).Concat(graph.Edges.Select(EdgeStatement));
    }

    /// <summary>The statement line of a node: <c>  "id" [attributes];</c>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The line, including its indentation.</returns>
    protected string NodeStatement(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return Indent + Quote(node.Id) + AttributeList(NodeAttributes(node)) + ";";
    }

    /// <summary>The statement line of an edge: <c>  "from" -> "to" [attributes];</c>.</summary>
    /// <param name="edge">The edge.</param>
    /// <returns>The line, including its indentation.</returns>
    protected string EdgeStatement(GraphEdge<TNode> edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        return Indent + Quote(edge.From.Id) + " -> " + Quote(edge.To.Id) + AttributeList(EdgeAttributes(edge)) + ";";
    }

    /// <summary>
    /// A DOT quoted string: <paramref name="text"/> in double quotes, with <c>"</c> escaped as <c>\"</c> and each
    /// newline character written as the two characters <c>\n</c>.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The quoted text.</returns>
    protected static string Quote(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return "\"" + text.Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal) + "\"";
    }

    private static string AttributeList(IEnumerable<KeyValuePair<string, string>> attributes)
    {
        string list = string.Join(", ", attributes.Select(a => a.Key + "=" + a.Value));
        return list.Length == 0 ? string.Empty : " [" + list + "]";
    }
}
