using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Costing;

/// <summary>
/// What the byproduct optimisation has learnt in one tier (Python <c>no_credit</c> and <c>no_byproduct</c>): byproducts
/// that end up wasted earn no credit in the resource costs, and fluids that cannot be disposed of must never be left
/// over as a byproduct. Mutable; create a fresh one per tier.
/// </summary>
public sealed class CreditPolicy
{
    private readonly HashSet<ItemId> _noCredit = [];
    private readonly HashSet<ItemId> _noByproduct = [];

    /// <summary>Items whose production as a byproduct earns no credit (Python <c>no_credit</c>).</summary>
    public IReadOnlySet<ItemId> NoCredit => _noCredit;

    /// <summary>Fluids no candidate recipe may make as a byproduct (Python <c>no_byproduct</c>).</summary>
    public IReadOnlySet<ItemId> NoByproduct => _noByproduct;

    /// <summary>Whether a byproduct is credited at its own cost.</summary>
    /// <param name="item">The byproduct.</param>
    /// <returns><see langword="true"/> unless the item is in <see cref="NoCredit"/>.</returns>
    public bool EarnsCredit(ItemId item) => !_noCredit.Contains(item);

    /// <summary>Whether recipes making the item as a byproduct are excluded.</summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true"/> when the item is in <see cref="NoByproduct"/>.</returns>
    public bool IsForbiddenByproduct(ItemId item) => _noByproduct.Contains(item);

    /// <summary>Adds items to <see cref="NoCredit"/> (Python <c>no_credit |= wasted</c>).</summary>
    /// <param name="items">The wasted byproducts.</param>
    public void DenyCredit(IEnumerable<ItemId> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _noCredit.UnionWith(items);
    }

    /// <summary>Adds a fluid to <see cref="NoByproduct"/> (Python <c>no_byproduct.add(fluid)</c>).</summary>
    /// <param name="fluid">The fluid that could not be disposed of.</param>
    public void ForbidByproduct(ItemId fluid) => _noByproduct.Add(fluid);
}
