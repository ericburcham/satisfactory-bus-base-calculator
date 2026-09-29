using Satisfactory.Planner.Graphviz;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>Test double for <see cref="IProcessRunner"/>: records every call and returns a canned result (or throws).</summary>
public sealed class FakeProcessRunner : IProcessRunner
{
    private readonly List<(string FileName, IReadOnlyList<string> Arguments)> _calls = [];

    /// <summary>Gets or sets the result returned by <see cref="Run"/>; defaults to exit code 0 with no output.</summary>
    public ProcessResult Result { get; set; } = new(0, string.Empty, string.Empty);

    /// <summary>Gets or sets an exception that <see cref="Run"/> throws (after recording the call) instead of returning.</summary>
    public Exception? ExceptionToThrow { get; set; }

    /// <summary>Gets the recorded calls, in order; each argument list is a copy taken at call time.</summary>
    public IReadOnlyList<(string FileName, IReadOnlyList<string> Arguments)> Calls => _calls;

    /// <inheritdoc />
    public ProcessResult Run(string fileName, IReadOnlyList<string> arguments)
    {
        _calls.Add((fileName, arguments.ToArray()));
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Result;
    }
}
