using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>Python <c>_balance</c>: the net rate of every item in a production state.</summary>
public sealed class ProductionBalance
{
    private ProductionBalance()
    {
    }

    /// <summary>
    /// Computes the balance exactly as the Python: for every run (in order) <c>f = n * 60 / time</c>, then each product
    /// adds <c>amount * f</c> and each ingredient subtracts <c>amount * f</c>; then extraction is added and demand
    /// subtracted. Keys appear in the order the Python <c>defaultdict</c> first touches them.
    /// </summary>
    /// <param name="state">The production state.</param>
    /// <returns>Item to its net rate per minute (positive = surplus, negative = shortfall).</returns>
    public static OrderedDictionary<ItemId, double> Of(ProductionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var balance = new OrderedDictionary<ItemId, double>();
        foreach ((RecipeId id, double machines) in state.Runs)
        {
            Recipe recipe = state.Recipes[id];
            double f = machines * PlannerConstants.SecondsPerMinute / recipe.TimeSeconds;
            foreach (ItemAmount product in recipe.Products)
            {
                Add(balance, product.Item, product.Amount * f);
            }

            foreach (ItemAmount ingredient in recipe.Ingredients)
            {
                Subtract(balance, ingredient.Item, ingredient.Amount * f);
            }
        }

        foreach ((ItemId resource, double rate) in state.Extract)
        {
            Add(balance, resource, rate);
        }

        foreach ((ItemId item, double rate) in state.Demand)
        {
            Subtract(balance, item, rate);
        }

        return balance;
    }

    private static void Add(OrderedDictionary<ItemId, double> balance, ItemId item, double value) =>
        balance[item] = balance.GetValueOrDefault(item, 0.0) + value;

    private static void Subtract(OrderedDictionary<ItemId, double> balance, ItemId item, double value) =>
        balance[item] = balance.GetValueOrDefault(item, 0.0) - value;
}
