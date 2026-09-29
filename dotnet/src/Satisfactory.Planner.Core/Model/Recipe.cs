using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Model;

/// <summary>
/// A recipe the planner can run in a machine: a manufacturing recipe from the data file or a generator pseudo-recipe.
/// </summary>
/// <param name="Id">The recipe's <c>className</c>.</param>
/// <param name="Name">The display name (<c>name</c>).</param>
/// <param name="IsAlternate">The <c>alternate</c> flag.</param>
/// <param name="TimeSeconds">The cycle time in seconds (<c>time</c>).</param>
/// <param name="BuildingId">The machine that runs it (<c>producedIn[0]</c>).</param>
/// <param name="Ingredients">The ingredients per cycle, in data file order.</param>
/// <param name="Products">The products per cycle, in data file order; the first one is the main product.</param>
public abstract record Recipe(
    RecipeId Id,
    string Name,
    bool IsAlternate,
    double TimeSeconds,
    BuildingId BuildingId,
    IReadOnlyList<ItemAmount> Ingredients,
    IReadOnlyList<ItemAmount> Products)
{
    /// <summary>The first product (Python <c>r["products"][0]</c>).</summary>
    public ItemAmount MainProduct => Products[0];

    /// <summary>
    /// The per-minute rate of one machine at 100% clock for an ingredient or product amount, computed exactly as the
    /// Python does: <c>amount * 60 / time</c>.
    /// </summary>
    /// <param name="amount">An ingredient or product of this recipe.</param>
    /// <returns><c>(amount.Amount * 60) / TimeSeconds</c>.</returns>
    public double PerMinute(ItemAmount amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        return amount.Amount * 60 / TimeSeconds;
    }

    /// <summary>Whether any product of this recipe is <paramref name="item"/>.</summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true"/> when <paramref name="item"/> is among <see cref="Products"/>.</returns>
    public bool Produces(ItemId item) => Products.Any(p => p.Item == item);

    /// <summary>Whether any ingredient of this recipe is <paramref name="item"/>.</summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true"/> when <paramref name="item"/> is among <see cref="Ingredients"/>.</returns>
    public bool Consumes(ItemId item) => Ingredients.Any(i => i.Item == item);
}
