using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Numerics;

namespace Satisfactory.Planner.Core.Costing;

/// <summary>
/// The natural resources needed per unit of each item (Python <c>self.cost</c>) and the cost of making an item with a
/// given recipe (Python <c>recipe_cost</c>).
/// </summary>
public sealed class ResourceCostTable
{
    private readonly OrderedDictionary<ItemId, double> _costs;
    private readonly CreditPolicy _credit;

    /// <summary>Creates the table.</summary>
    /// <param name="costs">The cost per unit of every costed item (copied, order kept).</param>
    /// <param name="credit">
    /// The credit policy; <see cref="CostOf"/> reads its <see cref="CreditPolicy.NoCredit"/> at call time, as the Python
    /// reads <c>self.no_credit</c>.
    /// </param>
    public ResourceCostTable(IReadOnlyDictionary<ItemId, double> costs, CreditPolicy credit)
    {
        ArgumentNullException.ThrowIfNull(costs);
        ArgumentNullException.ThrowIfNull(credit);
        _costs = new OrderedDictionary<ItemId, double>(costs);
        _credit = credit;
    }

    /// <summary>The cost per unit of every item that has one; items without a cost are unreachable (infinite).</summary>
    public IReadOnlyDictionary<ItemId, double> Costs => _costs;

    /// <summary>Looks up the cost per unit of an item.</summary>
    /// <param name="item">The item.</param>
    /// <param name="cost">The cost when the item has one.</param>
    /// <returns><see langword="true"/> when the item has a cost.</returns>
    public bool TryGetCost(ItemId item, out double cost) => _costs.TryGetValue(item, out cost);

    /// <summary>
    /// Natural resources per unit of <paramref name="item"/> when made with <paramref name="recipe"/> (Python
    /// <c>recipe_cost</c>): <c>max(0.0, cin - credit) / amount</c>, where <c>cin</c> sums the ingredients' costs
    /// (unknown ones are infinite) and <c>credit</c> sums the costs of the other products (unknown ones count 0) that
    /// earn credit. The product entry is the first one that is <paramref name="item"/>; any other entry, even of the
    /// same item, is a different product.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="item">A product of the recipe.</param>
    /// <returns>The cost per unit; <see cref="double.PositiveInfinity"/> when an ingredient has no cost.</returns>
    /// <exception cref="ArgumentException"><paramref name="recipe"/> does not produce <paramref name="item"/>.</exception>
    public double CostOf(Recipe recipe, ItemId item)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        double cin = 0.0;
        foreach (ItemAmount ingredient in recipe.Ingredients)
        {
            cin += CostOrInfinity(ingredient.Item) * ingredient.Amount;
        }

        int productIndex = IndexOfProduct(recipe, item);
        double credit = 0.0;
        for (int index = 0; index < recipe.Products.Count; index++)
        {
            ItemAmount other = recipe.Products[index];
            if (index != productIndex && _credit.EarnsCredit(other.Item))
            {
                credit += _costs.GetValueOrDefault(other.Item, 0.0) * other.Amount;
            }
        }

        return PythonNumerics.Max(0.0, cin - credit) / recipe.Products[productIndex].Amount;
    }

    private double CostOrInfinity(ItemId item) =>
        _costs.TryGetValue(item, out double cost) ? cost : double.PositiveInfinity;

    private static int IndexOfProduct(Recipe recipe, ItemId item)
    {
        for (int index = 0; index < recipe.Products.Count; index++)
        {
            if (recipe.Products[index].Item == item)
            {
                return index;
            }
        }

        throw new ArgumentException($"Recipe {recipe.Id} does not produce {item}.", nameof(item));
    }
}
