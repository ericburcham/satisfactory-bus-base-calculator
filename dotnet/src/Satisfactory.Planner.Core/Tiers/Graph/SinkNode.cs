namespace Satisfactory.Planner.Core.Tiers.Graph;

/// <summary>The AWESOME Sink; its id is <c>SINK</c>.</summary>
public sealed class SinkNode : TierGraphNode
{
    /// <summary>The id of the sink node.</summary>
    public const string SinkId = "SINK";

    /// <summary>Initializes a new sink node.</summary>
    public SinkNode()
        : base(SinkId)
    {
    }

    /// <inheritdoc />
    public override TierGraphNodeKind Kind => TierGraphNodeKind.Sink;

    /// <inheritdoc />
    public override string Label => "AWESOME Sink";
}
