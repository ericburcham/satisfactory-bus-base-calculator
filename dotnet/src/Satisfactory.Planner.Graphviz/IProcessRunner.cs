namespace Satisfactory.Planner.Graphviz;

/// <summary>Runs an external process to completion and captures its output.</summary>
public interface IProcessRunner
{
    /// <summary>Starts <paramref name="fileName"/> with <paramref name="arguments"/> and waits for it to exit.</summary>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="arguments">The arguments, each passed as one argument without shell interpretation.</param>
    /// <returns>The exit code and the captured standard output and standard error.</returns>
    ProcessResult Run(string fileName, IReadOnlyList<string> arguments);
}
