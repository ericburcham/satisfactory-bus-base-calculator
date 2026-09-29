namespace Satisfactory.Planner.Graphviz;

/// <summary>The outcome of a finished process.</summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="StdOut">Everything the process wrote to standard output.</param>
/// <param name="StdErr">Everything the process wrote to standard error.</param>
public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);
