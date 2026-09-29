namespace Satisfactory.Planner.Graphviz;

/// <summary>
/// Null Object <see cref="IDotRenderer"/> used when Graphviz is not installed: it is never available and
/// <see cref="RenderSvg"/> does nothing (the Python only renders <c>if shutil.which("dot")</c>).
/// </summary>
public sealed class NullDotRenderer : IDotRenderer
{
    /// <summary>Gets <see langword="false"/>: nothing is ever rendered.</summary>
    public bool IsAvailable => false;

    /// <summary>Does nothing.</summary>
    /// <param name="dotPath">Ignored.</param>
    /// <param name="svgPath">Ignored.</param>
    public void RenderSvg(string dotPath, string svgPath)
    {
    }
}
