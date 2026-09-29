namespace Satisfactory.Planner.Graphviz;

/// <summary>Thrown when Graphviz fails to render a DOT file (the Python raises <c>CalledProcessError</c>).</summary>
public sealed class GraphvizRenderException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="GraphvizRenderException"/> class.</summary>
    public GraphvizRenderException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GraphvizRenderException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public GraphvizRenderException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GraphvizRenderException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public GraphvizRenderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GraphvizRenderException"/> class for a failed process.</summary>
    /// <param name="message">The error message; it should include the exit code and the standard error text.</param>
    /// <param name="exitCode">The non-zero exit code of the Graphviz process.</param>
    /// <param name="standardError">What the Graphviz process wrote to standard error.</param>
    public GraphvizRenderException(string message, int exitCode, string standardError)
        : base(message)
    {
        ExitCode = exitCode;
        StandardError = standardError;
    }

    /// <summary>Gets the exit code of the failed Graphviz process, or <see langword="null"/> when not known.</summary>
    public int? ExitCode { get; }

    /// <summary>Gets the standard error output of the failed Graphviz process, or <see langword="null"/> when not known.</summary>
    public string? StandardError { get; }
}
