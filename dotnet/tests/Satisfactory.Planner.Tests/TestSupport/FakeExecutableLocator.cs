using Satisfactory.Planner.Graphviz;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>Test double for <see cref="IExecutableLocator"/>: resolves only the names it was given and records every lookup.</summary>
public sealed class FakeExecutableLocator : IExecutableLocator
{
    private readonly Dictionary<string, string> _executables = new(StringComparer.Ordinal);
    private readonly List<string> _requests = [];

    /// <summary>Gets the names passed to <see cref="Find"/>, in call order.</summary>
    public IReadOnlyList<string> Requests => _requests;

    /// <summary>Makes <paramref name="name"/> resolve to <paramref name="path"/>.</summary>
    public FakeExecutableLocator WithExecutable(string name, string path)
    {
        _executables[name] = path;
        return this;
    }

    /// <inheritdoc />
    public string? Find(string executableName)
    {
        _requests.Add(executableName);
        return _executables.TryGetValue(executableName, out string? path) ? path : null;
    }
}
