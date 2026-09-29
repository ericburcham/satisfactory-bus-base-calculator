using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Selection;

/// <summary>The recipe chosen for each item (Python <c>_chosen</c> / <c>recipe_for</c>).</summary>
public sealed class RecipeChoice
{
    private readonly OrderedDictionary<ItemId, Recipe> _recipes;

    /// <summary>Creates the choice.</summary>
    /// <param name="recipes">Item to its chosen recipe (copied, order kept).</param>
    public RecipeChoice(IReadOnlyDictionary<ItemId, Recipe> recipes)
    {
        ArgumentNullException.ThrowIfNull(recipes);
        _recipes = new OrderedDictionary<ItemId, Recipe>(recipes);
    }

    /// <summary>Item to its chosen recipe.</summary>
    public IReadOnlyDictionary<ItemId, Recipe> Recipes => _recipes;

    /// <summary>The recipe chosen for an item (Python <c>recipe_for</c>).</summary>
    /// <param name="item">The item.</param>
    /// <returns>The recipe, or <see langword="null"/> when the item has none (extractable or not makeable).</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "The member name For is fixed by the shared port spec; the solution is C# only.")]
    public Recipe? For(ItemId item) => _recipes.TryGetValue(item, out Recipe? recipe) ? recipe : null;
}
