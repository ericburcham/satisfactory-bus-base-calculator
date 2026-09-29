using System.Collections.Frozen;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Progression;

/// <summary>
/// The planner's state for one tier (Python <c>set_tier</c>); computes <see cref="Automatable"/> as Python
/// <c>_automatable</c>: start from the resources that have an extractor, then add every product of an available recipe
/// whose ingredients are all automatable, until nothing changes.
/// </summary>
public sealed class TierContext : ITierContext
{
    /// <summary>Creates the context and computes the automatable items.</summary>
    /// <param name="tier">The tier.</param>
    /// <param name="data">The game data.</param>
    /// <param name="availableRecipes">The recipes unlocked by the tier, in <see cref="GameData.Recipes"/> order (copied).</param>
    /// <param name="lockedRawResources">The resources without an unlocked extractor (copied).</param>
    /// <param name="extractors">The tier's extractors, built with <paramref name="lockedRawResources"/>.</param>
    public TierContext(
        int tier,
        GameData data,
        IEnumerable<Recipe> availableRecipes,
        IReadOnlySet<ItemId> lockedRawResources,
        IExtractorCatalog extractors)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(availableRecipes);
        ArgumentNullException.ThrowIfNull(lockedRawResources);
        ArgumentNullException.ThrowIfNull(extractors);
        Tier = tier;
        Data = data;
        AvailableRecipes = availableRecipes.ToList().AsReadOnly();
        LockedRawResources = lockedRawResources.ToFrozenSet();
        Extractors = extractors;
        Automatable = ComputeAutomatable(data, AvailableRecipes, extractors);
    }

    /// <inheritdoc />
    public int Tier { get; }

    /// <inheritdoc />
    public GameData Data { get; }

    /// <inheritdoc />
    public IReadOnlyList<Recipe> AvailableRecipes { get; }

    /// <inheritdoc />
    public IReadOnlySet<ItemId> LockedRawResources { get; }

    /// <inheritdoc />
    public IReadOnlySet<ItemId> Automatable { get; }

    /// <inheritdoc />
    public IExtractorCatalog Extractors { get; }

    private static FrozenSet<ItemId> ComputeAutomatable(
        GameData data, IReadOnlyList<Recipe> recipes, IExtractorCatalog extractors)
    {
        var automatable = new HashSet<ItemId>(data.Resources.Where(extractors.IsExtractable));
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (Recipe recipe in recipes)
            {
                if (!recipe.Ingredients.All(i => automatable.Contains(i.Item)))
                {
                    continue;
                }

                foreach (ItemAmount product in recipe.Products)
                {
                    if (automatable.Add(product.Item))
                    {
                        changed = true;
                    }
                }
            }
        }

        return automatable.ToFrozenSet();
    }
}
