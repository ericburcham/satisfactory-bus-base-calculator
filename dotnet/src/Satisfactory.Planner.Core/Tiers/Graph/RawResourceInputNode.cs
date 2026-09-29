using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Tiers.Graph;

/// <summary>A raw resource taken straight from extractors; labelled <c>(raw)</c>.</summary>
public sealed class RawResourceInputNode : ExternalInputNode
{
    /// <summary>Initializes a new raw resource input node.</summary>
    /// <param name="item">The raw resource.</param>
    /// <param name="itemName">The display name of the resource.</param>
    public RawResourceInputNode(ItemId item, string itemName)
        : base(item, itemName)
    {
    }

    /// <inheritdoc />
    public override TierGraphNodeKind Kind => TierGraphNodeKind.RawInput;

    /// <inheritdoc />
    protected override string Source => "raw";
}
