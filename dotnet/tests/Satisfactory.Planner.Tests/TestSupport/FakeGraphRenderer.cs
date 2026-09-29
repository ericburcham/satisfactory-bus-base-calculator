using Satisfactory.Planner.Reporting.Rendering;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>Test double for <see cref="IGraphRenderer"/>: logs <c>render:{path}</c> and records the paths.</summary>
public sealed class FakeGraphRenderer(List<string> log) : IGraphRenderer
{
    private readonly List<string> _paths = [];

    public IReadOnlyList<string> Paths => _paths;

    public void Render(string dotFilePath)
    {
        log.Add($"render:{dotFilePath}");
        _paths.Add(dotFilePath);
    }
}
