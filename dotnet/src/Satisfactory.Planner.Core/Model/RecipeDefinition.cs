using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Model;

/// <summary>
/// Any entry of the data file's <c>recipes</c> object (including build-gun, hand and workshop recipes), reduced to what
/// the unlock-tier computation needs.
/// </summary>
/// <param name="Id">The recipe's <c>className</c>.</param>
/// <param name="Name">The display name (<c>name</c>; the class name when absent).</param>
/// <param name="Ingredients">The ingredients, in data file order.</param>
/// <param name="Products">The products, in data file order.</param>
public sealed record RecipeDefinition(
    RecipeId Id,
    string Name,
    IReadOnlyList<ItemAmount> Ingredients,
    IReadOnlyList<ItemAmount> Products);
