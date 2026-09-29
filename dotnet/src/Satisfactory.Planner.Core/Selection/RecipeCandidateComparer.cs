using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Numerics;

namespace Satisfactory.Planner.Core.Selection;

/// <summary>
/// The preference order of the candidate recipes for one item (the sort key of Python <c>_candidates</c>), field by
/// field: resource cost rounded to <see cref="PlannerConstants.CostRoundingDigits"/> decimals (cheapest first), vanilla
/// before alternate, unpackaging last, recipes whose main product is the item first, recipes named like the item
/// (ignoring <c>alternate: </c> and case) first, fewer ingredients first, then the name (ordinal).
/// </summary>
public sealed class RecipeCandidateComparer : IComparer<Recipe>
{
    private readonly ItemId _item;
    private readonly string _itemName;
    private readonly ResourceCostTable _costs;

    /// <summary>Creates the comparer.</summary>
    /// <param name="item">The item the candidates make.</param>
    /// <param name="itemName">The item's display name (compared lower-cased).</param>
    /// <param name="costs">The resource costs (<see cref="ResourceCostTable.CostOf"/>).</param>
    public RecipeCandidateComparer(ItemId item, string itemName, ResourceCostTable costs)
    {
        ArgumentNullException.ThrowIfNull(itemName);
        ArgumentNullException.ThrowIfNull(costs);
        _item = item;
        _itemName = itemName.ToLowerInvariant();
        _costs = costs;
    }

    /// <inheritdoc />
    public int Compare(Recipe? x, Recipe? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        int result = PythonNumerics.Round(_costs.CostOf(x, _item), PlannerConstants.CostRoundingDigits)
            .CompareTo(PythonNumerics.Round(_costs.CostOf(y, _item), PlannerConstants.CostRoundingDigits));
        if (result != 0)
        {
            return result;
        }

        result = x.IsAlternate.CompareTo(y.IsAlternate);
        if (result != 0)
        {
            return result;
        }

        result = IsUnpackaging(x).CompareTo(IsUnpackaging(y));
        if (result != 0)
        {
            return result;
        }

        result = (x.MainProduct.Item != _item).CompareTo(y.MainProduct.Item != _item);
        if (result != 0)
        {
            return result;
        }

        result = NameDiffers(x).CompareTo(NameDiffers(y));
        if (result != 0)
        {
            return result;
        }

        result = x.Ingredients.Count.CompareTo(y.Ingredients.Count);
        return result != 0 ? result : string.CompareOrdinal(x.Name, y.Name);
    }

    private static bool IsUnpackaging(Recipe recipe) =>
        recipe.Name.StartsWith(PlannerConstants.UnpackagePrefix, StringComparison.Ordinal);

    private bool NameDiffers(Recipe recipe) =>
        !string.Equals(
            recipe.Name.ToLowerInvariant().Replace(PlannerConstants.AlternateNamePrefix, string.Empty, StringComparison.Ordinal),
            _itemName,
            StringComparison.Ordinal);
}
