namespace Satisfactory.Planner.Graphviz;

/// <summary>
/// Factory that picks the <see cref="IDotRenderer"/> for this machine: a <see cref="GraphvizDotRenderer"/> when the
/// Graphviz <c>dot</c> executable can be located, otherwise a <see cref="NullDotRenderer"/>.
/// </summary>
public sealed class DotRendererFactory
{
    private const string DotExecutableName = "dot";

    private readonly IExecutableLocator _locator;
    private readonly IProcessRunner _runner;

    /// <summary>Initializes a new instance of the <see cref="DotRendererFactory"/> class.</summary>
    /// <param name="locator">Finds the <c>dot</c> executable.</param>
    /// <param name="runner">Runs <c>dot</c> when it is found.</param>
    public DotRendererFactory(IExecutableLocator locator, IProcessRunner runner)
    {
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(runner);
        _locator = locator;
        _runner = runner;
    }

    /// <summary>Creates the renderer, looking up <c>dot</c> once.</summary>
    /// <returns>A <see cref="GraphvizDotRenderer"/> for the located executable, or a <see cref="NullDotRenderer"/>.</returns>
    public IDotRenderer Create()
    {
        string? dotPath = _locator.Find(DotExecutableName);
        return dotPath is null ? new NullDotRenderer() : new GraphvizDotRenderer(dotPath, _runner);
    }
}
