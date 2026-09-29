using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Tiers.Graph;

/// <summary>An item that enters the tier from outside its new machines; its id is <c>"in_" + item id</c>.</summary>
public abstract class ExternalInputNode : TierGraphNode
{
    /// <summary>The prefix of every external input node id.</summary>
    public const string IdPrefix = "in_";

    /// <summary>Initializes a new external input node.</summary>
    /// <param name="item">The item.</param>
    /// <param name="itemName">The display name of the item.</param>
    protected ExternalInputNode(ItemId item, string itemName)
        : base(IdFor(item))
    {
        ArgumentNullException.ThrowIfNull(itemName);
        Item = item;
        ItemName = itemName;
    }

    /// <summary>Gets the item.</summary>
    public ItemId Item { get; }

    /// <summary>Gets the display name of the item.</summary>
    public string ItemName { get; }

    /// <inheritdoc />
    public override string Label => $"{ItemName}\n({Source})";

    /// <summary>Gets the word shown in brackets under the name, such as <c>raw</c> or <c>bus</c>.</summary>
    protected abstract string Source { get; }

    /// <summary>Gets the node id used for an item's external input.</summary>
    /// <param name="item">The item.</param>
    /// <returns><c>"in_" + item id</c>.</returns>
    public static string IdFor(ItemId item) => IdPrefix + item.Value;
}
