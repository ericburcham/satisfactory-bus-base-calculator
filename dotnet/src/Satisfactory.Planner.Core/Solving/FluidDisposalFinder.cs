using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Numerics;
using Satisfactory.Planner.Core.Progression;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>
/// Python <c>disposal_recipe</c>: the recipe that turns a leftover fluid into sinkable solids (alternates allowed).
/// Candidates are the tier's recipes except generators that consume the fluid, whose products are all water or
/// sinkable solids, and whose ingredients are all automatable. Preference: single-input recipes, then packaging, then
/// the fewest extra natural resources (rounded to 6 decimals), then vanilla, then the name (ordinal); the first
/// candidate wins a full tie.
/// </summary>
public sealed class FluidDisposalFinder : IFluidDisposalFinder
{
    private readonly ITierContext _context;
    private readonly ResourceCostTable _costs;

    /// <summary>Creates the finder.</summary>
    /// <param name="context">The tier.</param>
    /// <param name="costs">The resource costs of the extra ingredients.</param>
    public FluidDisposalFinder(ITierContext context, ResourceCostTable costs)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(costs);
        _context = context;
        _costs = costs;
    }

    /// <inheritdoc />
    public Recipe Find(ItemId fluid)
    {
        IReadOnlySet<ItemId> automatable = _context.Automatable;
        Recipe? best = null;
        (bool, bool, double, bool, string) bestKey = default;
        foreach (Recipe recipe in _context.AvailableRecipes)
        {
            if (recipe is GeneratorRecipe
                || !recipe.Consumes(fluid)
                || !recipe.Products.All(IsDisposable)
                || !recipe.Ingredients.All(i => automatable.Contains(i.Item)))
            {
                continue;
            }

            (bool, bool, double, bool, string) key = (
                recipe.Ingredients.Count > 1,
                !recipe.Name.StartsWith(PlannerConstants.PackagedPrefix, StringComparison.Ordinal),
                PythonNumerics.Round(ExtraCost(recipe, fluid), PlannerConstants.CostRoundingDigits),
                recipe.IsAlternate,
                recipe.Name);
            if (best is null || Compare(key, bestKey) < 0)
            {
                best = recipe;
                bestKey = key;
            }
        }

        return best ?? throw new UndisposableFluidException(fluid);
    }

    private bool IsDisposable(ItemAmount product) =>
        product.Item == PlannerConstants.Water
        || (!_context.Data.IsFluid(product.Item) && !PlannerConstants.Unsinkable.Contains(product.Item));

    /// <summary>
    /// Python <c>extra(r)</c>: <c>sum(self.cost.get(i["item"], 0) * i["amount"] for i in r["ingredients"] if
    /// i["item"] != fluid)</c>, with Python's compensated float sum (an unknown item's int 0 is passed as 0.0).
    /// </summary>
    private double ExtraCost(Recipe recipe, ItemId fluid) =>
        PythonNumerics.Sum(recipe.Ingredients
            .Where(i => i.Item != fluid)
            .Select(i => _costs.Costs.GetValueOrDefault(i.Item, 0.0) * i.Amount));

    private static int Compare((bool, bool, double, bool, string) x, (bool, bool, double, bool, string) y)
    {
        int result = x.Item1.CompareTo(y.Item1);
        if (result == 0)
        {
            result = x.Item2.CompareTo(y.Item2);
        }

        if (result == 0)
        {
            result = x.Item3.CompareTo(y.Item3);
        }

        if (result == 0)
        {
            result = x.Item4.CompareTo(y.Item4);
        }

        return result != 0 ? result : string.CompareOrdinal(x.Item5, y.Item5);
    }
}
