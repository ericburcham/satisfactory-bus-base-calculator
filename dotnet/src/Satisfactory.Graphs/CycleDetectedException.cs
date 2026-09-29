namespace Satisfactory.Graphs;

/// <summary>
/// The exception thrown when adding an edge to a <see cref="DirectedAcyclicGraph{TNode}"/> would close a cycle
/// (including a self-loop). It names the rejected edge in its message and in <see cref="FromId"/> /
/// <see cref="ToId"/>.
/// </summary>
public sealed class CycleDetectedException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CycleDetectedException"/> class with a generic message and
    /// empty edge ids.
    /// </summary>
    public CycleDetectedException()
        : this("Adding the edge would create a cycle.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CycleDetectedException"/> class with the specified message and
    /// empty edge ids.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CycleDetectedException(string message)
        : base(message)
    {
        FromId = string.Empty;
        ToId = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CycleDetectedException"/> class with the specified message,
    /// inner exception and empty edge ids.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public CycleDetectedException(string message, Exception innerException)
        : base(message, innerException)
    {
        FromId = string.Empty;
        ToId = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CycleDetectedException"/> class for the rejected edge
    /// <paramref name="fromId"/> -&gt; <paramref name="toId"/>. The message names both ids.
    /// </summary>
    /// <param name="fromId">The id of the node the rejected edge leaves.</param>
    /// <param name="toId">The id of the node the rejected edge enters.</param>
    /// <exception cref="ArgumentNullException"><paramref name="fromId"/> or <paramref name="toId"/> is
    /// <see langword="null"/>.</exception>
    public CycleDetectedException(string fromId, string toId)
        : base(FormatMessage(fromId, toId))
    {
        FromId = fromId;
        ToId = toId;
    }

    /// <summary>
    /// Gets the id of the node the rejected edge leaves (empty when the exception was created without edge ids).
    /// </summary>
    public string FromId { get; }

    /// <summary>
    /// Gets the id of the node the rejected edge enters (empty when the exception was created without edge ids).
    /// </summary>
    public string ToId { get; }

    private static string FormatMessage(string fromId, string toId)
    {
        ArgumentNullException.ThrowIfNull(fromId);
        ArgumentNullException.ThrowIfNull(toId);
        return string.Equals(fromId, toId, StringComparison.Ordinal)
            ? $"Adding the edge \"{fromId}\" -> \"{toId}\" would create a self-loop (a cycle)."
            : $"Adding the edge \"{fromId}\" -> \"{toId}\" would create a cycle: \"{fromId}\" is already reachable from \"{toId}\".";
    }
}
