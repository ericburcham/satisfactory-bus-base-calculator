using Satisfactory.Planner.Graphviz;
using Satisfactory.Planner.Reporting.Rendering;

namespace Satisfactory.Planner.Cli;

/// <summary>
/// Adapter from the reporting layer's <see cref="IGraphRenderer"/> to a Graphviz <see cref="IDotRenderer"/>:
/// renders <c>tier_N.dot</c> to <c>tier_N.svg</c> next to it when Graphviz is available, and does nothing otherwise
/// (Python: <c>if shutil.which("dot"): subprocess.run(["dot", "-Tsvg", base + ".dot", "-o", base + ".svg"], check=True)</c>).
/// </summary>
public sealed class GraphvizRendererAdapter : IGraphRenderer
{
    private readonly IDotRenderer _renderer;

    /// <summary>Initializes a new instance of the <see cref="GraphvizRendererAdapter"/> class.</summary>
    /// <param name="renderer">The DOT renderer to delegate to.</param>
    public GraphvizRendererAdapter(IDotRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        _renderer = renderer;
    }

    /// <summary>
    /// Renders <paramref name="dotFilePath"/> to <c>Path.ChangeExtension(dotFilePath, ".svg")</c> when the renderer
    /// is available; otherwise does nothing.
    /// </summary>
    /// <param name="dotFilePath">The path of the <c>.dot</c> file to render.</param>
    /// <exception cref="GraphvizRenderException">The renderer failed (propagated unchanged).</exception>
    public void Render(string dotFilePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(dotFilePath);
        if (_renderer.IsAvailable)
        {
            _renderer.RenderSvg(dotFilePath, Path.ChangeExtension(dotFilePath, ".svg"));
        }
    }
}
