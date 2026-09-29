using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Tiers.Graph;

/// <summary>An item taken from the main bus; labelled <c>(bus)</c>.</summary>
public sealed class BusInputNode : ExternalInputNode
{
    /// <summary>Initializes a new bus input node.</summary>
    /// <param name="item">The item.</param>
    /// <param name="itemName">The display name of the item.</param>
    public BusInputNode(ItemId item, string itemName)
        : base(item, itemName)
    {
    }

    /// <inheritdoc />
    public override TierGraphNodeKind Kind => TierGraphNodeKind.BusInput;

    /// <inheritdoc />
    protected override string Source => "bus";
}
