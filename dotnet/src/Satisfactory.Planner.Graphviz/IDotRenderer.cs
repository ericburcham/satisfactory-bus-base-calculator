namespace Satisfactory.Planner.Graphviz;

/// <summary>Renders Graphviz DOT files to images.</summary>
public interface IDotRenderer
{
    /// <summary>Gets a value indicating whether rendering actually happens (Graphviz was found).</summary>
    bool IsAvailable { get; }

    /// <summary>Renders the DOT file at <paramref name="dotPath"/> to an SVG file at <paramref name="svgPath"/>.</summary>
    /// <param name="dotPath">The input DOT file.</param>
    /// <param name="svgPath">The SVG file to write.</param>
    void RenderSvg(string dotPath, string svgPath);
}
