using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Progression;

/// <summary>The earliest progression tier of recipes, buildings and raw resources.</summary>
public interface IUnlockTierResolver
{
    /// <summary>
    /// The tier from which a recipe can run: the later of its own unlock tier and its building's
    /// (Python <c>recipe_tier</c>).
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <returns>The tier.</returns>
    int RecipeTier(Recipe recipe);

    /// <summary>The unlock tier of a building's build recipe, 0 when unknown (Python <c>building_tier</c>).</summary>
    /// <param name="building">The building.</param>
    /// <returns>The tier.</returns>
    int BuildingTier(BuildingId building);

    /// <summary>The tier of the building that extracts a natural resource (Python <c>raw_tier</c>).</summary>
    /// <param name="resource">The resource.</param>
    /// <returns>The tier.</returns>
    int RawResourceTier(ItemId resource);
}
