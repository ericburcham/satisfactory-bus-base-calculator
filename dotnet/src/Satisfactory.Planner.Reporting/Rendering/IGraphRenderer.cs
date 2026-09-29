namespace Satisfactory.Planner.Reporting.Rendering;

/// <summary>Renders a written Graphviz <c>.dot</c> file into a picture, when a renderer is available.</summary>
public interface IGraphRenderer
{
    /// <summary>Renders the graph in <paramref name="dotFilePath"/> (for example to an <c>.svg</c> next to it).</summary>
    /// <param name="dotFilePath">The path of the <c>.dot</c> file to render.</param>
    void Render(string dotFilePath);
}
