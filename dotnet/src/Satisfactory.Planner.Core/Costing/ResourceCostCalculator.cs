using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Numerics;
using Satisfactory.Planner.Core.Progression;

namespace Satisfactory.Planner.Core.Costing;

/// <summary>
/// Python <c>_costs</c>: the natural resources needed per unit of each item, taking the cheapest recipe for everything
/// (Bellman-Ford style relaxation over the tier's recipes, repeated until nothing changes or
/// <see cref="PlannerConstants.CostRelaxationPasses"/> passes). Extractable items cost their weight. A multi-output
/// recipe's other products are credited at their own cost unless they earn no credit.
/// </summary>
public sealed class ResourceCostCalculator : IResourceCostCalculator
{
    private readonly IResourceWeights _weights;

    /// <summary>Creates the calculator.</summary>
    /// <param name="weights">The weight of each natural resource.</param>
    public ResourceCostCalculator(IResourceWeights weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        _weights = weights;
    }

    /// <inheritdoc />
    public ResourceCostTable Calculate(ITierContext context, CreditPolicy credit)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(credit);
        IExtractorCatalog extractors = context.Extractors;
        IReadOnlySet<ItemId> automatable = context.Automatable;

        // Python iterates a set here; the order of the cost dict is never observed, so sort for determinism.
        var cost = new OrderedDictionary<ItemId, double>();
        foreach (ItemId item in automatable.Where(extractors.IsExtractable).Order())
        {
            cost[item] = _weights.WeightOf(item);
        }

        var recipes = context.AvailableRecipes
            .Where(r => r.Ingredients.All(i => automatable.Contains(i.Item)))
            .ToList();
        for (int pass = 0; pass < PlannerConstants.CostRelaxationPasses; pass++)
        {
            bool changed = false;
            foreach (Recipe recipe in recipes)
            {
                changed |= Relax(recipe, cost, extractors, credit);
            }

            if (!changed)
            {
                break;
            }
        }

        return new ResourceCostTable(cost, credit);
    }

    private static bool Relax(
        Recipe recipe, OrderedDictionary<ItemId, double> cost, IExtractorCatalog extractors, CreditPolicy credit)
    {
        double cin = 0.0;
        foreach (ItemAmount ingredient in recipe.Ingredients)
        {
            cin += (cost.TryGetValue(ingredient.Item, out double c) ? c : double.PositiveInfinity) * ingredient.Amount;
        }

        if (cin == double.PositiveInfinity)
        {
            return false;
        }

        bool changed = false;
        for (int productIndex = 0; productIndex < recipe.Products.Count; productIndex++)
        {
            ItemAmount product = recipe.Products[productIndex];
            if (cost.ContainsKey(product.Item) && extractors.IsExtractable(product.Item))
            {
                continue;
            }

            // "q is not p": every other product entry, even one of the same item.
            double byproductCredit = 0.0;
            for (int otherIndex = 0; otherIndex < recipe.Products.Count; otherIndex++)
            {
                ItemAmount other = recipe.Products[otherIndex];
                if (otherIndex != productIndex
                    && cost.TryGetValue(other.Item, out double otherCost)
                    && credit.EarnsCredit(other.Item))
                {
                    byproductCredit += otherCost * other.Amount;
                }
            }

            double unitCost = PythonNumerics.Max(0.0, cin - byproductCredit) / product.Amount;
            double current = cost.TryGetValue(product.Item, out double known) ? known : double.PositiveInfinity;
            if (unitCost < current - PlannerConstants.CostImprovementTolerance)
            {
                cost[product.Item] = unitCost;
                changed = true;
            }
        }

        return changed;
    }
}
