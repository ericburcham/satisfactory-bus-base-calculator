namespace Satisfactory.Planner.Graphviz;

/// <summary>Finds executables by name, in the way a shell would resolve a command.</summary>
public interface IExecutableLocator
{
    /// <summary>Looks up an executable.</summary>
    /// <param name="executableName">The command name, for example <c>dot</c>.</param>
    /// <returns>The full path of the first matching executable, or <see langword="null"/> when none is found.</returns>
    string? Find(string executableName);
}
