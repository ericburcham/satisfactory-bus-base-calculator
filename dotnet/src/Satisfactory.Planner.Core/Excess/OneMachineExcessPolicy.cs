using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Excess;

/// <summary>
/// One full machine of excess (Python <c>plan</c>): the output of one machine at 100% clock, from the first product
/// entry that is the item, <c>amount * 60 / time</c>.
/// </summary>
public sealed class OneMachineExcessPolicy : IExcessPolicy
{
    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="recipe"/> does not produce <paramref name="item"/>.</exception>
    public double ExcessRate(ItemId item, Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ItemAmount product = recipe.Products.FirstOrDefault(p => p.Item == item)
            ?? throw new ArgumentException($"Recipe {recipe.Id} does not produce {item}.", nameof(recipe));
        return recipe.PerMinute(product);
    }
}
